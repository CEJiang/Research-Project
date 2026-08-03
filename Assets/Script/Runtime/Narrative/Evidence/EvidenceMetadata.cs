using System.Collections.Generic;

public class EvidenceMetadata
{
    public Evidence evidence;
    public Dictionary<string, float> previousHypothesisState;
    public Dictionary<string, float> updatedHypothesisState;
    public List<ClaimSelectionResult> claimResults;
        
    public override string ToString()
    {
        string result = $"Evidence: {evidence.displayNameEn}\nZone: {evidence.zoneAt}\nFacts: {evidence.facts}\n";

        result += "Previous Hypothesis State:\n";
        foreach (var kvp in previousHypothesisState)
        {
            result += $"- {kvp.Key}: {kvp.Value}\n";
        }

        result += "Updated Hypothesis State:\n";
        foreach (var kvp in updatedHypothesisState)
        {
            result += $"- {kvp.Key}: {kvp.Value}\n";
        }

        result += "Claim Results:\n";
        foreach (var claimResult in claimResults)
        {
            result += $"- Claim ID: {claimResult.claimID}, Confidence: {claimResult.confidence}, Reason: {claimResult.reason}\n";
        }

        return result;
    }
}

