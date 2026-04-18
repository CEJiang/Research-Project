using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ClaimManager : Singleton<ClaimManager>
{
    public List<Claim> claims = new();
    public Dictionary<string, Claim> claimDictionary = new();

    void Start()
    {
        LoadClaims();

        Logger.Log(this, "Claim Manager Initialized");
    }


    public void LoadClaims()
    {
        claims.Clear();
        Claim[] loadedClaims = Resources.LoadAll<Claim>("Claims");
        claims.AddRange(loadedClaims);

        claimDictionary.Clear();
        foreach (var claim in claims)
        {
            claimDictionary[claim.id] = claim;
        }
    }

    public string GetClaims()
    {
        string claimText = "";
        foreach (var claim in claims)
        {
            claimText += $"- ID: {claim.id} Description: {claim.description}\n";
        }
        return claimText;
    }

    public List<ClaimSelectionResult> EvaluateEvidenceClaimsAsync(List<FeatureSelectionResult> featureResults)
    {
        List<ClaimSelectionResult> claimResults = new();

        foreach (var s in featureResults)
        {
            for (int i = 0; i < claims.Count; i++)
            {
                var claim = claims[i];
                if (claim.requiredFeatures != null && claim.requiredFeatures.Contains(s.featureId))
                {
                    Debug.Log($"[FeatureSelectionManager] Feature '{s.featureId}' matched for Claim '{claim.id}' with strength {s.strength} and reason: {s.reason}");
                    claimResults.Add(new ClaimSelectionResult
                    {
                        claimId = claim.id,
                        polarity = s.polarity,
                        strength = s.strength,
                        reason = s.reason
                    });
                }
            }
        }

        Debug.Log("[FeatureSelectionManager] Parsed " + claimResults.Count + " claim results from " + featureResults.Count + " feature results.");
        return claimResults;
    }
}
