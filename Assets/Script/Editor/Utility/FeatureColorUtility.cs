#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

public static class FeatureColorUtility
{
    private static readonly Dictionary<string, Color> colorMap = new();

    private static readonly Color[] palette =
    {
        new Color(0.95f, 0.4f, 0.4f),
        new Color(0.4f, 0.9f, 0.5f),
        new Color(0.4f, 0.6f, 1.0f),
        new Color(0.9f, 0.8f, 0.3f),
        new Color(0.8f, 0.5f, 0.9f),
        new Color(0.5f, 0.8f, 0.9f),
        new Color(0.95f, 0.6f, 0.2f),
    };

    public static Color GetColor(string key)
    {
        if (!colorMap.ContainsKey(key))
        {
            colorMap[key] = palette[colorMap.Count % palette.Length];
        }
        return colorMap[key];
    }
}
#endif
