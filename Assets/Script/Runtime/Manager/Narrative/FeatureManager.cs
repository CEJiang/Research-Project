using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class FeatureManager : Singleton<FeatureManager>
{
    public Dictionary<string, Feature> FeatureDictionary { get; private set; } = new();

    public void SetFeatureDictionary(Dictionary<string, Feature> featureDictionary)
    {
        FeatureDictionary = featureDictionary;
    }

    public List<Feature> ExtractFeaturesFromEvidence(Evidence evidence)
    {
        List<Fact> facts = evidence.facts;
        List<Feature> features = new();

        foreach (var fact in facts)
        {
            if (FeatureDictionary.TryGetValue(fact.featureID, out Feature feature))
            {
                features.Add(feature);
            }
            else
            {
                Debug.LogWarning($"Feature with ID {fact.featureID} not found in FeatureDictionary.");
            }
        }

        return features;
    }
}
