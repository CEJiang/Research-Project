using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ReasoningValidityManager : Singleton<ReasoningValidityManager>
{
    public const string REASONING_VALIDITY_PATH = "Prompts/PlayerReasoning/ReasoningValidityTemplate";

    [Header("Prompt")]
    [SerializeField] private SimplePromptTemplate reasoningValidityTemplate;

    private void Start()
    {
        var reasoningValidityTemplateAsset =
            Instantiate(Resources.Load<SimplePromptTemplateAsset>(REASONING_VALIDITY_PATH));

        reasoningValidityTemplateAsset.ApplyTo(reasoningValidityTemplate);
    }

    public Task<ReasoningValidityResult> GenerateReasoningValidity()
    {
        Debug.Log("Generating Reasoning Validity...");
        return HandleReasoningValidityAsync();
    }

    private async Task<ReasoningValidityResult> HandleReasoningValidityAsync()
    {
        string prompt = BuildReasoningValidityPrompt();
        Debug.Log("[ReasoningValidityManager] Prompt: " + prompt);

        string response = await TaskLLMManager.Instance.RunTask(
            prompt
        );

        Debug.Log("[ReasoningValidityManager] LLM Response: " + response);

        return ParseLLMResponse(response);
    }

    private string BuildReasoningValidityPrompt()
    {
        string reasoningGraphData = ReasoningGraphManager.Instance.GetReasoningGraphDataForLLM();
        return reasoningValidityTemplate.prompt.Replace("{REASONING_GRAPH_DATA}", reasoningGraphData);
    }

    private ReasoningValidityResult ParseLLMResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return CreateFallbackResult(
                issueType: "parse_failed",
                description: "LLM response is empty."
            );
        }

        string cleanedJson = CleanJsonResponse(response);

        try
        {
            ReasoningValidityResult result = JsonUtility.FromJson<ReasoningValidityResult>(cleanedJson);

            if (result == null)
            {
                return CreateFallbackResult(
                    issueType: "parse_failed",
                    description: "Parsed result is null."
                );
            }

            NormalizeResult(result);
            return result;
        }
        catch (Exception e)
        {
            Debug.LogError("[ReasoningValidityManager] Failed to parse JSON response: " + e.Message);
            Debug.LogError("[ReasoningValidityManager] Raw response: " + response);

            return CreateFallbackResult(
                issueType: "parse_failed",
                description: "Failed to parse LLM JSON response."
            );
        }
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

    private void NormalizeResult(ReasoningValidityResult result)
    {
        // validity
        if (string.IsNullOrWhiteSpace(result.validity))
        {
            result.validity = "invalid";
        }
        else
        {
            result.validity = result.validity.Trim().ToLowerInvariant();
        }

        // primaryIssue
        if (string.IsNullOrWhiteSpace(result.primaryIssue))
        {
            result.primaryIssue = "none";
        }
        else
        {
            result.primaryIssue = result.primaryIssue.Trim().ToLowerInvariant();
        }

        // issues
        if (result.issues == null)
        {
            result.issues = new List<ReasoningIssue>();
        }

        foreach (var issue in result.issues)
        {
            if (issue == null) continue;

            issue.type = string.IsNullOrWhiteSpace(issue.type)
                ? "unknown"
                : issue.type.Trim().ToLowerInvariant();

            issue.fromNode ??= "";
            issue.toNode ??= "";

            issue.reasoning = string.IsNullOrWhiteSpace(issue.reasoning)
                ? ""
                : issue.reasoning.Trim().ToLowerInvariant();

            issue.description ??= "";
            issue.severity = Mathf.Clamp01(issue.severity);
        }

        // validity 值不合法時，直接 fallback 為 invalid
        if (result.validity != "valid" &&
            result.validity != "partially_valid" &&
            result.validity != "invalid")
        {
            result.validity = "invalid";
            result.scoringAllowed = false;
            result.primaryIssue = "parse_failed";

            result.issues = new List<ReasoningIssue>
            {
                new ReasoningIssue
                {
                    type = "parse_failed",
                    fromNode = "",
                    toNode = "",
                    reasoning = "",
                    description = "Unexpected validity value returned by LLM.",
                    severity = 1f
                }
            };

            return;
        }

        // primaryIssue 不合法時，盡量修正成 none
        if (result.primaryIssue != "none" &&
            result.primaryIssue != "contradiction" &&
            result.primaryIssue != "missing_link" &&
            result.primaryIssue != "unsupported_jump" &&
            result.primaryIssue != "weak_reasoning" &&
            result.primaryIssue != "parse_failed")
        {
            result.primaryIssue = "none";
        }

        // invalid 一律不允許 scoring
        if (result.validity == "invalid")
        {
            result.scoringAllowed = false;
        }

        // valid 但沒 issue 時，primaryIssue 應該是 none
        if (result.validity == "valid" && result.issues.Count == 0)
        {
            result.primaryIssue = "none";
        }

        // 如果沒有 issue 但卻標了不是 none，修正掉
        if (result.issues.Count == 0 && result.primaryIssue != "parse_failed")
        {
            result.primaryIssue = "none";
        }
    }

    private ReasoningValidityResult CreateFallbackResult(string issueType, string description)
    {
        return new ReasoningValidityResult
        {
            validity = "invalid",
            scoringAllowed = false,
            primaryIssue = issueType,
            issues = new List<ReasoningIssue>
            {
                new ReasoningIssue
                {
                    type = issueType,
                    fromNode = "",
                    toNode = "",
                    reasoning = "",
                    description = description,
                    severity = 1f
                }
            }
        };
    }
}

[Serializable]
public class ReasoningIssue
{
    public string type;        // contradiction / missing_link / unsupported_jump / weak_reasoning
    public string fromNode;    // 問題邊的起點
    public string toNode;      // 問題邊的終點
    public string reasoning;    // leads to / conflicts with / is consistent with
    public string description; // 為什麼這條 edge 有問題
    public float severity;     // 0~1
}

[Serializable]
public class ReasoningValidityResult
{
    public string validity;          // valid / partially_valid / invalid
    public bool scoringAllowed;
    public string primaryIssue;      // none / contradiction / missing_link / unsupported_jump / weak_reasoning
    public List<ReasoningIssue> issues;
}