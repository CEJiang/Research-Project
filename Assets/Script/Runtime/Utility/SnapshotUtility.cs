using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public static class SnapshotUtility
{
    public static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    public static Texture2D GetSnapshot(string objectName)
    {
        string path = System.IO.Path.Combine(Application.dataPath, "Resources", "Snapshots", $"{objectName}.png");
        if (!System.IO.File.Exists(path)) return null;

        byte[] bytes = System.IO.File.ReadAllBytes(path);
        Texture2D tex = new(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(bytes);
        return tex;
    }


    public static Task CaptureSnapshot(GameObject item)
    {
        SemanticActionObject semanticActionObject = item.GetComponent<SemanticActionObject>();
        
        string filePath = System.IO.Path.Combine(Application.dataPath, "Resources", "Snapshots", $"{semanticActionObject.displayNameEn}.png");

        if (!System.IO.File.Exists(filePath))
        {
            // Move the item to a dedicated snapshot area
            GameObject itemPreviewRoot = GameObject.Find("ItemPreviewRoot");
            item.transform.SetParent(itemPreviewRoot.transform);

            // Position the item appropriately
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.Euler(30, 45, 0);

            // Create a RenderTexture
            RenderTexture rt = new(256, 256, 24, RenderTextureFormat.ARGB32);
            Camera snapshotCamera = itemPreviewRoot.GetComponentInChildren<Camera>();
            snapshotCamera.targetTexture = rt;
            snapshotCamera.Render();

            // Activate the RenderTexture and read the pixels
            RenderTexture.active = rt;
            Texture2D snapshot = new(256, 256, TextureFormat.RGBA32, false);
            snapshot.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
            snapshot.Apply();

            // Clean up
            snapshotCamera.targetTexture = null;
            RenderTexture.active = null;
            Object.Destroy(rt);

            SaveSnapshotAsPNG(snapshot, filePath);
        }

        return Task.CompletedTask;
    }

    public static void SaveSnapshotAsPNG(Texture2D snapshot, string filePath)
    {
        byte[] bytes = snapshot.EncodeToPNG();
        System.IO.File.WriteAllBytes(filePath, bytes);

        #if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
        #endif

        Debug.Log($"Snapshot saved to {filePath}");
    }
}
