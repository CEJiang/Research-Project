using System;
using System.Collections.Generic;
using UnityEngine;

public class SceneSpatialContextDataLoader : Singleton<SceneSpatialContextDataLoader>
{
    public SceneSpatialContext SceneSpatialContext { get; private set; }

    public Dictionary<string, Zone> zoneDictionary = new();

    public List<Zone> zones;

    protected override void Awake()
    {
        base.Awake();
        LoadSceneSpatialContextDataFromJSON();
    }

    public void LoadSceneSpatialContextDataFromJSON()
    {
        TextAsset jsonTextFile = Resources.Load<TextAsset>("Data/SceneSpatialContext");

        if (jsonTextFile == null)
        {
            Debug.LogError("Failed to load Scene Spatial Context JSON file from Resources/Data/SceneSpatialContext.json");
            return;
        }

        SceneSpatialContext = JsonUtility.FromJson<SceneSpatialContext>(jsonTextFile.text);

        if (SceneSpatialContext == null || SceneSpatialContext.zones == null)
        {
            Debug.LogError("Scene Spatial Context JSON format is invalid.");
            return;
        }

        zones = SceneSpatialContext.zones;

        foreach (var zone in zones)
        {
            if (!zoneDictionary.ContainsKey(zone.zoneID))
            {
                zoneDictionary.Add(zone.zoneID, zone);
            }
            else
            {
                Debug.LogWarning($"Duplicate zoneID '{zone.zoneID}' found in Scene Spatial Context JSON.");
            }
        }
    }
}

[Serializable]
public class SceneSpatialContext
{
    public List<Zone> zones = new();
}