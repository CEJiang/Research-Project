using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class ClaimManager : Singleton<ClaimManager>
{
    public List<Claim> claims = new();
    public async Task<List<ClaimSelectionResult>> EvaluateEvidenceClaimsAsync(Evidence evidence)
    {
        List<ClaimSelectionResult> claimResults = await ClaimSelectionManager.Instance.GenerateClaimSelection(evidence);
        evidence.SetClaimResults(claimResults);
        return claimResults;
    }

    public string GetClaimsAsStringForLLM()
    {
        StringBuilder sb = new();

        foreach (var claim in claims)
        {
            sb.AppendLine($"- Claim ID: {claim.claimID}, Description: {claim.description}, Information Value: {claim.informationValue}");
        }

        return sb.ToString();
    }
}
