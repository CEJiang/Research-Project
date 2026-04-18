using System;
using System.Collections.Generic;
using UnityEngine;

public class HypothesisStateManager : Singleton<HypothesisStateManager>
{
    [Header("Definitions")]
    public List<Hypothesis> hypotheses = new();
    public Dictionary<string, HypothesisRuntimeState> hypothesisRuntimeStates = new();

    [Header("Final Score")]
    [SerializeField] private float ehTemperature = 3f;   // τ
    [SerializeField, Range(0f, 1f)] private float gamma = 0.5f;

    [Header("Inner Voice Context")]
    [SerializeField] private int historyMax = 50;
    private readonly List<Dictionary<string, float>> scoreHistory = new();

    public InnerVoiceContext latestInnerVoiceContext;
    public Evidence lastEvidence;
    public List<ClaimSelectionResult> latestClaimResults;

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
            hypothesisRuntimeStates[hypothesis.id] = new HypothesisRuntimeState(hypothesis.id);
        }

        PushScoreSnapshot(GetCurrentHypothesisState());
    }

    // -----------------------------
    // 1) Evidence-driven update
    // -----------------------------
    public void UpdatePropensityScore(Dictionary<string, float> propensityScores)
    {
        foreach (var kvp in propensityScores)
        {
            string hypothesisId = kvp.Key;
            float score = kvp.Value;

            if (!hypothesisRuntimeStates.ContainsKey(hypothesisId))
                continue;

            hypothesisRuntimeStates[hypothesisId].propensityScore += score;
        }

        ComputeFinalScore();
    }

    // -----------------------------
    // 2) Reasoning adjustment update
    // -----------------------------
    public void UpdateReasoningAdjustment(Dictionary<string, float> reasoningAdjustments)
    {
        foreach (var state in hypothesisRuntimeStates.Values)
        {
            state.reasoningAdjustment = 0f;
        }

        foreach (var kvp in reasoningAdjustments)
        {
            string hypothesisId = kvp.Key;
            float adjustment = kvp.Value;

            if (!hypothesisRuntimeStates.ContainsKey(hypothesisId))
                continue;

            // Ah 理論上應落在 [-1, 1]
            hypothesisRuntimeStates[hypothesisId].reasoningAdjustment =
                Mathf.Clamp(adjustment, -1f, 1f);
        }

        ComputeFinalScore();
    }

    // -----------------------------
    // 3) Recalculate final score only
    // -----------------------------
    public void ComputeFinalScore()
    {
        foreach (var kvp in hypothesisRuntimeStates)
        {
            HypothesisRuntimeState state = kvp.Value;

            float ehRaw = state.propensityScore;
            float ah = Mathf.Clamp(state.reasoningAdjustment, -1f, 1f);

            float safeTemperature = Mathf.Max(0.0001f, ehTemperature);

            // Normalize Eh with tanh => [-1, 1]
            float ehNorm = (float)Math.Tanh(ehRaw / safeTemperature);

            // Evidence Correction gate:
            // Eh 越接近 0，Ah 影響越大
            // Eh 越接近 ±1，Ah 影響越小
            float correctionGate = 1f - (ehNorm * ehNorm);

            // Final = evidence basis + bounded reasoning correction
            float final = ehNorm + gamma * ah * correctionGate;

            // Clamp final score to [-1, 1]
            state.finalScore = Mathf.Clamp(final, -1f, 1f);

            Debug.Log(
                $"[HypothesisState] H={state.hypothesisId}, " +
                $"EhRaw={ehRaw:F3}, EhNorm={ehNorm:F3}, Ah={ah:F3}, " +
                $"gamma={gamma:F2}, gate={correctionGate:F3}, Final={state.finalScore:F3}"
            );
        }
    }

    // -----------------------------
    // 4) Commit one meaningful step
    // -----------------------------
    public void CommitInnerVoiceStep()
    {
        Dictionary<string, float> currentScores = GetCurrentHypothesisState();

        Dictionary<string, float> prevScores =
            scoreHistory.Count > 0
            ? new Dictionary<string, float>(scoreHistory[^1])
            : CreateZeroScores();

        Dictionary<string, float> deltasThisStep = GetDeltas(prevScores, currentScores);

        latestInnerVoiceContext = InnerVoiceContextBuilder.Build(
            currentScores,
            scoreHistory,
            prevScores,
            deltasThisStep,
            lastEvidence,
            latestClaimResults
        );

        PushScoreSnapshot(currentScores);
    }

    public Dictionary<string, float> ComputePropensityScore(
    List<ClaimSelectionResult> claimSelectionResults,
    Evidence item)
    {
        if (claimSelectionResults == null || claimSelectionResults.Count == 0)
        {
            lastEvidence = item;
            latestClaimResults = claimSelectionResults;
            return new Dictionary<string, float>();
        }

        lastEvidence = item;
        latestClaimResults = claimSelectionResults;

        // 最終回傳：每個 hypothesis 的本次 propensity delta
        Dictionary<string, float> propensityScores = new();

        // 中間聚合：同一 hypothesis × 同一 semantic group 先累積
        Dictionary<string, float> groupedScores = new();

        foreach (ClaimSelectionResult result in claimSelectionResults)
        {
            if (!ClaimManager.Instance.claimDictionary.TryGetValue(result.claimId, out Claim claim))
                continue;

            string semanticGroup = GetSemanticGroup(claim.id);

            foreach (var effect in claim.effects)
            {
                float baseChange = effect.weight * result.strength;

                bool claimSupports = effect.polarity == Polarity.Support;
                bool evidenceSupports = IsSupport(result.polarity);

                float sign = (claimSupports == evidenceSupports) ? +1f : -1f;
                float propensityChange = sign * baseChange;

                // key = hypothesisId + semanticGroup
                string groupedKey = $"{effect.hypothesisId}|{semanticGroup}";

                if (!groupedScores.ContainsKey(groupedKey))
                {
                    groupedScores[groupedKey] = 0f;
                }

                groupedScores[groupedKey] += propensityChange;

                Debug.Log(
                    $"[HypothesisState][RawGroupAccum] Claim={claim.id}, Group={semanticGroup}, " +
                    $"H={effect.hypothesisId}, baseChange={baseChange:F3}, sign={sign:F1}, " +
                    $"delta={propensityChange:F3}, groupedNow={groupedScores[groupedKey]:F3}"
                );
            }
        }
        

        // 對每個 group 做 clamp，避免同類 claim 重複加太多/扣太多
        foreach (var kvp in groupedScores)
        {
            string groupedKey = kvp.Key;
            float rawGroupedScore = kvp.Value;

            string[] parts = groupedKey.Split('|');
            string hypothesisId = parts[0];
            string semanticGroup = parts[1];

            float clampedGroupedScore = Mathf.Clamp(rawGroupedScore, -1f, 1f);

            if (!propensityScores.ContainsKey(hypothesisId))
            {
                propensityScores[hypothesisId] = 0f;
            }

            propensityScores[hypothesisId] += clampedGroupedScore;

            Debug.Log(
                $"[HypothesisState][GroupClamped] H={hypothesisId}, Group={semanticGroup}, " +
                $"raw={rawGroupedScore:F3}, clamped={clampedGroupedScore:F3}, " +
                $"propensityNow={propensityScores[hypothesisId]:F3}"
            );
        }

        return propensityScores;
    }

    private string GetSemanticGroup(string claimId)
    {
        if (string.IsNullOrWhiteSpace(claimId))
            return "Unknown";

        // 例如:
        // C2a1 -> C2a
        // C2b4 -> C2b
        // C3c2 -> C3c
        if (claimId.Length >= 3)
        {
            return claimId.Substring(0, 3);
        }

        return claimId;
    }

    private bool IsSupport(string polarity)
    {
        if (string.IsNullOrWhiteSpace(polarity)) return false;
        return polarity.Trim().Equals("support", StringComparison.OrdinalIgnoreCase);
    }

    public Dictionary<string, float> GetCurrentHypothesisState()
    {
        Dictionary<string, float> state = new();

        foreach (var hypothesis in hypothesisRuntimeStates.Values)
        {
            state[hypothesis.hypothesisId] = hypothesis.finalScore;
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
            state[hypothesis.hypothesisId] = hypothesis.reasoningAdjustment;
        }

        return state;
    }

    private void PushScoreSnapshot(Dictionary<string, float> scores)
    {
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
        Dictionary<string, float> current)
    {
        Dictionary<string, float> deltas = new();

        foreach (var kvp in current)
        {
            string hypothesisId = kvp.Key;
            float currentScore = kvp.Value;
            float prevScore = prev.ContainsKey(hypothesisId) ? prev[hypothesisId] : 0f;
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
    public float finalScore;

    public HypothesisRuntimeState(string hypothesisId)
    {
        this.hypothesisId = hypothesisId;
        propensityScore = 0f;
        reasoningAdjustment = 0f;
        finalScore = 0f;
    }
}