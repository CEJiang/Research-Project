using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 收集所有連續特徵與事件，供 Editor 可視化
/// </summary>
public class FeatureVisualizer : Singleton<FeatureVisualizer>
{
    [System.Serializable]
    public class FeatureSeries
    {
        public string name;
        public List<(float t, float v)> values = new();
    }

    public Dictionary<string, FeatureSeries> continuousFeatures = new();
    public List<(float t, string eventName)> eventMarkers = new();

    private const int maxSamples = 2000;

    public void AddFeature(string name, float value)
    {
        if (!continuousFeatures.ContainsKey(name))
            continuousFeatures[name] = new FeatureSeries { name = name };

        var list = continuousFeatures[name].values;
        list.Add((Time.realtimeSinceStartup, value));
        if (list.Count > maxSamples)
            list.RemoveAt(0);
    }

    public void AddEvent(string eventName)
    {
        eventMarkers.Add((Time.realtimeSinceStartup, eventName));
    }

    public Dictionary<string, FeatureSeries> GetAllFeatures() => continuousFeatures;
    public List<(float, string)> GetAllEvents() => eventMarkers;
}
