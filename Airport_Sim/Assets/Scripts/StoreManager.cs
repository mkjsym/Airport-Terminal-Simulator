using System;
using System.IO.MemoryMappedFiles;
using System.Threading.Tasks;
using UnityEngine;

public class CameraCaptureToSharedMemory : MonoBehaviour
{
    public RenderTexture[] renderTextures = new RenderTexture[4];
    private const string sharedMemoryName = "CameraSharedMemory";
    private const int width = 1280;
    private const int height = 720;
    private const int bytesPerPixel = 4; // RGBA 형식의 4바이트
    private MemoryMappedFile sharedMemory;
    private MemoryMappedViewAccessor accessor;

    void Start()
    {
        // 공유 메모리 생성 (4개의 카메라 데이터를 담을 크기로 설정)
        sharedMemory = MemoryMappedFile.CreateOrOpen(sharedMemoryName, width * height * bytesPerPixel * renderTextures.Length);
        accessor = sharedMemory.CreateViewAccessor();
        Application.targetFrameRate = 120;
    }

    private void Update()
    {
        // 4개의 카메라에서 각각 이미지 데이터를 캡처하고 비동기로 공유 메모리에 저장
        for (int i = 0; i < renderTextures.Length; i++)
        {
            int offset = i * width * height * bytesPerPixel;
            CaptureAndWriteImage(renderTextures[i], offset);
        }
    }

    private void CaptureAndWriteImage(RenderTexture renderTexture, int offset)
    {
        // RenderTexture에서 이미지 캡처
        RenderTexture.active = renderTexture;
        Texture2D screenShot = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
        screenShot.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        screenShot.Apply();
        RenderTexture.active = null;

        // 이미지 데이터를 비동기적으로 메모리에 기록
        byte[] imageData = screenShot.GetRawTextureData();
        Task.Run(() => WriteToSharedMemory(imageData, offset));

        Destroy(screenShot); // 메모리 해제
    }

    private void WriteToSharedMemory(byte[] imageData, int offset)
    {
        try
        {
            // 공유 메모리의 지정된 offset 위치에 이미지 데이터 쓰기
            accessor.WriteArray(offset, imageData, 0, imageData.Length);
            Debug.Log("Image data written to shared memory at offset: " + offset);
        }
        catch (Exception e)
        {
            Debug.LogError("Shared memory write error: " + e.Message);
        }
    }

    void OnApplicationQuit()
    {
        // 공유 메모리 해제
        accessor?.Dispose();
        sharedMemory?.Dispose();
    }
}
