using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ActionVisualizer
{
#if UNITY_EDITOR
    public static List<SemanticAction> actionsToVisualize = new();

    public static void LoadJsonData(string filePath)
    {
        string json = System.IO.File.ReadAllText($"{Application.dataPath}/Logs/{filePath}");
        var actionArray = JsonUtility.FromJson<SemanticActionArray>(json);
        actionsToVisualize = actionArray.actions;
    }
#endif    
}