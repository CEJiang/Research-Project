using System.Collections.Generic;
using UnityEngine;

public class FeatureDataLoader : Singleton<FeatureDataLoader>
{
    public Dictionary<string, Feature> FeatureDictionary { get; private set; } = new();
    protected override void Awake()
    {
        base.Awake();
        LoadFeatureDataFromJSON();
    }
    void Start()
    {
        FeatureManager.Instance.SetFeatureDictionary(FeatureDictionary);
    }

    public void LoadFeatureDataFromJSON()
    {
        TextAsset jsonTextFile = Resources.Load<TextAsset>("Data/Features");

        if (jsonTextFile == null)
        {
            Debug.LogError("Failed to load Feature JSON file from Resources/Data/Features.json");
            return;
        }

         FeatureList featureList =
            JsonUtility.FromJson<FeatureList>(jsonTextFile.text);

        if (featureList == null || featureList.features == null)
        {
            Debug.LogError("Feature JSON format is invalid.");
            return;
        }

        FeatureDictionary.Clear();

        foreach (var feature in featureList.features)
        {
            if (string.IsNullOrEmpty(feature.featureID))
            {
                Debug.LogWarning("Feature contains empty featureID.");
                continue;
            }

            FeatureDictionary[feature.featureID] = ConvertFeature(feature);
        }

        Debug.Log($"Loaded features: {FeatureDictionary.Count}");
    }

    public Feature ConvertFeature(FeatureLoader loaders)
    {
        Dimension dimension = loaders.dimension switch
        {
            "Structure" => Dimension.Structure,
            "Surface" => Dimension.Surface,
            "Spatial" => Dimension.Spatial,
            "Interaction" => Dimension.Interaction,
            "Temporal" => Dimension.Temporal,
            _ => throw new System.ArgumentException($"Invalid dimension value: {loaders.dimension}")
        };

        return new Feature
        {
            featureID = loaders.featureID,
            description = loaders.description,
            dimension = dimension
        };
    }
}

[System.Serializable]
public class FeatureList
{
    public List<FeatureLoader> features;
}

[System.Serializable]
public class FeatureLoader
{
    public string featureID;
    public string description;
    public string dimension;
}