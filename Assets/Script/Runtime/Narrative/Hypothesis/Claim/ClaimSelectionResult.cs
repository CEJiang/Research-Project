using System.Collections.Generic;

[System.Serializable]
public class ClaimSelectionResult
{
    public string claimID;
    public float confidence;
    public string informationValue; // The information value of the claim in reasoning to the evidence
    public List<string> basedFeatureIDs; // List of feature IDs that why LLM thinks this claim is relevant to the evidence
    public bool spatialContextUsed; // Whether the spatial context was used in the reasoning process
    public string reason;

    public float InformationValue => informationValue switch
    {
        "None" => 0.0f,
        "Weak" => 0.25f,
        "Moderate" => 0.5f,
        "Strong" => 0.75f,
        "Core" => 1.0f,
        _ => 0.0f
    };
}
