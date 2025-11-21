using System;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "LoggerSettings", menuName = "Utility/LoggerSettings")]
public class Logger : ScriptableObject
{

#region Instance
    const string PATH_FULL = @"Assets/Settings/LoggerSettings.asset";

    static Logger instance;
    public static Logger Instance
    {
        get
        {
            // Not initialized yet
            if (instance == null)
            {
                // Try to load it from the asset database
#if UNITY_EDITOR
                instance = AssetDatabase.LoadAssetAtPath<Logger>(PATH_FULL);
#endif

                // If there is none in the asset database, create a new one
                if (instance == null)
                {
                    Debug.Log("LoggerSettings.asset is not found. Creating a new one. Path: " + PATH_FULL);
                    instance = CreateInstance<Logger>();
#if UNITY_EDITOR
                    AssetDatabase.CreateAsset(instance, PATH_FULL);
                    AssetDatabase.SaveAssets();
#endif
                }
            }
            return instance;
        }
    }
#endregion

#region Settings
    [Serializable]
    public struct LogActivations
    {
        public bool normalOn;
        public bool warningOn;
        public bool errorOn;
        public bool developerOn;
    }

    [Header("Log Activations")]
    [SerializeField] LogActivations activations;
    bool NormalOn => activations.normalOn;
    bool WarningOn => activations.warningOn;
    bool ErrorOn => activations.errorOn;
    bool DeveloperOn => activations.developerOn;

    [Serializable]
    public struct LogColors
    {
        public Color normalColor;
        public Color warningColor;
        public Color errorColor;
        public Color developerColor;
    }

    [Header("Log Colors")]
    [SerializeField] LogColors colors;
    Color NormalColor => colors.normalColor;
    Color WarningColor => colors.warningColor;
    Color ErrorColor => colors.errorColor;
    Color DeveloperColor => colors.developerColor;
#endregion

#region Log Methods
    public static void Log(string message)
    {
        if (!Instance.NormalOn) return;

        string logMessage = $"[Normal] {message}";
        LogWithColor(logMessage, Instance.NormalColor);
    }
    
    
    public static void Log(object sender, string message)
    {
        if (!Instance.NormalOn) return;

        string name = sender.GetType().Name;
        string logMessage = $"[Normal] - <b>[{name}]</b>  - {message}";
        LogWithColor(logMessage, Instance.NormalColor);
    }

    public static void Warning(string message)
    {
        if (!Instance.WarningOn) return;

        string logMessage = $"[Warning] {message}";
        LogWithColor(logMessage, Instance.WarningColor);
    }

    public static void Warning(object sender, string message)
    {
        if (!Instance.WarningOn) return;
        
        string name = sender.GetType().Name;
        string logMessage = $"[Warning] - <b>[{name}]</b>  - {message}";
        LogWithColor(logMessage, Instance.WarningColor);
    }

    public static void Error(string message)
    {
        if (!Instance.ErrorOn) return;

        string logMessage = $"[Error] {message}";
        LogWithColor(logMessage, Instance.ErrorColor);
    }

    public static void Error(object sender, string message)
    {
        if (!Instance.ErrorOn) return;
        
        string name = sender.GetType().Name;
        string logMessage = $"[Error] - <b>[{name}]</b>  - {message}";
        LogWithColor(logMessage, Instance.ErrorColor);
    }

    public static void Developer(string message)
    {
        if (!Instance.DeveloperOn) return;

        string logMessage = $"[Developer] {message}";
        LogWithColor(logMessage, Instance.DeveloperColor);
    }

    public static void Developer(object sender, string message)
    {
        if (!Instance.DeveloperOn) return;
        
        string name = sender.GetType().Name;
        string logMessage = $"[Developer] - <b>[{name}]</b>  - {message}";
        LogWithColor(logMessage, Instance.DeveloperColor);
    }
#endregion

#region Others
    // private static string B(string message)
    // {
    //     return $"<b>[{message}]</b>";
    // }

    private static string Hex(Color color)
    {
        return ColorUtility.ToHtmlStringRGBA(color);
    }

    private static void LogWithColor(string message, Color color)
    {
        Debug.Log($"<color=#{Hex(color)}>{message}</color>");
    }
#endregion

}