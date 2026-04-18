using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class ImageConverter
{
    public static Sprite LoadRuntimeSprite(string imagePath)
    {
        if (!File.Exists(imagePath))
        {
            Debug.LogError("Image not found: " + imagePath);
            return null;
        }

        byte[] bytes = File.ReadAllBytes(imagePath);
        Texture2D tex = new(2, 2);
        tex.LoadImage(bytes);
        
        return Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f)
        );
    }
}
