using System.Collections.Generic;
using UnityEngine;

public class EvidenceDataManager : Singleton<EvidenceDataManager>
{
    public Dictionary<string, List<Fact>> EvidenceFactsDictionary { get; private set; } = new();
    public Dictionary<string, Zone> EvidenceZoneDictionary { get; private set; } = new();

    protected override void Awake()
    {
        base.Awake();
        LoadEvidenceFactsFromJSON();
    }

    void Start()
    {
        List<NestedActionAutoRegister> allSemanticObjects = new List<NestedActionAutoRegister>(FindObjectsOfType<NestedActionAutoRegister>());
        foreach (var semanticObject in allSemanticObjects)
        {
            string evidenceName = semanticObject.displayNameEn;
            if (!EvidenceFactsDictionary.ContainsKey(evidenceName))
            {
                // Debug.LogWarning($"No facts found for evidence: {evidenceName}");
                continue;
            }

            semanticObject.facts = EvidenceFactsDictionary[evidenceName];
            semanticObject.zone = EvidenceZoneDictionary[evidenceName];
        }
    }

    public void LoadEvidenceFactsFromJSON()
    {
        TextAsset jsonTextFile = Resources.Load<TextAsset>("Data/EvidenceFacts");

        if (jsonTextFile == null)
        {
            Debug.LogError("Failed to load Evidence Facts JSON file from Resources/Data/EvidenceFacts.json");
            return;
        }

        EvidenceFactsList evidenceFactsList =
            JsonUtility.FromJson<EvidenceFactsList>(jsonTextFile.text);

        if (evidenceFactsList == null || evidenceFactsList.evidenceFactsList == null)
        {
            Debug.LogError("EvidenceFacts JSON format is invalid.");
            return;
        }

        EvidenceFactsDictionary.Clear();

        foreach (var evidenceFacts in evidenceFactsList.evidenceFactsList)
        {
            if (string.IsNullOrEmpty(evidenceFacts.evidenceName))
            {
                Debug.LogWarning("EvidenceFacts contains empty evidenceName.");
                continue;
            }

            EvidenceFactsDictionary[evidenceFacts.evidenceName] = evidenceFacts.facts;
            EvidenceZoneDictionary[evidenceFacts.evidenceName] = evidenceFacts.zone;
                Debug.Log($"Loaded facts for evidence: {evidenceFacts.evidenceName} with {evidenceFacts.facts.Count} facts and zone: {evidenceFacts.zone}");
        }

        Debug.Log($"Loaded evidence facts: {EvidenceFactsDictionary.Count}");
    }
}

[System.Serializable]
public class EvidenceFactsList
{
    public List<EvidenceFacts> evidenceFactsList;
}

[System.Serializable]
public class EvidenceFacts
{
    public string evidenceName;
    public List<Fact> facts;
    public Zone zone;
}