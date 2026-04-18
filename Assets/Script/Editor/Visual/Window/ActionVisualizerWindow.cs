using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ActionVisualizerWindow : EditorWindow
{
    private string[] playerFolders;
    private int selectedPlayerIndex = -1;

    private string[] jsonFiles;
    private int selectedJsonIndex = -1;

    private float sidebarWidthRatio = 0.25f;
    private string lastFilePath = null;

    // Editor Pref Keys
    private const string PREF_PLAYER_INDEX = "ActionVis_PlayerIndex";
    private const string PREF_JSON_INDEX = "ActionVis_JsonIndex";
    private const string PREF_LAST_PATH = "ActionVis_LastPath";

    [MenuItem("Tools/Action Visualizer")]
    public static void ShowWindow()
    {
        var window = GetWindow<ActionVisualizerWindow>("Action Visualizer");
        window.minSize = new Vector2(1000, 600);
    }

    private void OnEnable()
    {
        RefreshPlayers();

        // Restore saved indexes
        selectedPlayerIndex = EditorPrefs.GetInt(PREF_PLAYER_INDEX, -1);
        selectedJsonIndex = EditorPrefs.GetInt(PREF_JSON_INDEX, -1);
        lastFilePath = EditorPrefs.GetString(PREF_LAST_PATH, null);

        // Restore JSON list
        if (selectedPlayerIndex >= 0 && selectedPlayerIndex < playerFolders.Length)
        {
            RefreshJsonFiles();
        }

        // Restore the timeline JSON file
        if (!string.IsNullOrEmpty(lastFilePath))
        {
            ActionVisualizer.LoadJsonData(lastFilePath);
            Repaint();
        }
    }

    private void RefreshPlayers()
    {
        string logsRoot = $"{Application.dataPath}/Logs";

        if (!Directory.Exists(logsRoot))
        {
            playerFolders = new string[0];
            return;
        }

        playerFolders = Directory.GetDirectories(logsRoot);
        for (int i = 0; i < playerFolders.Length; i++)
            playerFolders[i] = Path.GetFileName(playerFolders[i]);
    }

    private void RefreshJsonFiles()
    {
        if (selectedPlayerIndex < 0)
        {
            jsonFiles = new string[0];
            return;
        }

        string playerPath = $"{Application.dataPath}/Logs/{playerFolders[selectedPlayerIndex]}";
        string[] files = Directory.GetFiles(playerPath, "*.json");

        jsonFiles = new string[files.Length];
        for (int i = 0; i < files.Length; i++)
            jsonFiles[i] = Path.GetFileName(files[i]);
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();

        // LEFT SIDEBAR
        GUILayout.BeginVertical(GUILayout.Width(position.width * sidebarWidthRatio));
        DrawLeftSidebar();
        GUILayout.EndVertical();
        

        // TIMELINE PANEL
        GUILayout.BeginVertical(GUILayout.Width(position.width * (1 - sidebarWidthRatio)));
        DrawTimelinePanel();
        GUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    void DrawLeftSidebar()
    {
        GUILayout.Label("Player Selector", EditorStyles.boldLabel);

        // PLAYER DROPDOWN
        if (playerFolders.Length == 0)
        {
            GUILayout.Label("No players found.");
        }
        else
        {
            int newPlayerIndex = EditorGUILayout.Popup("Player", selectedPlayerIndex, playerFolders);

            if (newPlayerIndex != selectedPlayerIndex)
            {
                selectedPlayerIndex = newPlayerIndex;
                EditorPrefs.SetInt(PREF_PLAYER_INDEX, selectedPlayerIndex);

                selectedJsonIndex = -1;
                EditorPrefs.SetInt(PREF_JSON_INDEX, selectedJsonIndex);

                RefreshJsonFiles();
            }
        }

        GUILayout.Space(10);
        GUILayout.Label("JSON Files", EditorStyles.boldLabel);

        // JSON LIST
        if (jsonFiles == null || jsonFiles.Length == 0)
        {
            GUILayout.Label("No JSON files.");
        }
        else
        {
            int newJsonIndex = GUILayout.SelectionGrid(
                selectedJsonIndex,
                jsonFiles,
                1,
                EditorStyles.miniButton
            );

            if (newJsonIndex != selectedJsonIndex)
            {
                selectedJsonIndex = newJsonIndex;
                EditorPrefs.SetInt(PREF_JSON_INDEX, selectedJsonIndex);

                string selectedPlayer = playerFolders[selectedPlayerIndex];
                string selectedJson = jsonFiles[selectedJsonIndex];
                string path = $"{selectedPlayer}/{selectedJson}";

                lastFilePath = path;
                EditorPrefs.SetString(PREF_LAST_PATH, lastFilePath);

                ActionVisualizer.LoadJsonData(path);

                Repaint();
                SceneView.RepaintAll();
            }
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Refresh Players"))
            RefreshPlayers();
    }

    void DrawTimelinePanel()
    {
        GUILayout.Label("Action Timeline", EditorStyles.boldLabel);

        var actions = ActionVisualizer.actionsToVisualize;

        if (actions == null || actions.Count == 0)
        {
            GUILayout.Label("No actions loaded.");
            return;
        }

        float minTime = float.MaxValue;
        float maxTime = float.MinValue;

        foreach (var a in actions)
        {
            minTime = Mathf.Min(minTime, a.timestamp);
            maxTime = Mathf.Max(maxTime, a.timestamp);
        }

        float width = position.width * 0.7f;
        float height = 60f;

        Rect rect = GUILayoutUtility.GetRect(width, height);
        EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));

        foreach (var a in actions)
        {
            float t = Mathf.InverseLerp(minTime, maxTime, a.timestamp);
            float x = rect.x + t * rect.width;

            Rect dot = new Rect(x - 2, rect.y + rect.height / 2 - 2, 4, 4);
            EditorGUI.DrawRect(dot, Color.white);
        }

        GUILayout.Label($"Range: {minTime:F2} → {maxTime:F2}");
    }
}