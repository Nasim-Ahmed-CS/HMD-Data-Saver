using System;
using System.IO;
using UnityEngine;

public class DeviceFrameCapture : MonoBehaviour
{
    [Header("Capture Configuration")]
    public Camera sourceCamera;
    public string participantId = "Participant_01";
    public string subfolderName = "DeviceFrames";
    public int frameRate = 15;
    public int captureWidth = 1920;
    public int captureHeight = 1920;
    public bool savePNG = true;
    public bool saveJPG = false;

    [Header("Capture State")]
    public bool isCapturing = false;

    private string outputFolder;
    private float captureInterval;
    private float nextCaptureTime;
    private int frameIndex = 0;

    private void Awake()
    {
        if (sourceCamera == null)
        {
            sourceCamera = Camera.main;
        }

        InitializeOutputFolder();
        captureInterval = 1f / Mathf.Max(1, frameRate);
    }

    private void Update()
    {
        if (!isCapturing || sourceCamera == null)
            return;

        if (Time.unscaledTime < nextCaptureTime)
            return;

        nextCaptureTime = Time.unscaledTime + captureInterval;
        SaveCurrentFrame();
    }

    private void InitializeOutputFolder()
    {
        string rootFolder = Path.Combine(Application.persistentDataPath, participantId, subfolderName);

        if (!Directory.Exists(rootFolder))
        {
            Directory.CreateDirectory(rootFolder);
        }

        outputFolder = rootFolder;
    }

    public void StartCapture()
    {
        isCapturing = true;
        nextCaptureTime = Time.unscaledTime;
        frameIndex = 0;
    }

    public void StopCapture()
    {
        isCapturing = false;
    }

    private void SaveCurrentFrame()
    {
        if (sourceCamera == null)
            return;

        RenderTexture renderTexture = new RenderTexture(captureWidth, captureHeight, 24);
        RenderTexture previous = sourceCamera.targetTexture;
        sourceCamera.targetTexture = renderTexture;
        sourceCamera.Render();

        Texture2D texture = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
        texture.Apply();

        sourceCamera.targetTexture = previous;
        RenderTexture.active = null;
        Destroy(renderTexture);

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        string fileName = $"frame_{frameIndex:00000}_{timestamp}";

        if (savePNG)
        {
            byte[] png = texture.EncodeToPNG();
            File.WriteAllBytes(Path.Combine(outputFolder, fileName + ".png"), png);
        }

        if (saveJPG)
        {
            byte[] jpg = texture.EncodeToJPG();
            File.WriteAllBytes(Path.Combine(outputFolder, fileName + ".jpg"), jpg);
        }

        Destroy(texture);
        frameIndex++;
    }
}
