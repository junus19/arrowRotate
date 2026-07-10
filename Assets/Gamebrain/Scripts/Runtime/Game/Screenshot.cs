using System;
using System.IO;
using UnityEngine;

public class Screenshot : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            Take();
        }
    }

    private void Take()
    {
        string folder = "Assets/Screenshots";

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        string filename =
            $"{folder}/capture_{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}.png";

        TakeTransparentScreenshot(Camera.main, Screen.width, Screen.height, filename);

        Debug.Log("Screenshot saved: " + filename);
    }

    public static void TakeTransparentScreenshot(
        Camera cam,
        int width,
        int height,
        string savePath)
    {
        // Backup
        RenderTexture previousRT = RenderTexture.active;
        RenderTexture previousCamRT = cam.targetTexture;
        CameraClearFlags previousFlags = cam.clearFlags;
        Color previousBg = cam.backgroundColor;

        // Transparent texture
        Texture2D tex = new Texture2D(
            width,
            height,
            TextureFormat.RGBA32,
            false
        );

        // Alpha destekli RT
        RenderTexture rt = RenderTexture.GetTemporary(
            width,
            height,
            24,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Linear
        );

        // Camera setup
        cam.targetTexture = rt;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0);

        // Render
        RenderTexture.active = rt;

        GL.Clear(true, true, new Color(0, 0, 0, 0));

        cam.Render();

        // Read pixels
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        // Save PNG
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(savePath, bytes);

        // Restore
        cam.targetTexture = previousCamRT;
        cam.clearFlags = previousFlags;
        cam.backgroundColor = previousBg;
        RenderTexture.active = previousRT;

        RenderTexture.ReleaseTemporary(rt);

        Destroy(tex);
    }
}