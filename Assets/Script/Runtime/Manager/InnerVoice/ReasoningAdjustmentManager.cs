using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class ReasoningAdjustmentManager : Singleton<ReasoningAdjustmentManager>
{
    public const string REASONING_ADJUSTMENT_PATH =
        "Prompts/PlayerReasoning/ReasoningAdjustmentTemplate";

    [Header("Prompt")]
    [SerializeField]
    private SimplePromptTemplate reasoningAdjustmentTemplate;

    [Header("Final Adjustment Weights")]
    [SerializeField, Range(0f, 1f)]
    private float supportWeight = 0.45f;

    [SerializeField, Range(0f, 1f)]
    private float oppositionWeight = 0.35f;

    [SerializeField, Range(0f, 1f)]
    private float sequenceWeight = 0.20f;

    [Header("Edge Semantic Support Weights")]
    [SerializeField, Range(0f, 1f)]
    private float validityWeight = 0.60f;

    [SerializeField, Range(0f, 1f)]
    private float strengthWeight = 0.40f;

    [Header("Graph Reliability Weights")]
    [SerializeField, Range(0f, 1f)]
    private float edgeQualityWeight = 0.60f;

    [SerializeField, Range(0f, 1f)]
    private float validEdgeRatioWeight = 0.25f;

    [SerializeField, Range(0f, 1f)]
    private float patternDiversityWeight = 0.15f;

    [Header("Scoring")]
    [Tooltip("Maximum reasoning contribution from all outgoing edges of one source Evidence.")]
    [SerializeField]
    private float sourceEvidenceBudget = 1f;

    [Tooltip("Controls the saturation rate of final reasoning adjustments.")]
    [SerializeField]
    private float adjustmentTemperature = 1f;


    public ReasoningGraphEvaluationResponse LatestGraphEvaluation
    {
        get;
        private set;
    } = new ReasoningGraphEvaluationResponse();

    public ReasoningAdjustmentResponse LatestAdjustmentResponse
    {
        get;
        private set;
    } = new ReasoningAdjustmentResponse();

    private void Start()
    {
        InitializePromptTemplate();
    }

    private void InitializePromptTemplate()
    {
        SimplePromptTemplateAsset templateAsset =
            Resources.Load<SimplePromptTemplateAsset>(
                REASONING_ADJUSTMENT_PATH
            );

        if (templateAsset == null)
        {
            Debug.LogError(
                $"[ReasoningAdjustmentManager] Prompt template not found: " +
                $"{REASONING_ADJUSTMENT_PATH}"
            );
            return;
        }

        if (reasoningAdjustmentTemplate == null)
        {
            Debug.LogError(
                "[ReasoningAdjustmentManager] reasoningAdjustmentTemplate is not assigned."
            );
            return;
        }

        SimplePromptTemplateAsset templateInstance =
            Instantiate(templateAsset);

        templateInstance.ApplyTo(reasoningAdjustmentTemplate);
    }

    /// <summary>
    /// Evaluates the entire current reasoning graph with one LLM request.
    /// Evaluates the current reasoning graph with one LLM request.
    /// The LLM returns overall Edge interpretation and H0-H4 effects.
    /// Numerical adjustment aggregation is handled locally.
    /// </summary>
    public Task<ReasoningAdjustmentResponse> GenerateReasoningAdjustment()
    {
        Debug.Log(
            "[ReasoningAdjustmentManager] Generating reasoning adjustment..."
        );

        return HandleReasoningAdjustmentAsync();
    }

    private async Task<ReasoningAdjustmentResponse>
        HandleReasoningAdjustmentAsync()
    {
        string prompt = BuildReasoningAdjustmentPrompt();

        Debug.Log(
            "[ReasoningAdjustmentManager] Prompt:\n" + prompt
        );

        string rawResponse =
            await TaskLLMManager.Instance.RunTask(prompt);

        Debug.Log(
            "[ReasoningAdjustmentManager] LLM Response:\n" +
            rawResponse
        );

        ReasoningGraphEvaluationResponse graphEvaluation =
            ParseGraphEvaluationResponse(rawResponse);

        LatestGraphEvaluation =
            graphEvaluation;

        ReasoningAdjustmentResponse result =
            AggregateGraphEvaluation(graphEvaluation);

        LatestAdjustmentResponse =
            result;

        DebugAggregatedResult(result);

        return result;
    }

    private string BuildReasoningAdjustmentPrompt()
    {
        if (reasoningAdjustmentTemplate == null ||
            string.IsNullOrWhiteSpace(
                reasoningAdjustmentTemplate.prompt
            ))
        {
            throw new InvalidOperationException(
                "[ReasoningAdjustmentManager] Reasoning adjustment prompt is empty."
            );
        }

        string reasoningGraphData =
            ReasoningGraphManager.Instance
                .GetReasoningGraphDataForLLM();

        string hypothesisList = string.Join(
            "\n",
            HypothesisStateManager.Instance.hypotheses
                .Where(hypothesis =>
                    hypothesis != null &&
                    !string.IsNullOrWhiteSpace(hypothesis.id)
                )
                .Select(hypothesis =>
                    $"- {hypothesis.id}: {hypothesis.description}"
                )
        );

        return reasoningAdjustmentTemplate.prompt
            .Replace(
                "{REASONING_GRAPH_DATA}",
                reasoningGraphData ?? string.Empty
            )
            .Replace(
                "{HYPOTHESIS_LIST}",
                hypothesisList
            );
    }

    private ReasoningGraphEvaluationResponse
        ParseGraphEvaluationResponse(string rawResponse)
    {
        ReasoningGraphEvaluationResponse emptyResponse =
            new ReasoningGraphEvaluationResponse();

        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            Debug.LogWarning(
                "[ReasoningAdjustmentManager] Empty LLM response."
            );

            return emptyResponse;
        }

        try
        {
            string json = ExtractJson(rawResponse);

            ReasoningGraphEvaluationResponse parsed =
                JsonUtility.FromJson<
                    ReasoningGraphEvaluationResponse
                >(json);

            if (parsed == null)
            {
                Debug.LogWarning(
                    "[ReasoningAdjustmentManager] Parsed graph evaluation is null."
                );

                return emptyResponse;
            }

            parsed.edgeEvaluations ??=
                new List<ReasoningEdgeEvaluation>();

            parsed.edgeEvaluations =
                parsed.edgeEvaluations
                    .Where(IsValidEdgeEvaluation)
                    .Select(NormalizeEdgeEvaluation)
                    .ToList();

            return parsed;
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[ReasoningAdjustmentManager] " +
                $"Failed to parse LLM response: {ex.Message}"
            );

            Debug.LogError(
                "[ReasoningAdjustmentManager] Raw response:\n" +
                rawResponse
            );

            return emptyResponse;
        }
    }

    private bool IsValidEdgeEvaluation(
        ReasoningEdgeEvaluation edge
    )
    {
        if (edge == null)
            return false;

        if (string.IsNullOrWhiteSpace(edge.edgeId))
            return false;

        if (string.IsNullOrWhiteSpace(edge.sourceEvidenceId))
            return false;

        if (string.IsNullOrWhiteSpace(edge.targetEvidenceId))
            return false;

        return true;
    }

    private ReasoningEdgeEvaluation NormalizeEdgeEvaluation(
        ReasoningEdgeEvaluation edge
    )
    {
        edge.edgeId =
            edge.edgeId?.Trim() ?? string.Empty;

        edge.sourceEvidenceId =
            edge.sourceEvidenceId?.Trim() ?? string.Empty;

        edge.targetEvidenceId =
            edge.targetEvidenceId?.Trim() ?? string.Empty;


        edge.playerReasoningType =
            NormalizePlayerReasoningType(
                edge.playerReasoningType
            );

        edge.validity =
            NormalizeValidity(edge.validity);

        edge.strength =
            NormalizeStrength(edge.strength);

        edge.primaryIssue =
            NormalizePrimaryIssue(
                edge.primaryIssue
            );

        edge.reason =
            edge.reason?.Trim() ?? string.Empty;

        edge.hypothesisEvaluations ??=
            new List<EdgeHypothesisEvaluation>();

        HashSet<string> validHypothesisIds =
            HypothesisStateManager.Instance.hypotheses
                .Where(hypothesis =>
                    hypothesis != null &&
                    !string.IsNullOrWhiteSpace(hypothesis.id)
                )
                .Select(hypothesis => hypothesis.id)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase
                );

        edge.hypothesisEvaluations =
            edge.hypothesisEvaluations
                .Where(evaluation =>
                    evaluation != null &&
                    !string.IsNullOrWhiteSpace(
                        evaluation.hypothesisId
                    ) &&
                    validHypothesisIds.Contains(
                        evaluation.hypothesisId.Trim()
                    )
                )
                .Select(evaluation =>
                {
                    evaluation.hypothesisId =
                        evaluation.hypothesisId.Trim();

                    evaluation.effect =
                        NormalizeHypothesisEffect(
                            evaluation.effect
                        );

                    evaluation.semanticFit =
                        NormalizeStrength(
                            evaluation.semanticFit
                        );

                    evaluation.reason =
                        evaluation.reason?.Trim()
                        ?? string.Empty;

                    return evaluation;
                })
                .GroupBy(
                    evaluation => evaluation.hypothesisId,
                    StringComparer.OrdinalIgnoreCase
                )
                .Select(group => group.First())
                .ToList();

        EnsureAllHypothesesExist(edge);

        return edge;
    }

    private void EnsureAllHypothesesExist(
        ReasoningEdgeEvaluation edge
    )
    {
        if (edge.hypothesisEvaluations == null)
        {
            edge.hypothesisEvaluations =
                new List<EdgeHypothesisEvaluation>();
        }

        foreach (Hypothesis hypothesis in
                 HypothesisStateManager.Instance.hypotheses)
        {
            if (hypothesis == null ||
                string.IsNullOrWhiteSpace(hypothesis.id))
            {
                continue;
            }

            bool alreadyExists =
                edge.hypothesisEvaluations.Any(
                    evaluation =>
                        string.Equals(
                            evaluation.hypothesisId,
                            hypothesis.id,
                            StringComparison.OrdinalIgnoreCase
                        )
                );

            if (alreadyExists)
            {
                continue;
            }

            edge.hypothesisEvaluations.Add(
                new EdgeHypothesisEvaluation
                {
                    hypothesisId = hypothesis.id,
                    effect = HypothesisEffectTypes.Neutral,
                    semanticFit = ReasoningStrength.Unsupported,
                    reason = string.Empty
                }
            );
        }

        edge.hypothesisEvaluations =
            edge.hypothesisEvaluations
                .OrderBy(evaluation =>
                    evaluation.hypothesisId
                )
                .ToList();
    }

    private ReasoningAdjustmentResponse AggregateGraphEvaluation(
        ReasoningGraphEvaluationResponse graphEvaluation
    )
    {
        Dictionary<string, HypothesisReasoningAccumulator>
            accumulators =
                CreateHypothesisAccumulators();

        List<ReasoningEdgeEvaluation> edges =
            graphEvaluation?.edgeEvaluations ??
            new List<ReasoningEdgeEvaluation>();

        if (edges.Count == 0)
        {
            return BuildEmptyAdjustmentResponse();
        }

        Dictionary<string, float> edgeSemanticSupport =
            new Dictionary<string, float>(
                StringComparer.OrdinalIgnoreCase
            );

        foreach (ReasoningEdgeEvaluation edge in edges)
        {
            float semanticSupport =
                ComputeSemanticSupport(edge);

            edge.semanticSupport =
                semanticSupport;

            edgeSemanticSupport[edge.edgeId] =
                semanticSupport;
        }

        /*
         * Each source Evidence has a fixed outgoing reasoning budget.
         * This prevents unlimited score gain from drawing many edges.
         */
        Dictionary<string, float> sourceQualityTotals =
            edges
                .GroupBy(
                    edge => edge.sourceEvidenceId,
                    StringComparer.OrdinalIgnoreCase
                )
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(edge =>
                        edgeSemanticSupport.TryGetValue(
                            edge.edgeId,
                            out float semanticSupportValue
                        )
                            ? semanticSupportValue
                            : 0f
                    ),
                    StringComparer.OrdinalIgnoreCase
                );

        /*
         * Repeated semantic profiles receive diminishing returns.
         * The key is based on player reasoning type and H0-H4 evaluation profile,
         * not on a forced Claim-to-Claim pair.
         */
        Dictionary<string, int> patternCounts =
            edges
                .GroupBy(CreatePatternKey)
                .ToDictionary(
                    group => group.Key,
                    group => group.Count()
                );

        foreach (ReasoningEdgeEvaluation edge in edges)
        {
            float semanticSupport =
                edgeSemanticSupport.TryGetValue(
                    edge.edgeId,
                    out float semanticSupportValue
                )
                    ? semanticSupportValue
                    : 0f;

            float sourceTotal =
                sourceQualityTotals.TryGetValue(
                    edge.sourceEvidenceId,
                    out float total
                )
                    ? total
                    : 0f;

            float sourceBudgetScale =
                CalculateSourceBudgetScale(
                    semanticSupport,
                    sourceTotal
                );

            string patternKey =
                CreatePatternKey(edge);

            int repeatedCount =
                patternCounts.TryGetValue(
                    patternKey,
                    out int count
                )
                    ? Mathf.Max(1, count)
                    : 1;

            float noveltyFactor =
                1f / Mathf.Sqrt(repeatedCount);

            float effectiveEdgeWeight =
                sourceBudgetScale *
                noveltyFactor;

            edge.effectiveAdjustmentWeight =
                Mathf.Clamp01(effectiveEdgeWeight);

            ApplyEdgeEvaluations(
                edge,
                effectiveEdgeWeight,
                accumulators
            );
        }

        List<ReasoningAdjustmentItem> adjustments =
            new List<ReasoningAdjustmentItem>();

        foreach (Hypothesis hypothesis in
                 HypothesisStateManager.Instance.hypotheses)
        {
            if (hypothesis == null ||
                string.IsNullOrWhiteSpace(hypothesis.id))
            {
                continue;
            }

            HypothesisReasoningAccumulator accumulator =
                accumulators[hypothesis.id];

            float reasoningSupportRaw =
                accumulator.supportRaw;

            float reasoningOppositionRaw =
                accumulator.oppositionRaw;

            float sequenceSupportRaw =
                accumulator.sequenceRaw;

            float reasoningSupport =
                SaturatePositive(
                    reasoningSupportRaw
                );

            float reasoningOpposition =
                SaturatePositive(
                    reasoningOppositionRaw
                );

            float sequenceSupport =
                SaturatePositive(
                    sequenceSupportRaw
                );

            float rawScore =
                ComputeRawScore(
                    reasoningSupportRaw,
                    reasoningOppositionRaw,
                    sequenceSupportRaw
                );

            float score =
                ComputeFinalScore(
                    reasoningSupport,
                    reasoningOpposition,
                    sequenceSupport
                );

            ReasoningAdjustmentItem item =
                new ReasoningAdjustmentItem
                {
                    hypothesisId = hypothesis.id,

                    reasoningSupportRaw =
                        reasoningSupportRaw,

                    reasoningOppositionRaw =
                        reasoningOppositionRaw,

                    sequenceSupportRaw =
                        sequenceSupportRaw,

                    reasoningSupport =
                        reasoningSupport,

                    reasoningOpposition =
                        reasoningOpposition,

                    sequenceSupport =
                        sequenceSupport,

                    rawScore =
                        rawScore,

                    score =
                        score,

                    reason = BuildHypothesisReason(
                        hypothesis.id,
                        edges
                    )
                };

            adjustments.Add(item);
        }

        float graphReliability =
            ComputeGraphReliability(edges);

        return new ReasoningAdjustmentResponse
        {
            adjustments = adjustments,
            edgeEvaluations = edges,
            graphReliability = graphReliability,
            validEdgeRatio =
                ComputeValidEdgeRatio(edges),
            patternDiversity =
                ComputePatternDiversity(edges)
        };
    }

    private void ApplyEdgeEvaluations(
        ReasoningEdgeEvaluation edge,
        float effectiveEdgeWeight,
        Dictionary<string, HypothesisReasoningAccumulator>
            accumulators
    )
    {
        if (edge.hypothesisEvaluations == null)
            return;

        bool isLeadTo =
            string.Equals(
                edge.playerReasoningType,
                PlayerReasoningTypes.LeadsTo,
                StringComparison.OrdinalIgnoreCase
            );

        foreach (
            EdgeHypothesisEvaluation evaluation
            in edge.hypothesisEvaluations
        )
        {
            if (evaluation == null ||
                string.IsNullOrWhiteSpace(
                    evaluation.hypothesisId
                ))
            {
                continue;
            }

            if (!accumulators.TryGetValue(
                    evaluation.hypothesisId,
                    out HypothesisReasoningAccumulator accumulator
                ))
            {
                continue;
            }

            float semanticFit =
                GetStrengthWeight(
                    evaluation.semanticFit
                );

            float contribution =
                effectiveEdgeWeight *
                semanticFit;

            switch (evaluation.effect)
            {
                case HypothesisEffectTypes.Support:
                    accumulator.supportRaw +=
                        contribution;

                    if (isLeadTo)
                    {
                        accumulator.sequenceRaw +=
                            contribution;
                    }
                    break;

                case HypothesisEffectTypes.Oppose:
                    accumulator.oppositionRaw +=
                        contribution;
                    break;

                case HypothesisEffectTypes.Neutral:
                default:
                    break;
            }
        }
    }

    private float ComputeSemanticSupport(
        ReasoningEdgeEvaluation edge
    )
    {
        if (edge == null)
            return 0f;

        float validityScore =
            GetInterpretationValidityScore(
                edge.validity
            );

        float strengthScore =
            GetStrengthWeight(
                edge.strength
            );

        float totalWeight =
            Mathf.Max(
                0.0001f,
                validityWeight +
                strengthWeight
            );

        float normalizedValidityWeight =
            validityWeight / totalWeight;

        float normalizedStrengthWeight =
            strengthWeight / totalWeight;

        float semanticSupport =
            validityScore *
            normalizedValidityWeight

            + strengthScore *
            normalizedStrengthWeight;

        return Mathf.Clamp01(
            semanticSupport
        );
    }

    private float CalculateSourceBudgetScale(
        float edgeQuality,
        float sourceTotal
    )
    {
        if (edgeQuality <= 0f)
            return 0f;

        float safeBudget =
            Mathf.Max(
                0.0001f,
                sourceEvidenceBudget
            );

        if (sourceTotal <= safeBudget)
        {
            return edgeQuality;
        }

        return edgeQuality /
               Mathf.Max(0.0001f, sourceTotal) *
               safeBudget;
    }

    private float ComputeRawScore(
        float reasoningSupportRaw,
        float reasoningOppositionRaw,
        float sequenceSupportRaw
    )
    {
        float totalWeight =
            Mathf.Max(
                0.0001f,
                supportWeight +
                oppositionWeight +
                sequenceWeight
            );

        float normalizedSupportWeight =
            supportWeight / totalWeight;

        float normalizedOppositionWeight =
            oppositionWeight / totalWeight;

        float normalizedSequenceWeight =
            sequenceWeight / totalWeight;

        return
            reasoningSupportRaw *
            normalizedSupportWeight

            - reasoningOppositionRaw *
            normalizedOppositionWeight

            + sequenceSupportRaw *
            normalizedSequenceWeight;
    }

    private float ComputeFinalScore(
        float reasoningSupport,
        float reasoningOpposition,
        float sequenceSupport
    )
    {
        float totalWeight =
            Mathf.Max(
                0.0001f,
                supportWeight +
                oppositionWeight +
                sequenceWeight
            );

        float normalizedSupportWeight =
            supportWeight / totalWeight;

        float normalizedOppositionWeight =
            oppositionWeight / totalWeight;

        float normalizedSequenceWeight =
            sequenceWeight / totalWeight;

        float score =
            reasoningSupport *
            normalizedSupportWeight

            - reasoningOpposition *
            normalizedOppositionWeight

            + sequenceSupport *
            normalizedSequenceWeight;

        return Mathf.Clamp(score, -1f, 1f);
    }

    private float SaturatePositive(float rawValue)
    {
        if (rawValue <= 0f)
            return 0f;

        float safeTemperature =
            Mathf.Max(
                0.0001f,
                adjustmentTemperature
            );

        return 1f -
               Mathf.Exp(
                   -rawValue / safeTemperature
               );
    }

    private float ComputeGraphReliability(
        List<ReasoningEdgeEvaluation> edges
    )
    {
        if (edges == null || edges.Count == 0)
            return 0f;

        float meanEdgeQuality =
            edges.Average(edge =>
                Mathf.Clamp01(
                    edge.semanticSupport
                )
            );

        float validEdgeRatio =
            ComputeValidEdgeRatio(edges);

        float patternDiversity =
            ComputePatternDiversity(edges);

        float totalWeight =
            Mathf.Max(
                0.0001f,
                edgeQualityWeight +
                validEdgeRatioWeight +
                patternDiversityWeight
            );

        float reliability =
            meanEdgeQuality *
            (edgeQualityWeight / totalWeight)

            + validEdgeRatio *
            (validEdgeRatioWeight / totalWeight)

            + patternDiversity *
            (patternDiversityWeight / totalWeight);

        return Mathf.Clamp01(reliability);
    }

    private float ComputeValidEdgeRatio(
        List<ReasoningEdgeEvaluation> edges
    )
    {
        if (edges == null || edges.Count == 0)
            return 0f;

        float validWeightSum = 0f;

        foreach (ReasoningEdgeEvaluation edge in edges)
        {
            validWeightSum +=
                GetValidityWeight(edge.validity);
        }

        return Mathf.Clamp01(
            validWeightSum / edges.Count
        );
    }

    private float ComputePatternDiversity(
        List<ReasoningEdgeEvaluation> edges
    )
    {
        if (edges == null || edges.Count == 0)
            return 0f;

        int uniquePatternCount =
            edges
                .Select(CreatePatternKey)
                .Distinct(
                    StringComparer.OrdinalIgnoreCase
                )
                .Count();

        return Mathf.Clamp01(
            (float)uniquePatternCount /
            edges.Count
        );
    }

    private string CreatePatternKey(
        ReasoningEdgeEvaluation edge
    )
    {
        string reasoning =
            string.IsNullOrWhiteSpace(
                edge.playerReasoningType
            )
                ? PlayerReasoningTypes.Unknown
                : edge.playerReasoningType.Trim();

        string hypothesisProfile =
            edge.hypothesisEvaluations == null
                ? "NO_PROFILE"
                : string.Join(
                    "|",
                    edge.hypothesisEvaluations
                        .OrderBy(evaluation =>
                            evaluation.hypothesisId
                        )
                        .Select(evaluation =>
                            $"{evaluation.hypothesisId}:" +
                            $"{evaluation.effect}:" +
                            $"{evaluation.semanticFit}"
                        )
                );

        return
            $"{reasoning}|{hypothesisProfile}";
    }

    private Dictionary<string, HypothesisReasoningAccumulator>
        CreateHypothesisAccumulators()
    {
        Dictionary<string, HypothesisReasoningAccumulator>
            result =
                new Dictionary<
                    string,
                    HypothesisReasoningAccumulator
                >(
                    StringComparer.OrdinalIgnoreCase
                );

        foreach (Hypothesis hypothesis in
                 HypothesisStateManager.Instance.hypotheses)
        {
            if (hypothesis == null ||
                string.IsNullOrWhiteSpace(hypothesis.id))
            {
                continue;
            }

            result[hypothesis.id] =
                new HypothesisReasoningAccumulator();
        }

        return result;
    }

    private string BuildHypothesisReason(
        string hypothesisId,
        List<ReasoningEdgeEvaluation> edges
    )
    {
        if (edges == null || edges.Count == 0)
            return string.Empty;

        List<string> reasons =
            edges
                .Where(edge =>
                    edge.hypothesisEvaluations != null
                )
                .SelectMany(edge =>
                    edge.hypothesisEvaluations
                        .Where(evaluation =>
                            evaluation != null &&
                            string.Equals(
                                evaluation.hypothesisId,
                                hypothesisId,
                                StringComparison.OrdinalIgnoreCase
                            ) &&
                            evaluation.effect !=
                                HypothesisEffectTypes.Neutral &&
                            !string.IsNullOrWhiteSpace(
                                evaluation.reason
                            )
                        )
                        .Select(evaluation =>
                            new
                            {
                                edge.effectiveAdjustmentWeight,
                                Reason =
                                    evaluation.reason.Trim()
                            }
                        )
                )
                .OrderByDescending(item =>
                    item.effectiveAdjustmentWeight
                )
                .Take(3)
                .Select(item => item.Reason)
                .ToList();

        return string.Join(" ", reasons);
    }

    private ReasoningAdjustmentResponse
        BuildEmptyAdjustmentResponse()
    {
        List<ReasoningAdjustmentItem> adjustments =
            new List<ReasoningAdjustmentItem>();

        foreach (Hypothesis hypothesis in
                 HypothesisStateManager.Instance.hypotheses)
        {
            if (hypothesis == null ||
                string.IsNullOrWhiteSpace(hypothesis.id))
            {
                continue;
            }

            adjustments.Add(
                new ReasoningAdjustmentItem
                {
                    hypothesisId = hypothesis.id,
                    reasoningSupportRaw = 0f,
                    reasoningOppositionRaw = 0f,
                    sequenceSupportRaw = 0f,
                    reasoningSupport = 0f,
                    reasoningOpposition = 0f,
                    sequenceSupport = 0f,
                    rawScore = 0f,
                    score = 0f,
                    reason = string.Empty
                }
            );
        }

        return new ReasoningAdjustmentResponse
        {
            adjustments = adjustments,
            edgeEvaluations =
                new List<ReasoningEdgeEvaluation>(),
            graphReliability = 0f,
            validEdgeRatio = 0f,
            patternDiversity = 0f
        };
    }

    public Dictionary<string, float>
        ConvertResponseToDictionary(
            ReasoningAdjustmentResponse response
        )
    {
        Dictionary<string, float> result =
            new Dictionary<string, float>();

        foreach (Hypothesis hypothesis in
                 HypothesisStateManager.Instance.hypotheses)
        {
            if (hypothesis == null ||
                string.IsNullOrWhiteSpace(hypothesis.id))
            {
                continue;
            }

            result[hypothesis.id] = 0f;
        }

        if (response?.adjustments == null)
            return result;

        foreach (ReasoningAdjustmentItem item
                 in response.adjustments)
        {
            if (item == null ||
                string.IsNullOrWhiteSpace(
                    item.hypothesisId
                ))
            {
                continue;
            }

            if (!result.ContainsKey(item.hypothesisId))
                continue;

            result[item.hypothesisId] =
                Mathf.Clamp(
                    item.score,
                    -1f,
                    1f
                );
        }

        return result;
    }

    public Dictionary<string, float>
        BuildGraphReliabilityDictionary(
            ReasoningAdjustmentResponse response
        )
    {
        Dictionary<string, float> result =
            new Dictionary<string, float>();

        float reliability =
            response != null
                ? Mathf.Clamp01(
                    response.graphReliability
                )
                : 0f;

        foreach (Hypothesis hypothesis in
                 HypothesisStateManager.Instance.hypotheses)
        {
            if (hypothesis == null ||
                string.IsNullOrWhiteSpace(hypothesis.id))
            {
                continue;
            }

            result[hypothesis.id] =
                reliability;
        }

        return result;
    }

    private float GetInterpretationValidityScore(
        string validity
    )
    {
        switch (NormalizeValidity(validity))
        {
            case ReasoningValidity.Valid:
                return 1f;

            case ReasoningValidity.PartiallySupported:
                return 0.5f;

            case ReasoningValidity.Invalid:
            default:
                return 0f;
        }
    }

    private float GetValidityWeight(string validity)
    {
        switch (NormalizeValidity(validity))
        {
            case ReasoningValidity.Valid:
                return 1f;

            case ReasoningValidity.PartiallySupported:
                return 0.6f;

            case ReasoningValidity.Invalid:
            default:
                return 0f;
        }
    }

    private float GetStrengthWeight(string strength)
    {
        switch (NormalizeStrength(strength))
        {
            case ReasoningStrength.Weak:
                return 0.25f;

            case ReasoningStrength.Moderate:
                return 0.50f;

            case ReasoningStrength.Strong:
                return 0.75f;

            case ReasoningStrength.Core:
                return 1f;

            case ReasoningStrength.Unsupported:
            default:
                return 0f;
        }
    }

    private string NormalizeValidity(
        string validity
    )
    {
        if (string.IsNullOrWhiteSpace(validity))
            return ReasoningValidity.Invalid;

        string normalized =
            validity
                .Trim()
                .Replace("_", "")
                .Replace("-", "")
                .Replace(" ", "")
                .ToLowerInvariant();

        switch (normalized)
        {
            case "valid":
                return ReasoningValidity.Valid;

            case "partiallysupported":
            case "partial":
            case "weaklysupported":
                return ReasoningValidity.PartiallySupported;

            default:
                return ReasoningValidity.Invalid;
        }
    }

    private string NormalizeStrength(
        string strength
    )
    {
        if (string.IsNullOrWhiteSpace(strength))
            return ReasoningStrength.Unsupported;

        switch (strength.Trim().ToLowerInvariant())
        {
            case "unsupported":
            case "none":
            case "neutral":
                return ReasoningStrength.Unsupported;

            case "weak":
                return ReasoningStrength.Weak;

            case "moderate":
                return ReasoningStrength.Moderate;

            case "strong":
                return ReasoningStrength.Strong;

            case "core":
                return ReasoningStrength.Core;

            default:
                return ReasoningStrength.Unsupported;
        }
    }

    private string NormalizePrimaryIssue(
        string primaryIssue
    )
    {
        if (string.IsNullOrWhiteSpace(primaryIssue))
            return InterpretationIssueTypes.None;

        string normalized =
            primaryIssue
                .Trim()
                .Replace("_", "")
                .Replace("-", "")
                .Replace(" ", "")
                .ToLowerInvariant();

        switch (normalized)
        {
            case "none":
                return InterpretationIssueTypes.None;

            case "reasoningmismatch":
            case "wrongreasoning":
                return InterpretationIssueTypes.ReasoningMismatch;

            case "wrongdirection":
            case "reverseddirection":
                return InterpretationIssueTypes.WrongDirection;

            case "uncertaindirection":
            case "ambiguousdirection":
                return InterpretationIssueTypes.UncertainDirection;

            case "missingintermediatestep":
            case "missingstep":
            case "inferentialgap":
                return InterpretationIssueTypes.MissingIntermediateStep;

            case "weakgrounding":
            case "insufficientgrounding":
                return InterpretationIssueTypes.WeakGrounding;

            case "contextmismatch":
            case "contextinconsistency":
                return InterpretationIssueTypes.ContextMismatch;

            case "unsupportedconnection":
            default:
                return InterpretationIssueTypes.UnsupportedConnection;
        }
    }

    private string NormalizePlayerReasoningType(
        string reasoningType
    )
    {
        if (string.IsNullOrWhiteSpace(reasoningType))
            return PlayerReasoningTypes.Unknown;

        string normalized =
            reasoningType
                .Trim()
                .Replace("_", "")
                .Replace("-", "")
                .Replace(" ", "")
                .ToLowerInvariant();

        switch (normalized)
        {
            case "leadsto":
            case "causalsequence":
            case "sequence":
                return PlayerReasoningTypes.LeadsTo;

            case "conflictswith":
            case "contradiction":
                return PlayerReasoningTypes.ConflictsWith;

            case "consistentwith":
            case "mutualsupport":
            case "sharedexplanation":
                return PlayerReasoningTypes.ConsistentWith;

            default:
                return PlayerReasoningTypes.Unknown;
        }
    }

    private string NormalizeHypothesisEffect(
        string effect
    )
    {
        if (string.IsNullOrWhiteSpace(effect))
            return HypothesisEffectTypes.Neutral;

        switch (effect.Trim().ToLowerInvariant())
        {
            case "support":
            case "supports":
            case "positive":
                return HypothesisEffectTypes.Support;

            case "oppose":
            case "opposes":
            case "opposition":
            case "contradict":
            case "contradicts":
            case "negative":
                return HypothesisEffectTypes.Oppose;

            case "neutral":
            case "none":
            default:
                return HypothesisEffectTypes.Neutral;
        }
    }

    private string ExtractJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new Exception(
                "The response is empty."
            );
        }

        string cleaned = raw.Trim();

        if (cleaned.StartsWith("```json"))
        {
            cleaned =
                cleaned.Substring("```json".Length);
        }
        else if (cleaned.StartsWith("```"))
        {
            cleaned =
                cleaned.Substring(3);
        }

        if (cleaned.EndsWith("```"))
        {
            cleaned =
                cleaned.Substring(
                    0,
                    cleaned.Length - 3
                );
        }

        int start = cleaned.IndexOf('{');
        int end = cleaned.LastIndexOf('}');

        if (start < 0 || end < start)
        {
            throw new Exception(
                "No valid JSON object was found."
            );
        }

        return cleaned.Substring(
            start,
            end - start + 1
        );
    }

    private void DebugAggregatedResult(
        ReasoningAdjustmentResponse response
    )
    {
        if (response == null)
            return;

        Debug.Log(
            "[ReasoningAdjustmentManager] " +
            $"GraphReliability={response.graphReliability:F3}, " +
            $"ValidEdgeRatio={response.validEdgeRatio:F3}, " +
            $"PatternDiversity={response.patternDiversity:F3}"
        );

        if (response.edgeEvaluations != null)
        {
            foreach (ReasoningEdgeEvaluation edge
                     in response.edgeEvaluations)
            {
                Debug.Log(
                    "[ReasoningEdgeEvaluation] " +
                    $"Edge={edge.edgeId}, " +
                    $"Reasoning={edge.playerReasoningType}, " +
                    $"Validity={edge.validity}, " +
                    $"Strength={edge.strength}, " +
                    $"SemanticSupport={edge.semanticSupport:F3}, " +
                    $"AdjustmentWeight={edge.effectiveAdjustmentWeight:F3}, " +
                    $"PrimaryIssue={edge.primaryIssue}, " +
                    $"Reason={edge.reason}"
                );

                if (edge.hypothesisEvaluations == null)
                    continue;

                foreach (
                    EdgeHypothesisEvaluation evaluation
                    in edge.hypothesisEvaluations
                )
                {
                    Debug.Log(
                        "[EdgeHypothesisEvaluation] " +
                        $"Edge={edge.edgeId}, " +
                        $"H={evaluation.hypothesisId}, " +
                        $"Effect={evaluation.effect}, " +
                        $"SemanticFit={evaluation.semanticFit}, " +
                        $"Reason={evaluation.reason}"
                    );
                }
            }
        }

        if (response.adjustments == null)
            return;

        foreach (ReasoningAdjustmentItem item
                 in response.adjustments)
        {
            Debug.Log(
                "[ReasoningAdjustment] " +
                $"H={item.hypothesisId}, " +
                $"SupportRaw={item.reasoningSupportRaw:F3}, " +
                $"OppositionRaw={item.reasoningOppositionRaw:F3}, " +
                $"SequenceRaw={item.sequenceSupportRaw:F3}, " +
                $"Support={item.reasoningSupport:F3}, " +
                $"Opposition={item.reasoningOpposition:F3}, " +
                $"Sequence={item.sequenceSupport:F3}, " +
                $"RawScore={item.rawScore:F3}, " +
                $"Score={item.score:F3}"
            );
        }
    }
}

#region Response Data

[Serializable]
public class ReasoningGraphEvaluationResponse
{
    public List<ReasoningEdgeEvaluation>
        edgeEvaluations =
            new List<ReasoningEdgeEvaluation>();
}

[Serializable]
public class ReasoningEdgeEvaluation
{
    public string edgeId;

    public string sourceEvidenceId;
    public string targetEvidenceId;

    public string playerReasoningType;

    public string validity;
    public string strength;

    public string primaryIssue;

    public List<EdgeHypothesisEvaluation>
        hypothesisEvaluations =
            new List<EdgeHypothesisEvaluation>();

    public string reason;

    // Computed locally from validity and strength.
    public float semanticSupport;

    // Computed locally after source budget and pattern novelty controls.
    public float effectiveAdjustmentWeight;
}

[Serializable]
public class EdgeHypothesisEvaluation
{
    public string hypothesisId;

    // Support / Oppose / Neutral
    public string effect;

    // Unsupported / Weak / Moderate / Strong / Core
    public string semanticFit;
    public string reason;
}

[Serializable]
public class ReasoningAdjustmentResponse
{
    public List<ReasoningAdjustmentItem> adjustments =  new();
    public List<ReasoningEdgeEvaluation> edgeEvaluations =  new();
    public float graphReliability;
    public float validEdgeRatio;
    public float patternDiversity;
}

[Serializable]
public class ReasoningAdjustmentItem
{
    public string hypothesisId;

    // Raw values before SaturatePositive().
    public float reasoningSupportRaw;
    public float reasoningOppositionRaw;
    public float sequenceSupportRaw;

    // Saturated values currently used by the existing final score.
    public float reasoningSupport;
    public float reasoningOpposition;
    public float sequenceSupport;

    // Weighted score before and after saturation.
    public float rawScore;
    public float score;

    public string reason;
}

[Serializable]
internal class HypothesisReasoningAccumulator
{
    public float supportRaw;
    public float oppositionRaw;
    public float sequenceRaw;
}

#endregion

#region Constants

public static class ReasoningValidity
{
    public const string Valid = "Valid";
    public const string PartiallySupported = "PartiallySupported";
    public const string Invalid = "Invalid";
}

public static class ReasoningStrength
{
    public const string Unsupported = "Unsupported";
    public const string Weak = "Weak";
    public const string Moderate = "Moderate";
    public const string Strong = "Strong";
    public const string Core = "Core";
}

public static class InterpretationIssueTypes
{
    public const string None = "None";
    public const string ReasoningMismatch = "ReasoningMismatch";
    public const string WrongDirection = "WrongDirection";
    public const string UncertainDirection = "UncertainDirection";
    public const string MissingIntermediateStep = "MissingIntermediateStep";
    public const string WeakGrounding = "WeakGrounding";
    public const string ContextMismatch = "ContextMismatch";
    public const string UnsupportedConnection = "UnsupportedConnection";
}

public static class PlayerReasoningTypes
{
    public const string LeadsTo = "LeadsTo";
    public const string ConflictsWith = "ConflictsWith";
    public const string ConsistentWith = "ConsistentWith";
    public const string Unknown = "Unknown";
}

public static class HypothesisEffectTypes
{
    public const string Support = "Support";
    public const string Oppose = "Oppose";
    public const string Neutral = "Neutral";
}

#endregion