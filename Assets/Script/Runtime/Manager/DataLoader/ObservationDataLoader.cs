using System.Collections.Generic;
using UnityEngine;

public class ObservationDataLoader : Singleton<ObservationDataLoader>
{
    public Dictionary<string, List<ObservationCandidate>> observationDictionary { get; private set; } = new();
    public Dictionary<string, string> zoneDictionary { get; private set; } = new();
    protected override void Awake()
    {
        base.Awake();
        LoadObservationsFromJSON();
    }

    void Start()
    {
        List<NestedActionAutoRegister> allNestedSemanticObjects = new(FindObjectsOfType<NestedActionAutoRegister>());
        foreach (var semanticObject in allNestedSemanticObjects)
        {
            string evidenceName = semanticObject.displayNameEn;
            if (!observationDictionary.ContainsKey(evidenceName))
            {
                continue;
            }

            List<ObservationCandidate> randomizedCandidates = new(observationDictionary[evidenceName]);

            // Shuffle(randomizedCandidates);

            semanticObject.observationCandidates = randomizedCandidates;
            semanticObject.zone = SceneSpatialContextDataLoader.Instance.zoneDictionary.GetValueOrDefault(zoneDictionary.GetValueOrDefault(evidenceName));
        }

        List<SemanticActionObject> allSemanticObjects = new List<SemanticActionObject>(FindObjectsOfType<SemanticActionObject>());
        foreach (var semanticObject in allSemanticObjects)
        {
            string evidenceName = semanticObject.displayNameEn;
            if (!observationDictionary.ContainsKey(evidenceName))
            {
                continue;
            }

            // Randomize the order of observation candidates for each semantic object
            List<ObservationCandidate> randomizedCandidates =
                new(observationDictionary[evidenceName]);

            // Shuffle(randomizedCandidates);

            semanticObject.observationCandidates = randomizedCandidates;
            semanticObject.zone = SceneSpatialContextDataLoader.Instance.zoneDictionary.GetValueOrDefault(zoneDictionary.GetValueOrDefault(evidenceName));
        }
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public void LoadObservationsFromJSON()
    {
        TextAsset jsonTextFile = Resources.Load<TextAsset>("Data/Observations");

        if (jsonTextFile == null)
        {
            Debug.LogError("Failed to load  Observations JSON file from Resources/Data/Observations.json");
            return;
        }

        ObservationsList evidenceObservationsList =
            JsonUtility.FromJson<ObservationsList>(jsonTextFile.text);

        if (evidenceObservationsList == null || evidenceObservationsList.evidenceObservationsList == null)
        {
            Debug.LogError("Observations JSON format is invalid.");
            return;
        }

        observationDictionary.Clear();

        foreach (var evidenceObservations in evidenceObservationsList.evidenceObservationsList)
        {
            if (string.IsNullOrEmpty(evidenceObservations.evidenceName))
            {
                Debug.LogWarning("Observations contains empty evidenceName.");
                continue;
            }

            observationDictionary[evidenceObservations.evidenceName] = ConvertObservationCandidate(evidenceObservations.observationCandidates);
            zoneDictionary[evidenceObservations.evidenceName] = evidenceObservations.zone;
        }

        Debug.Log($"Loaded evidence observation candidates: {observationDictionary.Count}");
    }

    public List<ObservationCandidate> ConvertObservationCandidate(List<ObservationCandidateLoader> loaders)
    {
        List<ObservationCandidate> candidates = new();

        foreach (var loader in loaders)
        {
            Dimension dimension = loader.dimension switch
            {
                "Structure" => Dimension.Structure,
                "Surface" => Dimension.Surface,
                "Spatial" => Dimension.Spatial,
                "Interaction" => Dimension.Interaction,
                "Temporal" => Dimension.Temporal,
                _ => throw new System.NotImplementedException($"Dimension '{loader.dimension}' is not implemented.")
            };

            ObservationCandidateType candidateType = loader.candidateType switch
            {
                "TrueFact" => ObservationCandidateType.TrueFact,
                "CounterfactualDistractor" => ObservationCandidateType.CounterfactualDistractor,
                "ImplausibleDistractor" => ObservationCandidateType.ImplausibleDistractor,
                _ => throw new System.NotImplementedException($"ObservationCandidateType '{loader.candidateType}' is not implemented.")
            };

            candidates.Add(new ObservationCandidate(
                loader.candidateID,
                dimension,
                loader.descriptionZh,
                loader.descriptionEn,
                loader.featureID,
                candidateType
            ));
        }

        return candidates;
    }
}

[System.Serializable]
public class ObservationsList
{
    public List<Observations> evidenceObservationsList;
}

[System.Serializable]
public class Observations
{
    public string evidenceName;
    public List<ObservationCandidateLoader> observationCandidates;
    public string zone;
}
[System.Serializable]
public class ObservationCandidateLoader
{
    public string candidateID;
    public string dimension;
    public string descriptionZh;
    public string descriptionEn;
    public string featureID;
    public string candidateType;
}