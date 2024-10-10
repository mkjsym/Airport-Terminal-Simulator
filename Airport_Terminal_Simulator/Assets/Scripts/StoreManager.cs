using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using UnityEngine;

public class CameraCaptureToSharedMemory : MonoBehaviour
{
    public RenderTexture[] renderTexture = new RenderTexture[4]; // 4개의 카메라 텍스처
    private const string sharedMemoryName = "CameraSharedMemory";
    private const int width = 1280;
    private const int height = 720;
    private const int mul = 4; // 4개의 카메라
    private MemoryMappedFile sharedMemory;
    private MemoryMappedViewAccessor accessor;

    void Start()
    {
        // 공유 메모리 생성 (4개의 카메라 데이터를 담을 크기로 설정)
        sharedMemory = MemoryMappedFile.CreateOrOpen(sharedMemoryName, width * height * 3 * mul);
        accessor = sharedMemory.CreateViewAccessor();
        
        Application.targetFrameRate = 120;
    }

    private void Update()
    {
        // 4개의 카메라에서 각각 이미지 데이터를 캡처하고 공유 메모리에 저장
        int offset = 0;
        for (int i = 0; i < renderTexture.Length; i++)
        {
            // 각 카메라의 RenderTexture를 활성화
            RenderTexture.active = renderTexture[i];
            Texture2D screenShot = new Texture2D(renderTexture[i].width, renderTexture[i].height, TextureFormat.RGB24, false);
            screenShot.ReadPixels(new Rect(0, 0, renderTexture[i].width, renderTexture[i].height), 0, 0);
            screenShot.Apply();
            RenderTexture.active = null;

            // 이미지 데이터를 바이트 배열로 변환
            byte[] imageData = screenShot.GetRawTextureData();

            // 공유 메모리에 이미지 데이터 저장 (카메라마다 offset을 다르게 설정)
            WriteToSharedMemory(imageData, offset);

            // 다음 카메라 데이터를 저장할 위치로 offset을 증가
            offset += imageData.Length;

            Destroy(screenShot);
        }
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
