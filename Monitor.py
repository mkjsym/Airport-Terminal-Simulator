import cv2
import numpy as np
import mmap

# 공유 메모리 크기 (RGB 이미지의 크기: 1920 * 1080 * 3)
image_width = 1920
image_height = 1080
shared_memory_size = image_width * image_height * 3

# 네 개의 공유 메모리에서 데이터를 읽어오는 함수
def read_from_shared_memory(memory_name):
    # Windows에서 태그 기반으로 메모리 매핑
    mm = mmap.mmap(0, shared_memory_size, tagname=memory_name)
    image_data = mm.read(shared_memory_size)
    mm.close()
    
    # 읽은 데이터를 NumPy 배열로 변환
    image_np = np.frombuffer(image_data, dtype=np.uint8)
    image_np = image_np.reshape((image_height, image_width, 3))
    image_np = cv2.cvtColor(image_np,cv2.COLOR_BGR2RGB)
    image_np=cv2.flip(image_np, 0)
    return image_np

# 네 개의 카메라 영상을 합쳐서 한 화면에 출력하는 함수
def display_combined_camera_feeds():
    while True:
        # 각 공유 메모리에서 카메라 이미지 읽기
        cam1_image = read_from_shared_memory("ShareCam1")
        cam2_image = read_from_shared_memory("ShareCam2")
        cam3_image = read_from_shared_memory("ShareCam3")
        cam4_image = read_from_shared_memory("ShareCam4")

        # 각 카메라의 이미지를 크기를 절반으로 축소
        cam1_resized = cv2.resize(cam1_image, (960, 540))
        cam2_resized = cv2.resize(cam2_image, (960, 540))
        cam3_resized = cv2.resize(cam3_image, (960, 540))
        cam4_resized = cv2.resize(cam4_image, (960, 540))

        # 영상 2*2 놓기
        top_row = np.hstack((cam1_resized, cam2_resized))
        bottom_row = np.hstack((cam3_resized, cam4_resized))

        # 전체 화면 구성
        combined_image = np.vstack((top_row, bottom_row))

        # 이미지 출력
        cv2.imshow('Combined Camera Feeds', combined_image)
        
        # 'q' 키를 누르면 종료
        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

    cv2.destroyAllWindows()

# 프로그램 실행
if __name__ == "__main__":
    display_combined_camera_feeds()
