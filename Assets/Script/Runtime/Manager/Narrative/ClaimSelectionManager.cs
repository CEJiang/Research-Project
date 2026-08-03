using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ClaimSelectionManager : Singleton<ClaimSelectionManager>
{
    public const string CLAIM_SELECTION_TEMPLATE_PATH = "Prompts/ClaimSelectionTemplate";

    [Header("Prompt")]
    [SerializeField] private SimplePromptTemplate claimSelectionTemplate;
    void Start()
    {
        var claimSelectionTemplateAsset = Instantiate(Resources.Load<SimplePromptTemplateAsset>(CLAIM_SELECTION_TEMPLATE_PATH));
        claimSelectionTemplateAsset.ApplyTo(claimSelectionTemplate);

    }

    public Task<List<ClaimSelectionResult>> GenerateClaimSelection(Evidence evidence)
    {
        Debug.Log("Generating Claim Selection...");
        return HandleSynthesisNarratorAsync(evidence);
    }

    private async Task<List<ClaimSelectionResult>> HandleSynthesisNarratorAsync(Evidence evidence)
    {
        string prompt = BuildClaimSelectionPrompt(evidence);
        Debug.Log("[ClaimSelectionManager] Prompt: " + prompt);

        string response = await TaskLLMManager.Instance.RunTask(
            prompt
        );

        Debug.Log("[ClaimSelectionManager] LLM Response: " + response);

        // // Parse the LLM response into a ClaimSelectionResult
        var claimSelectionResult = ParseLLMResponse(response);
        return claimSelectionResult;
    }

    private string BuildClaimSelectionPrompt(Evidence evidence)
    {
        string prompt = claimSelectionTemplate.prompt;

        return prompt.Replace("{evidenceName}", evidence.displayNameEn)
                     .Replace("{spatialContext}", evidence.spatialContext)
                     .Replace("{FeatureSet}", evidence.GetFeaturesAsStringForLLM())
                     .Replace("{ClaimSet}", ClaimManager.Instance.GetClaimsAsStringForLLM());
    }

    private List<ClaimSelectionResult> ParseLLMResponse(string response)
    {
        string cleanedJson = CleanJsonResponse(response);

        try
        {
            ClaimSelectionResponse result =
                JsonUtility.FromJson<ClaimSelectionResponse>(cleanedJson);

            if (result == null || result.selectedClaims == null)
            {
                Debug.LogWarning("[ClaimSelectionManager] No selected claims found.");
                return new List<ClaimSelectionResult>();
            }

            return result.selectedClaims;
        }
        catch (Exception e)
        {
            Debug.LogError("[ClaimSelectionManager] Failed to parse JSON response: " + e.Message);
            Debug.LogError("[ClaimSelectionManager] Raw response: " + response);
            Debug.LogError("[ClaimSelectionManager] Cleaned JSON: " + cleanedJson);
        }

        return new List<ClaimSelectionResult>();
    }

    private string CleanJsonResponse(string response)
    {
        string cleaned = response.Trim();

        if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned.Substring(7).Trim();
        }
        else if (cleaned.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned.Substring(3).Trim();
        }

        if (cleaned.EndsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned.Substring(0, cleaned.Length - 3).Trim();
        }

        return cleaned;
    }
}

[System.Serializable]
public class ClaimSelectionResponse
{
    public List<ClaimSelectionResult> selectedClaims;
}


/*
    Prompt 需要包含以下資訊：
    Feature set: 玩家選出的 Fact 對應到 Feature，單個 Evidence 的 Feature set 會被用來挑選出可能的 Claims set
    Claim set: 是由設計者提前設計好的 Claim 集合，讓 LLM 來挑選出最可能的 Claim set，並且給出每個 Claim 的 polarity 與 confidence

    Output 需要包含以下資訊：
    Claim ID: 對應到 Claim set 中的 Claim ID
    Confidence: 對應到 Claim set 中的 Claim confidence，0~1 的數值，表示 LLM 對於這個 Claim 的信心程度
    Reason: LLM 對於這個 Claim 的理由，為什麼會選擇這個 Claim，並且給出 polarity 與 confidence
*/

/*

You are a claim selection module for a narrative reasoning game.

Your task is to select plausible claims from the predefined Claim Set based only on the given Feature Set.

Important rules:
1. Do not invent new claims.
2. Only select claims whose claimID exists in the provided Claim Set.
3. Do not decide the final hypothesis.
4. Do not mention or infer the correct answer.
5. Treat all claims as possible interpretations, not confirmed facts.
6. Select only claims that are reasonably supported by the Feature Set.
7. If the Feature Set is insufficient, return an empty list.
8. Confidence must be between 0.0 and 1.0.
9. basedFeatureIDs must only contain featureIDs from the given Feature Set.
10. The reason should briefly explain why the selected features support this claim.

Input:
Evidence Name:
{evidenceName}

Feature Set:
{FeatureSet}

Claim Set:
{ClaimSet}

Output format:
Return only valid JSON. Do not include markdown, comments, or extra text.

{
  "selectedClaims": [
    {
      "claimID": "CLAIM_ID_FROM_CLAIM_SET",
      "confidence": 0.0,
      "basedFeatureIDs": [
        "FEATURE_ID_FROM_FEATURE_SET"
      ],
      "reason": "Brief explanation."
    }
  ]
}

*/

