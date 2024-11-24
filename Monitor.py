import cv2
import numpy as np
import mmap
import ffmpeg

# 공유 메모리 크기 (RGBA 이미지의 크기: 1280 * 720 * 4바이트 * 4개 카메라)
image_width = 1280
image_height = 720
bytes_per_pixel = 4  # RGBA 형식이므로 4바이트
shared_memory_size = image_width * image_height * bytes_per_pixel * 4  # 4개의 카메라 데이터 포함

rtmp_url = "rtmp://14.39.59.81/live/0000"
frame_rate = 30
fourcc = cv2.VideoWriter_fourcc(*'X264')


# 공유 메모리에서 데이터를 읽어오는 함수 (offset 기반)
def read_from_shared_memory(memory_name, offset, size):
    # Windows에서 태그 기반으로 메모리 매핑
    mm = mmap.mmap(0, shared_memory_size, tagname=memory_name)
    mm.seek(offset)  # 지정된 offset 위치로 이동
    image_data = mm.read(size)  # 지정된 크기만큼 데이터 읽기
    mm.close()
    
    # 읽은 데이터를 NumPy 배열로 변환
    image_np = np.frombuffer(image_data, dtype=np.uint8)
    image_np = image_np.reshape((image_height, image_width, 4))  # RGBA 포맷
    image_np = cv2.cvtColor(image_np, cv2.COLOR_BGRA2RGB)  # BGRA -> BGR 변환
    image_np = cv2.flip(image_np, 0)  # 상하 반전
    
    return image_np


def increase_brightness(image, value=50):
    hsv = cv2.cvtColor(image, cv2.COLOR_BGR2HSV)  # BGR -> HSV 변환
    h, s, v = cv2.split(hsv)
    v = cv2.add(v, value)  # 밝기 증가
    v = np.clip(v, 0, 255)  # 0~255 범위 유지
    final_hsv = cv2.merge((h, s, v))
    bright_image = cv2.cvtColor(final_hsv, cv2.COLOR_HSV2BGR)  # HSV -> BGR 변환
    return bright_image

def display_combined_camera_feeds():
    process = (
            ffmpeg
            .input("pipe:0", framerate=30, format="rawvideo", pix_fmt="bgr24", s="2560x1440")
            .output(rtmp_url, format="flv", vcodec="libx264", preset="fast", crf=23, video_bitrate=3000)
            .run_async(pipe_stdin=True)
        )
    while True:
        # 각 카메라 이미지 읽기 (오프셋과 크기를 사용하여 각각의 카메라 데이터 읽기)
        cam1_image = read_from_shared_memory("CameraSharedMemory", 0, image_width * image_height * bytes_per_pixel)
        cam2_image = read_from_shared_memory("CameraSharedMemory", image_width * image_height * bytes_per_pixel, image_width * image_height * bytes_per_pixel)
        cam3_image = read_from_shared_memory("CameraSharedMemory", image_width * image_height * bytes_per_pixel * 2, image_width * image_height * bytes_per_pixel)
        cam4_image = read_from_shared_memory("CameraSharedMemory", image_width * image_height * bytes_per_pixel * 3, image_width * image_height * bytes_per_pixel)
        
        # 각 카메라 이미지를 640x360으로 리사이즈
        cam1_resized = cv2.resize(cam1_image, (1280, 720))
        cam2_resized = cv2.resize(cam2_image, (1280, 720))
        cam3_resized = cv2.resize(cam3_image, (1280, 720))
        cam4_resized = cv2.resize(cam4_image, (1280, 720))

        # 상단, 하단 각각 두 개의 영상을 합치기
        top_row = np.hstack((cam1_resized, cam2_resized))
        bottom_row = np.hstack((cam3_resized, cam4_resized))

        # 상단과 하단을 세로로 합쳐서 전체 화면 구성
        combined_image = np.vstack((top_row, bottom_row))
        combined_image = increase_brightness(combined_image)
        
        
        
        process.stdin.write(combined_image.tobytes())
        # 결합된 화면 출력
        cv2.imshow('Combined Camera Feeds', combined_image)

        # 'q' 키를 누르면 종료
        if cv2.waitKey(1) & 0xFF == ord('q'):
            break
    cv2.destroyAllWindows()

# 프로그램 실행
if __name__ == "__main__":
    display_combined_camera_feeds()
