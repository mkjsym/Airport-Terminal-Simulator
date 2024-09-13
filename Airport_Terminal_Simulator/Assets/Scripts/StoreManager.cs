using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using UnityEngine;

public class CameraCaptureToSharedMemory : MonoBehaviour
{
    public RenderTexture[] renderTexture = new RenderTexture[4];
    private MemoryMappedFile[] ShareCam= new MemoryMappedFile[4] ; // 공유 메모리
    private MemoryMappedViewAccessor[] accessor = new MemoryMappedViewAccessor[4]; // 메모리 액세서
    private const int sharedMemorySize = 1920 * 1080 * 3; 

    void Start()
    {
        Application.targetFrameRate = 30;
        // 공유 메모리 생성 (이름은 다른 프로세스에서 참조할 때 필요함)
        ShareCam[0] = MemoryMappedFile.CreateOrOpen("ShareCam1", sharedMemorySize);
        ShareCam[1] = MemoryMappedFile.CreateOrOpen("ShareCam2", sharedMemorySize);
        ShareCam[2] = MemoryMappedFile.CreateOrOpen("ShareCam3", sharedMemorySize);
        ShareCam[3] = MemoryMappedFile.CreateOrOpen("ShareCam4", sharedMemorySize);

        accessor[0] = ShareCam[0].CreateViewAccessor();
        accessor[1] = ShareCam[1].CreateViewAccessor();
        accessor[2] = ShareCam[2].CreateViewAccessor();
        accessor[3] = ShareCam[3].CreateViewAccessor();
    }

    void Update()
    {
        for(int i=0;i<4;i++){
            CaptureAndWriteToSharedMemory(i);
        }
       
    }

    void CaptureAndWriteToSharedMemory(int i)
    {
        // RenderTexture에서 Texture2D로 변환
        RenderTexture.active = renderTexture[i];
        Texture2D capturedImage = new Texture2D(renderTexture[i].width, renderTexture[i].height, TextureFormat.RGB24, false);
        capturedImage.ReadPixels(new Rect(0, 0, renderTexture[i].width, renderTexture[i].height), 0, 0);
        capturedImage.Apply();

        // RenderTexture.active 해제
        RenderTexture.active = null;
        byte[] imageData = capturedImage.GetRawTextureData();
        Destroy(capturedImage); // 메모리 할당 해제 이거 안하면 메모리 터짐
        accessor[i].WriteArray(0, imageData, 0, imageData.Length);
    }

    void OnApplicationQuit()
    { 
        for(int i=0;i<4;i++){
            accessor[i]?.Dispose(); // 할당된 메모리 해제
            ShareCam[i]?.Dispose();
        }
        
    }
}