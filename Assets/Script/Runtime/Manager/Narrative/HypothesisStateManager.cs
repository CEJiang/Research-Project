using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HypothesisStateManager : Singleton<HypothesisStateManager>
{
    [Header("Definitions")]
    public List<Hypothesis> hypotheses = new();
    public Dictionary<string, HypothesisRuntimeState> hypothesisRuntimeStates = new();

    [Header("Hypothesis Evaluation Score")]
    [SerializeField] private float ehTemperature = 3f;
    [SerializeField, Range(0f, 1f)] private float gamma = 0.5f;

    [Header("Reflective Voice Context")]
    [SerializeField] private int historyMax = 50;
    private readonly List<Dictionary<string, float>> scoreHistory = new();

    public EvidenceDrivenContext latestEvidenceDrivenContext;
    public Evidence latestEvidence;
    public List<ClaimSelectionResult> latestClaimResults = new();

    void Start()
    {
        LoadHypotheses();
    }

    public void LoadHypotheses()
    {
        hypotheses.Clear();
        hypothesisRuntimeStates.Clear();
        scoreHistory.Clear();

        Hypothesis[] loaded = Resources.LoadAll<Hypothesis>("Hypothesis");
        hypotheses.AddRange(loaded);

        foreach (var hypothesis in hypotheses)
        {
            if (hypothesis == null || string.IsNullOrEmpty(hypothesis.id))
                continue;

            hypothesisRuntimeStates[hypothesis.id] =
                new HypothesisRuntimeState(hypothesis.id);
        }

        PushScoreSnapshot(GetCurrentHypothesisState());
    }

    public void UpdatePropensityScore(Dictionary<string, float> propensityScores)
    {
        if (propensityScores == null)
            return;

        foreach (var kvp in propensityScores)
        {
            if (!hypothesisRuntimeStates.ContainsKey(kvp.Key))
                continue;

            hypothesisRuntimeStates[kvp.Key].propensityScore += kvp.Value;
        }

        ComputeEvaluationScore();
    }

    public void UpdateReasoningAdjustment(
        Dictionary<string, float> reasoningAdjustments,
        Dictionary<string, float> graphReliabilities = null
    )
    {
        foreach (var state in hypothesisRuntimeStates.Values)
        {
            state.reasoningAdjustment = 0f;
            state.graphReliability = 1f;
        }

        if (reasoningAdjustments == null)
        {
            ComputeEvaluationScore();
            return;
        }

        foreach (var kvp in reasoningAdjustments)
        {
            if (!hypothesisRuntimeStates.ContainsKey(kvp.Key))
                continue;

            hypothesisRuntimeStates[kvp.Key].reasoningAdjustment =
                Mathf.Clamp(kvp.Value, -1f, 1f);

            if (graphReliabilities != null &&
                graphReliabilities.TryGetValue(kvp.Key, out float reliability))
            {
                hypothesisRuntimeStates[kvp.Key].graphReliability =
                    Mathf.Clamp01(reliability);
            }
        }

        ComputeEvaluationScore();
    }

    public void ComputeEvaluationScore()
    {
        foreach (var kvp in hypothesisRuntimeStates)
        {
            HypothesisRuntimeState state = kvp.Value;

            float ehRaw = state.propensityScore;
            float ah = Mathf.Clamp(state.reasoningAdjustment, -1f, 1f);
            float rGraph = Mathf.Clamp01(state.graphReliability);

            float safeTemperature = Mathf.Max(0.0001f, ehTemperature);
            float ehNorm = (float)Math.Tanh(ehRaw / safeTemperature);

            float evaluationScore = ehNorm + gamma * rGraph * ah;

            state.evaluationScore = Mathf.Clamp(evaluationScore, -1f, 1f);

            Debug.Log(
                $"[HypothesisState] H={state.hypothesisId}, " +
                $"EhRaw={ehRaw:F3}, EhNorm={ehNorm:F3}, Ah={ah:F3}, " +
                $"RGraph={rGraph:F3}, gamma={gamma:F2}, " +
                $"Evaluation={state.evaluationScore:F3}"
            );
        }
    }

    public void CommitEvidenceDrivenStep()
    {
        Dictionary<string, float> currentScores = GetCurrentHypothesisState();

        Dictionary<string, float> prevScores =
            scoreHistory.Count > 0
                ? new Dictionary<string, float>(scoreHistory[^1])
                : CreateZeroScores();

        Dictionary<string, float> deltasThisStep =
            GetDeltas(prevScores, currentScores);

        latestEvidenceDrivenContext = EvidenceDrivenContextBuilder.Build(
            currentScores,
            scoreHistory,
            prevScores,
            deltasThisStep,
            latestEvidence,
            latestClaimResults
        );

        PushScoreSnapshot(currentScores);
    }

    public Dictionary<string, float> ComputePropensityScore(
        List<ClaimSelectionResult> claimSelectionResults,
        Evidence evidence
    )
    {
        latestEvidence = evidence;
        latestClaimResults =
            claimSelectionResults ?? new List<ClaimSelectionResult>();

        Dictionary<string, float> propensityScores = new();

        if (latestClaimResults.Count == 0)
        {
            return propensityScores;
        }

        Dictionary<string, Claim> claimDict = ClaimManager.Instance.claims
            .Where(c => c != null && !string.IsNullOrEmpty(c.claimID))
            .GroupBy(c => c.claimID)
            .ToDictionary(g => g.Key, g => g.First());

        // 先過濾出有效、而且能在 Claim Library 中找到的結果。
        List<ClaimSelectionResult> validResults = latestClaimResults
            .Where(result =>
                result != null &&
                !string.IsNullOrEmpty(result.claimID) &&
                claimDict.ContainsKey(result.claimID) &&
                result.confidence > 0f
            )
            .ToList();

        if (validResults.Count == 0)
        {
            return propensityScores;
        }

        // 同一個 Evidence 的 Claim Budget 固定為 1。
        const float evidenceBudget = 1f;

        // 將所有有效 Claim confidence 的總和算出來。
        float totalConfidence = validResults.Sum(result =>
            Mathf.Clamp01(result.confidence)
        );

        if (totalConfidence <= Mathf.Epsilon)
        {
            return propensityScores;
        }

        foreach (var result in validResults)
        {
            Claim claim = claimDict[result.claimID];

            if (claim.effects == null || claim.effects.Count == 0)
                continue;

            float clampedConfidence = Mathf.Clamp01(result.confidence);

            // 所有 Claim 的 normalizedConfidence 總和為 1。
            float normalizedConfidence =
                clampedConfidence / totalConfidence;

            float claimBudget =
                normalizedConfidence * evidenceBudget * result.InformationValue;

            // Debug.Log(
            //     $"[EvidenceBudget] Evidence={evidence?.DisplayName}, " +
            //     $"Claim={result.claimID}, " +
            //     $"RawConfidence={clampedConfidence:F3}, " +
            //     $"NormalizedConfidence={normalizedConfidence:F3}, " +
            //     $"ClaimBudget={claimBudget:F3}"
            // );

            foreach (var effect in claim.effects)
            {
                if (effect == null ||
                    string.IsNullOrEmpty(effect.hypothesisID))
                {
                    continue;
                }

                float sign =
                    effect.polarity == Polarity.Support
                        ? 1f
                        : -1f;

                float scoreContribution =
                    sign *
                    effect.Weight *
                    claimBudget;

                if (!propensityScores.ContainsKey(effect.hypothesisID))
                {
                    propensityScores[effect.hypothesisID] = 0f;
                }

                propensityScores[effect.hypothesisID] +=
                    scoreContribution;

                // Debug.Log(
                //     $"[PropensityContribution] " +
                //     $"Evidence={evidence?.DisplayName}, " +
                //     $"Claim={result.claimID}, " +
                //     $"H={effect.hypothesisID}, " +
                //     $"Polarity={effect.polarity}, " +
                //     $"EffectWeight={effect.Weight:F3}, " +
                //     $"ClaimBudget={claimBudget:F3}, " +
                //     $"Contribution={scoreContribution:F3}"
                // );
            }
        }

        return propensityScores;
    }

    public Dictionary<string, float> GetCurrentHypothesisState()
    {
        Dictionary<string, float> state = new();

        foreach (var hypothesis in hypothesisRuntimeStates.Values)
        {
            state[hypothesis.hypothesisId] = hypothesis.evaluationScore;
        }

        return state;
    }

    public Dictionary<string, float> GetPropensityScores()
    {
        Dictionary<string, float> state = new();

        foreach (var hypothesis in hypothesisRuntimeStates.Values)
        {
            state[hypothesis.hypothesisId] = hypothesis.propensityScore;
        }

        return state;
    }

    public Dictionary<string, float> GetReasoningAdjustments()
    {
        Dictionary<string, float> state = new();

        foreach (var hypothesis in hypothesisRuntimeStates.Values)
        {
            state[hypothesis.hypothesisId] =
                hypothesis.reasoningAdjustment;
        }

        return state;
    }

    public Dictionary<string, float> GetGraphReliabilities()
    {
        Dictionary<string, float> state = new();

        foreach (var hypothesis in hypothesisRuntimeStates.Values)
        {
            state[hypothesis.hypothesisId] =
                hypothesis.graphReliability;
        }

        return state;
    }

    private void PushScoreSnapshot(Dictionary<string, float> scores)
    {
        if (scores == null)
            return;

        scoreHistory.Add(new Dictionary<string, float>(scores));

        if (scoreHistory.Count > historyMax)
        {
            scoreHistory.RemoveAt(0);
        }
    }

    private Dictionary<string, float> CreateZeroScores()
    {
        Dictionary<string, float> zeroScores = new();

        foreach (var hypothesis in hypothesisRuntimeStates.Values)
        {
            zeroScores[hypothesis.hypothesisId] = 0f;
        }

        return zeroScores;
    }

    public Dictionary<string, float> GetDeltas(
        Dictionary<string, float> prev,
        Dictionary<string, float> current
    )
    {
        Dictionary<string, float> deltas = new();

        if (current == null)
            return deltas;

        foreach (var kvp in current)
        {
            string hypothesisId = kvp.Key;
            float currentScore = kvp.Value;

            float prevScore =
                prev != null && prev.ContainsKey(hypothesisId)
                    ? prev[hypothesisId]
                    : 0f;

            deltas[hypothesisId] = currentScore - prevScore;
        }

        return deltas;
    }

    public IReadOnlyList<Dictionary<string, float>> GetScoreHistory()
    {
        return scoreHistory;
    }
}

[Serializable]
public class HypothesisRuntimeState
{
    public string hypothesisId;

    public float propensityScore;
    public float reasoningAdjustment;
    public float graphReliability;
    public float evaluationScore;

    public HypothesisRuntimeState(string hypothesisId)
    {
        this.hypothesisId = hypothesisId;

        propensityScore = 0f;
        reasoningAdjustment = 0f;
        graphReliability = 1f;
        evaluationScore = 0f;
    }
}