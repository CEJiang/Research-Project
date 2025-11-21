#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class TimelineVisualizerWindow : EditorWindow
{
    [MenuItem("Tools/Timeline Visualizer")]
    public static void ShowWindow() => GetWindow<TimelineVisualizerWindow>("Timeline Visualizer");

    private float timeRange = 10f;
    private bool autoScroll = true;
    private bool pause = false;
    private Vector2 scrollPos;
    private float lastUpdateTime = 0f;
    private const float UPDATE_INTERVAL = 0.05f;
    private Dictionary<string, bool> featureFoldouts = new();

    private GUIStyle labelStyle;
    private GUIStyle headerStyle;
    private GUIStyle boxStyle;

    private void OnEnable()
    {

    }

    private void InitializeStyles()
    {
        // 在 OnGUI 內部，可以安全地存取 EditorStyles 和 GUI.skin
        labelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            fontSize = 10,
            normal = { textColor = Color.white }
        };

        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12
        };

        // 現在可以安全地存取 GUI.skin.box
        boxStyle = new GUIStyle(GUI.skin.box) 
        {
            normal = { background = Texture2D.grayTexture }
        };
}

    private void OnGUI()
    {
        if (labelStyle == null)
        {
            InitializeStyles();
        }

        GUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("🪶 Timeline Visualizer", headerStyle);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(pause ? "▶ Resume" : "⏸ Pause", GUILayout.Width(80)))
            pause = !pause;
        autoScroll = GUILayout.Toggle(autoScroll, "AutoScroll", "Button", GUILayout.Width(100));
        timeRange = EditorGUILayout.Slider("Time Window", timeRange, 2f, 60f);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        var fv = FeatureVisualizer.Instance;
        if (fv == null)
        {
            EditorGUILayout.HelpBox("No FeatureVisualizer found in the scene.", MessageType.Warning);
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        var features = fv.GetAllFeatures();
        var events = fv.GetAllEvents();

        DrawBackgroundGrid();

        foreach (var kv in features)
        {
            if (!featureFoldouts.ContainsKey(kv.Key))
                featureFoldouts[kv.Key] = true;

            featureFoldouts[kv.Key] = EditorGUILayout.Foldout(featureFoldouts[kv.Key], kv.Key, true);
            if (featureFoldouts[kv.Key])
            {
                Rect rect = GUILayoutUtility.GetRect(position.width - 40, 80);
                DrawFeatureGraph(rect, kv.Key, kv.Value.values);
            }
        }

        GUILayout.Space(10);
        DrawEventMarkers(events);
        EditorGUILayout.EndScrollView();

        if (!pause && Time.realtimeSinceStartup - lastUpdateTime > UPDATE_INTERVAL)
        {
            Repaint();
            lastUpdateTime = Time.realtimeSinceStartup;
        }
    }

    private void DrawBackgroundGrid()
    {
        Rect gridRect = GUILayoutUtility.GetRect(position.width - 20, 400);
        Handles.color = new Color(1f, 1f, 1f, 0.05f);
        for (int i = 0; i < gridRect.width; i += 50)
            Handles.DrawLine(new Vector3(gridRect.x + i, gridRect.y), new Vector3(gridRect.x + i, gridRect.yMax));
        for (int j = 0; j < gridRect.height; j += 25)
            Handles.DrawLine(new Vector3(gridRect.x, gridRect.y + j), new Vector3(gridRect.xMax, gridRect.y + j));
    }

    private void DrawFeatureGraph(Rect rect, string featureName, List<(float t, float v)> data)
    {
        if (data == null || data.Count < 2) return;

        float now = Time.realtimeSinceStartup;
        float minT = now - timeRange;
        float maxT = now;

        var recent = data.Where(p => p.t >= minT && p.t <= maxT).ToList();
        if (recent.Count < 2) return;

        float minV = recent.Min(p => p.v);
        float maxV = recent.Max(p => p.v);
        if (Mathf.Approximately(minV, maxV)) maxV += 0.01f;

        Handles.BeginGUI();
        Handles.color = FeatureColorUtility.GetColor(featureName);
        Vector3 prev = MapToGraph(rect, recent[0].t, recent[0].v, minT, maxT, minV, maxV);

        for (int i = 1; i < recent.Count; i++)
        {
            Vector3 next = MapToGraph(rect, recent[i].t, recent[i].v, minT, maxT, minV, maxV);
            Handles.DrawAAPolyLine(2f, prev, next);
            prev = next;
        }

        Handles.color = new Color(1f, 1f, 1f, 0.1f);
        Handles.DrawLine(new Vector3(rect.x, rect.yMax), new Vector3(rect.xMax, rect.yMax));
        Handles.EndGUI();

        EditorGUI.LabelField(rect, $"{featureName}  [{minV:F2} ~ {maxV:F2}]", labelStyle);
    }

    private Vector3 MapToGraph(Rect rect, float t, float v, float minT, float maxT, float minV, float maxV)
    {
        float x = Mathf.Lerp(rect.x, rect.xMax, (t - minT) / (maxT - minT));
        float y = Mathf.Lerp(rect.yMax, rect.y, Mathf.InverseLerp(minV, maxV, v));
        return new Vector3(x, y, 0);
    }

    private void DrawEventMarkers(List<(float, string)> events)
    {
        if (events == null || events.Count == 0) return;

        float now = Time.realtimeSinceStartup;
        float minT = now - timeRange;
        float maxT = now;

        EditorGUILayout.LabelField("Event Timeline", EditorStyles.boldLabel);
        Rect rect = GUILayoutUtility.GetRect(position.width - 40, 60);
        EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));

        foreach (var e in events.Where(ev => ev.Item1 >= minT && ev.Item1 <= maxT))
        {
            float x = Mathf.Lerp(rect.x, rect.xMax, (e.Item1 - minT) / (maxT - minT));
            Handles.color = GetEventColor(e.Item2);
            Handles.DrawLine(new Vector3(x, rect.y), new Vector3(x, rect.yMax));
            Handles.Label(new Vector3(x + 2, rect.y + 5), e.Item2, labelStyle);
        }
    }

    private Color GetEventColor(string eventName)
    {
        if (eventName.Contains("InnerVoice")) return new Color(1f, 0.8f, 0.1f);
        if (eventName.Contains("Object")) return new Color(0.3f, 0.8f, 1f);
        if (eventName.Contains("Door")) return new Color(0.7f, 1f, 0.7f);
        return Color.gray;
    }
}
#endif
