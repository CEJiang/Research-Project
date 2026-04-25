using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.Linq;

public class FeatureSelectionManager : Singleton<FeatureSelectionManager>
{
    private const string FEATURE_TEMPLATE_PATH = "Prompts/FeatureSelectionTemplate";

    [Header("Prompt Template")]
    [SerializeField] private SimplePromptTemplate featureSelectionTemplate;

    private string featureDictionaryText;

    void Start()
    {
        // Load template asset into featureSelectionTemplate
        var asset = Resources.Load<SimplePromptTemplateAsset>(FEATURE_TEMPLATE_PATH);
        if (asset != null)
        {
            asset.ApplyTo(featureSelectionTemplate);
            Debug.Log("[FeatureSelectionManager] Template loaded.");
        }

        // Build feature dictionary ONCE (string) from FeatureManager
        featureDictionaryText = FeatureManager.Instance.GetFeature();
        Debug.Log("[FeatureSelectionManager] Feature dictionary prepared:\n" + featureDictionaryText);
    }

    public async Task<List<FeatureSelectionResult>> GenerateFeatureSelection(EvidenceModel evidence)
    {
        Debug.Log("[FeatureSelectionManager] Generating Feature Selection...");

        // Build the prompt for this specific evidence
        string prompt = BuildFeaturePrompt(evidence);

        Debug.Log("[FeatureSelectionManager] Prompt: " + prompt);

        // Send to LLM
        string response = await TaskLLMManager.Instance.RunTask(
            featureSelectionTemplate.prompt,
            prompt
        );

        Debug.Log("[FeatureSelectionManager] LLM Response: " + response);

        // Parse JSON into results
        return ParseFeatureSelectionResponse(response);
    }

    private string BuildFeaturePrompt(EvidenceModel evidence)
    {
        string prompt = featureSelectionTemplate.prompt;

        prompt = prompt.Replace("{{displayName}}", evidence.displayName)
                       .Replace("{{zoneAt}}", evidence.zoneAt)
                       .Replace("{{facts}}", evidence.facts)
                       .Replace("{{featureDictionary}}", featureDictionaryText);

        return prompt;
    }

    private List<FeatureSelectionResult> ParseFeatureSelectionResponse(string response)
    {
        try
        {
            response = response.Replace("```json", "")
                            .Replace("```", "")
                            .Trim();

            if (!response.StartsWith("{"))
            {
                Debug.LogWarning("[FeatureSelection] Response is not a JSON object. Raw: " + response);
                return new List<FeatureSelectionResult>();
            }

            Debug.Log("[FeatureSelectionManager] Parsing LLM response into FeatureSelectionResults...");
            var wrapper = JsonUtility.FromJson<FeatureSelectionResponseWrapper>(response);

            if (wrapper == null || wrapper.featureResults == null)
            {
                Debug.LogWarning("[FeatureSelection] Parsed wrapper or featureResults was null.");
                return new List<FeatureSelectionResult>();
            }
            Debug.Log("[FeatureSelectionManager] Successfully parsed " + wrapper.featureResults.Count + " feature results from LLM response.");

            return wrapper.featureResults;
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[FeatureSelection] JSON Parse Error: " + ex.Message);
            Debug.LogError("Response was: " + response);
            return new List<FeatureSelectionResult>();
        }
    }
}

[System.Serializable]
public class FeatureSelectionResponseWrapper
{
    public List<FeatureSelectionResult> featureResults;
}

[System.Serializable]
public class FeatureSelectionResult
{
    public string featureId;
    public string polarity;
    public float strength;
    public string reason;
}

/*

Your job is to evaluate the evidence facts and determine which FEATURES from the fixed Feature Dictionary are present.

============================================================
GLOBAL RULES (MUST FOLLOW)
============================================================
1. You may ONLY choose features from the Feature Dictionary below.
2. You MUST evaluate EVERY feature independently in a strict boolean manner:
   MATCH = A fact describes the core observable phenomenon required by the feature.
   NO MATCH = Facts do not provide enough specific detail to satisfy the feature.
3. You MUST NOT guess or assume missing details.
   However, you should recognize synonyms and equivalent physical descriptions.
4. The NAME or ZONE provides context but CANNOT activate a feature by itself.
5. If facts do not satisfy the core requirement of a feature, that feature MUST NOT be selected.
6. You MUST be deterministic. With the same input, you MUST produce the same output.

============================================================
STRICT MATCHING PROTOCOL (DETERMINISTIC)
============================================================
A) Feature-based Matching:
- A feature matches if a fact describes the core observable phenomenon required by the feature.
- Do NOT use "interpretation words" to bridge logic gaps.

B) One-fact-only rule:
- Each matched feature MUST be justified by exactly ONE fact sentence.
- Do NOT combine multiple facts to justify a single feature.

C) Evidence Integrity:
- If a fact explicitly negates a condition (e.g., "no tool marks"), do not match any features requiring that condition.

============================================================
BANNED REASONING WORDS (HARD RULE)
============================================================
The "reason" MUST NOT contain any of these words:
"suggest", "suggests", "imply", "implies", "indicate", "indicates",
"likely", "probably", "possible", "maybe", "could", "might"

============================================================
STRENGTH RULES (STRICT)
============================================================
strength = 1.0 → Strong Explicit Match:
- The fact uses the exact terminology found in the feature definition OR is a 100% direct match.

strength = 0.6 → Direct Physical Equivalence:
- The fact describes the same physical evidence using descriptive observational language.

strength = 0.3 → Weak/Incomplete Match:
- The fact mentions a component but lacks enough detail to be certain.

============================================================
OUTPUT RULES (STRICT)
============================================================
- Output ONLY valid JSON.
- Output ONLY matched features.
- reason MUST be a short factual sentence that directly restates ONE triggering fact.

============================================================
EVIDENCE INPUT
============================================================
Name: {{displayName}}
Zone: {{zoneAt}}
Facts: {{facts}}

============================================================
FEATURE DICTIONARY (USE ONLY THESE FEATURES)
============================================================
{{featureDictionary}}

Each entry is:
- featureId: string
- definition: string

============================================================
OUTPUT FORMAT (STRICT)
============================================================
{
  "featureResults": [
    {
      "featureId": "...",
      "polarity": "positive" or "negative",
      "strength": 0.0,
      "reason": "..."
    }
  ]
}

*/