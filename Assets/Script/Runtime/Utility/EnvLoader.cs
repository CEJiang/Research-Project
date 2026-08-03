using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EnvLoader
{
    private const string envFilePath = "OpenAI.env";
    private static Dictionary<string, string> envVariables = new();

    public static string GetValue(string key)
    {
        // Load the .env file if not already loaded
        if (!System.IO.File.Exists(envFilePath))
        {
            Debug.LogError($".env file not found at path: {envFilePath}");
            return null;
        }

        Logger.Log($"[EnvLoader] Loading .env file from path: {envFilePath}");

        var lines = System.IO.File.ReadAllLines(envFilePath);
        foreach (var line in lines)
        {
            if (line.StartsWith(key + ": "))
            {
                return line.Substring(key.Length + 2).Trim();
            }
        }

        Debug.LogWarning($"Key '{key}' not found in .env file.");
        return null;
    }
}
