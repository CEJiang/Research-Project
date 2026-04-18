using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SnapshotEvidenceUtility
{
    // 你的主攝影機（可以外部指定，也可以用 Camera.main）
    public static Camera captureCamera;

    static void Awake()
    {
        if (captureCamera == null)
        {
            captureCamera = Camera.main;
        }
    }

    public static string SnapshotEvidence()
    {
        if (captureCamera == null)
        {
            captureCamera = Camera.main;
            if (captureCamera == null)
            {
                Debug.LogError("[Snapshot] No camera assigned!");
                return "";
            }
        }

        // 1. 建立 RenderTexture
        RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24);
        captureCamera.targetTexture = rt;

        // 2. 將畫面渲染進 RenderTexture
        Texture2D snapshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        captureCamera.Render();

        // 3. 讀取像素
        RenderTexture.active = rt;
        snapshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        snapshot.Apply();

        // 4. 清理
        captureCamera.targetTexture = null;
        RenderTexture.active = null;
        Object.Destroy(rt);

        // 5. 產生檔案路徑
        string folder = Path.Combine(Application.dataPath, "Resources/Snapshots");
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string filename = $"snapshot_{System.DateTime.Now:yyyyMMdd_HHmmssfff}.png";
        string path = Path.Combine(folder, filename);

        // 6. 寫出 PNG 檔案
        byte[] bytes = snapshot.EncodeToPNG();
        File.WriteAllBytes(path, bytes);

        Debug.Log("[Snapshot] Saved: " + path);

        return path;
    }
}
