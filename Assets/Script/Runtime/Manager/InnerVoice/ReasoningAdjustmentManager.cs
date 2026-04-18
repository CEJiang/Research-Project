using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class ReasoningAdjustmentManager : Singleton<ReasoningAdjustmentManager>
{
    public const string REASONING_ADJUSTMENT_PATH = "Prompts/PlayerReasoning/ReasoningAdjustmentTemplate";

    [Header("Prompt")]
    [SerializeField] private SimplePromptTemplate reasoningAdjustmentTemplate;

    [Header("Final Score Weights")]
    [SerializeField] private float relationWeight = 0.4f;
    [SerializeField] private float oppositionWeight = 0.3f;
    [SerializeField] private float sequenceWeight = 0.3f;

    private void Start()
    {
        var reasoningTemplateAsset =
            Instantiate(Resources.Load<SimplePromptTemplateAsset>(REASONING_ADJUSTMENT_PATH));

        reasoningTemplateAsset.ApplyTo(reasoningAdjustmentTemplate);
    }

    public Task<ReasoningAdjustmentResponse> GenerateReasoningAdjustment()
    {
        Debug.Log("Generating Reasoning Adjustment...");
        return HandleReasoningAdjustmentAsync();
    }

    private async Task<ReasoningAdjustmentResponse> HandleReasoningAdjustmentAsync()
    {
        string prompt = BuildReasoningAdjustmentPrompt();
        Debug.Log("[ReasoningAdjustmentManager] Prompt: " + prompt);

        string response = await TaskLLMManager.Instance.RunTask(
            reasoningAdjustmentTemplate.prompt,
            prompt
        );

        Debug.Log("[ReasoningAdjustmentManager] LLM Response: " + response);

        ReasoningAdjustmentResponse adjustmentResponse = ParseLLMResponse(response);
        return adjustmentResponse;
    }

    private string BuildReasoningAdjustmentPrompt()
    {
        string prompt = reasoningAdjustmentTemplate.prompt;
        string relationGraphData = RelationGraphManager.Instance.GetRelationGraphDataForLLM();

        string hypothesisList = string.Join("\n",
            HypothesisStateManager.Instance.hypotheses.Select(h =>
                $"- {h.id}: {h.description}"
            )
        );

        return prompt
            .Replace("{RELATION_GRAPH_DATA}", relationGraphData)
            .Replace("{HYPOTHESIS_LIST}", hypothesisList);
    }

    private ReasoningAdjustmentResponse ParseLLMResponse(string response)
    {
        Dictionary<string, ReasoningAdjustmentItem> resultMap = CreateDefaultAdjustmentMap();

        if (string.IsNullOrWhiteSpace(response))
        {
            Debug.LogWarning("[ReasoningAdjustmentManager] Empty LLM response.");
            return BuildResponseFromMap(resultMap);
        }

        try
        {
            string cleaned = ExtractJson(response);

            ReasoningAdjustmentResponse parsed =
                JsonUtility.FromJson<ReasoningAdjustmentResponse>(cleaned);

            if (parsed == null || parsed.adjustments == null)
            {
                Debug.LogWarning("[ReasoningAdjustmentManager] Parsed response is null or missing adjustments.");
                return BuildResponseFromMap(resultMap);
            }

            foreach (var item in parsed.adjustments)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.hypothesisId))
                    continue;

                if (!resultMap.ContainsKey(item.hypothesisId))
                {
                    Debug.LogWarning($"[ReasoningAdjustmentManager] Unknown hypothesisId: {item.hypothesisId}");
                    continue;
                }

                var normalizedItem = new ReasoningAdjustmentItem
                {
                    hypothesisId = item.hypothesisId,

                    // 純支持強度：0 ~ 1
                    relationSupport = Mathf.Clamp01(item.relationSupport),

                    // 純反對強度：0 ~ 1
                    relationOpposition = Mathf.Clamp01(item.relationOpposition),

                    // sequence 強度：0 ~ 1
                    sequenceSupport = Mathf.Clamp01(item.sequenceSupport),

                    reason = string.IsNullOrWhiteSpace(item.reason) ? "" : item.reason.Trim()
                };

                // 最終 score 由程式計算
                normalizedItem.score = ComputeFinalScore(normalizedItem);

                resultMap[item.hypothesisId] = normalizedItem;
            }

            return BuildResponseFromMap(resultMap);
        }
        catch (Exception ex)
        {
            Debug.LogError("[ReasoningAdjustmentManager] Failed to parse LLM response: " + ex.Message);
            Debug.LogError("[ReasoningAdjustmentManager] Raw response: " + response);
            return BuildResponseFromMap(resultMap);
        }
    }

    private float ComputeFinalScore(ReasoningAdjustmentItem item)
    {
        float finalScore =
            item.relationSupport * relationWeight
            - item.relationOpposition * oppositionWeight
            + item.sequenceSupport * sequenceWeight;

        return Mathf.Clamp(finalScore, -1f, 1f);
    }

    private Dictionary<string, ReasoningAdjustmentItem> CreateDefaultAdjustmentMap()
    {
        Dictionary<string, ReasoningAdjustmentItem> map = new();

        foreach (var hypothesis in HypothesisStateManager.Instance.hypotheses)
        {
            map[hypothesis.id] = new ReasoningAdjustmentItem
            {
                hypothesisId = hypothesis.id,
                relationSupport = 0f,
                relationOpposition = 0f,
                sequenceSupport = 0f,
                score = 0f,
                reason = ""
            };
        }

        return map;
    }

    private ReasoningAdjustmentResponse BuildResponseFromMap(
        Dictionary<string, ReasoningAdjustmentItem> map)
    {
        var response = new ReasoningAdjustmentResponse
        {
            adjustments = new List<ReasoningAdjustmentItem>()
        };

        foreach (var hypothesis in HypothesisStateManager.Instance.hypotheses)
        {
            if (map.TryGetValue(hypothesis.id, out var item))
            {
                response.adjustments.Add(item);
            }
            else
            {
                response.adjustments.Add(new ReasoningAdjustmentItem
                {
                    hypothesisId = hypothesis.id,
                    relationSupport = 0f,
                    relationOpposition = 0f,
                    sequenceSupport = 0f,
                    score = 0f,
                    reason = ""
                });
            }
        }

        return response;
    }

    public Dictionary<string, float> ConvertResponseToDictionary(ReasoningAdjustmentResponse response)
    {
        Dictionary<string, float> map = new();

        foreach (var hypothesis in HypothesisStateManager.Instance.hypotheses)
        {
            map[hypothesis.id] = 0f;
        }

        if (response == null || response.adjustments == null)
            return map;

        foreach (var item in response.adjustments)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.hypothesisId))
                continue;

            if (!map.ContainsKey(item.hypothesisId))
                continue;

            map[item.hypothesisId] = item.score;
        }

        return map;
    }

    private string ExtractJson(string raw)
    {
        int start = raw.IndexOf('{');
        int end = raw.LastIndexOf('}');

        if (start < 0 || end < start)
            throw new Exception("No valid JSON object found in response.");

        return raw.Substring(start, end - start + 1);
    }
}

[Serializable]
public class ReasoningAdjustmentResponse
{
    public List<ReasoningAdjustmentItem> adjustments;
}

[Serializable]
public class ReasoningAdjustmentItem
{
    public string hypothesisId;

    // connected relations 中對該 hypothesis 的支持強度（0 ~ 1）
    public float relationSupport;

    // connected relations 中對該 hypothesis 的反對強度（0 ~ 1）
    public float relationOpposition;

    // connected nodes 是否形成多步驟 sequence（0 ~ 1）
    public float sequenceSupport;

    // 程式端計算出的最終分數
    public float score;

    public string reason;
}

/*
You are the Reasoning Adjustment LLM.

Your task is to evaluate the player's current relation graph and produce reasoning-structure adjustments for each hypothesis.

--------------------------------------------------
CORE PRINCIPLE
--------------------------------------------------
You must evaluate ONLY how the player structures reasoning through connections.

You are NOT evaluating raw evidence.
You are NOT evaluating which hypothesis is true.

You are evaluating:
→ how well the player's connected reasoning structure supports or opposes each hypothesis.

--------------------------------------------------
CRITICAL STRUCTURAL RESTRICTION
--------------------------------------------------
You must judge ONLY from connected graph structure.

- A single evidence node alone is NOT sufficient.
- Isolated nodes must NOT contribute meaningful support or opposition.
- Only connected nodes with meaningful relations count.

If evidence is not meaningfully connected:
→ treat it as weak or no structural contribution.

--------------------------------------------------
IMPORTANT
--------------------------------------------------
- Do NOT evaluate raw evidence support.
- The evidence-driven layer already handles evidence propensity.
- Do NOT reward hypotheses based on standalone nodes.
- Prefer connected reasoning over isolated clues.

--------------------------------------------------
FOR EACH HYPOTHESIS, OUTPUT:

1. relationSupport (0.0 to 1.0)

Definition:
- Measures how strongly connected relations support the hypothesis.

Rules:
- Evaluate ONLY connected edges.
- Consider:
  - "leads to" (causal / inferential support)
  - "is consistent with" (mutual reinforcement)
- Do NOT infer support from node meaning alone.
- Do NOT include opposition here.

Scoring:
- 0.0 = no meaningful support from connected structure
- 1.0 = strong and consistent structural support

--------------------------------------------------

2. relationOpposition (0.0 to 1.0)

Definition:
- Measures how strongly connected relations contradict or weaken the hypothesis.

Rules:
- Evaluate ONLY connected edges.
- Consider:
  - "conflicts with" relations
  - relations that contradict what the hypothesis would expect
- Do NOT infer opposition from node meaning alone.
- Do NOT include support here.

Scoring:
- 0.0 = no meaningful opposition
- 1.0 = strong structural contradiction against the hypothesis

--------------------------------------------------

3. sequenceSupport (0.0 to 1.0)

Definition:
- Measures whether connected nodes form a plausible multi-step event sequence under the hypothesis.

Rules:
- Must involve connected structure (not isolated nodes)
- Look for progression:
  cause → process → outcome
- A single node or disconnected nodes → near 0
- Strong multi-step chain → higher score

--------------------------------------------------
REASONING PRINCIPLES
--------------------------------------------------
- relationSupport = local structural support
- relationOpposition = local structural contradiction
- sequenceSupport = global chain coherence

--------------------------------------------------
STRICT EVALUATION RULES
--------------------------------------------------
1. Judge structure, not isolated evidence.
2. Connected nodes matter more than standalone nodes.
3. If a hypothesis is only suggested by unconnected nodes → keep all scores low.
4. Do NOT reward node presence.
5. Support and opposition must be evaluated separately.
6. Do NOT merge support and opposition into one value.
7. If no meaningful connected structure exists → all scores should remain low.

--------------------------------------------------
REASON FORMAT RULE
--------------------------------------------------
Explain using structure ONLY.

GOOD:
"A leads to B and B leads to C, forming a partial sequence for H2, while D conflicts with A, creating opposition."

BAD:
"Blood supports H2."

--------------------------------------------------
OUTPUT RULES
--------------------------------------------------
1. Output ONLY valid JSON.
2. Every hypothesis must appear exactly once.
3. No markdown.
4. No extra commentary.
5. Reasons must be concise and structural.
6. Base all judgments on connected graph structure.

--------------------------------------------------
Hypotheses:
{HYPOTHESIS_LIST}

--------------------------------------------------
Relation Graph:
{RELATION_GRAPH_DATA}

--------------------------------------------------
OUTPUT FORMAT:
{
  "adjustments": [
    {
      "hypothesisId": "H0",
      "relationSupport": 0.0,
      "relationOpposition": 0.0,
      "sequenceSupport": 0.0,
      "reason": "..."
    },
    {
      "hypothesisId": "H1",
      "relationSupport": 0.0,
      "relationOpposition": 0.0,
      "sequenceSupport": 0.0,
      "reason": "..."
    },
    {
      "hypothesisId": "H2",
      "relationSupport": 0.0,
      "relationOpposition": 0.0,
      "sequenceSupport": 0.0,
      "reason": "..."
    },
    {
      "hypothesisId": "H3",
      "relationSupport": 0.0,
      "relationOpposition": 0.0,
      "sequenceSupport": 0.0,
      "reason": "..."
    },
    {
      "hypothesisId": "H4",
      "relationSupport": 0.0,
      "relationOpposition": 0.0,
      "sequenceSupport": 0.0,
      "reason": "..."
    }
  ]
}
*/