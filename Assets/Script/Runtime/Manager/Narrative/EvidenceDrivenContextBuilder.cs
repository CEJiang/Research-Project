using System;
using System.Collections.Generic;
using System.Linq;

public static class EvidenceDrivenContextBuilder
{
    private const float GAP_LOW = 0.08f;
    private const float GAP_MODERATE = 0.20f;

    private const float EPS = 0.1f;
    private const int OSC_WINDOW = 4;
    private const int MAX_SIGNALS = 3;

    private const int EARLY_STEPS_LOCK = 6;
    private const int EARLY_STAGE_MAX = 3;
    private const int MID_STAGE_MAX = 8;

    private const float SOFTMAX_TEMPERATURE = 1.3f;
    private const float EARLY_SOFTMAX_TEMPERATURE = 2.0f;
    private const float EARLY_MAX_GAP_DELTA = 0.08f;
    private const float NARRATIVE_DELTA_CLAMP = 0.35f;

    public static EvidenceDrivenContext Build(
        Dictionary<string, float> currentScores,
        List<Dictionary<string, float>> scoreHistory,
        Dictionary<string, float> prevScores,
        Dictionary<string, float> deltasThisStep,
        Evidence lastEvidence,
        List<ClaimSelectionResult> claimResults)
    {
        if (currentScores == null) throw new ArgumentNullException(nameof(currentScores));
        if (scoreHistory == null) throw new ArgumentNullException(nameof(scoreHistory));
        if (currentScores.Count < 2) throw new ArgumentException("Need >= 2 hypotheses.");

        claimResults ??= new List<ClaimSelectionResult>();

        int steps = scoreHistory.Count;
        bool early = steps <= EARLY_STEPS_LOCK;
        string stage = ComputeStage(steps);

        float temperature = stage == "early"
            ? EARLY_SOFTMAX_TEMPERATURE
            : SOFTMAX_TEMPERATURE;

        Dictionary<string, float> probs = Softmax(currentScores, temperature);

        var sorted = probs
            .Where(kv => !float.IsNaN(kv.Value) && !float.IsInfinity(kv.Value))
            .OrderByDescending(kv => kv.Value)
            .ToList();

        if (sorted.Count < 2)
            throw new ArgumentException("Need at least 2 valid hypothesis probabilities.");

        var top = sorted[0];
        var second = sorted[1];

        float leadGap = top.Value - second.Value;

        float prevGap = 0f;
        if (prevScores != null && prevScores.Count >= 2)
        {
            Dictionary<string, float> prevProbs = Softmax(prevScores, temperature);

            if (prevProbs.TryGetValue(top.Key, out float prevTopP) &&
                prevProbs.TryGetValue(second.Key, out float prevSecondP))
            {
                prevGap = prevTopP - prevSecondP;
            }
        }

        float leadGapDelta = leadGap - prevGap;

        if (early)
            leadGapDelta = Math.Min(leadGapDelta, EARLY_MAX_GAP_DELTA);

        float deltaTopRaw = SafeDelta(deltasThisStep, top.Key);
        float deltaSecondRaw = SafeDelta(deltasThisStep, second.Key);

        float deltaTop = NarrativeDelta(deltaTopRaw);
        float deltaSecond = NarrativeDelta(deltaSecondRaw);

        List<ClaimSignalContext> claimSignals = BuildClaimSignals(claimResults);
        List<HypothesisImpactContext> hypothesisImpacts = BuildHypothesisImpacts(claimResults);

        var (forTop, againstTop) = ExtractForAgainstSignals(
            hypothesisImpacts,
            top.Key,
            MAX_SIGNALS
        );

        string confidence = ComputeConfidenceLevel(leadGap);

        if (early && confidence == "high")
            confidence = "moderate";

        string dominantShift = ComputeDominantShift(deltaTopRaw, EPS);

        if (early && dominantShift == "up" && leadGap < GAP_MODERATE)
            dominantShift = "flat";

        string competitionState = ComputeCompetitionState(leadGap, leadGapDelta, GAP_LOW, EPS);

        if (early && competitionState == "pulling_away")
            competitionState = "stable_gap";

        List<string> tensionTags = BuildTensionTags(forTop, againstTop);

        EvidenceDrivenContext context = new()
        {
            lastEvidenceID = lastEvidence?.evidenceID ?? "unknown_evidence_id",
            lastEvidenceDisplayName = lastEvidence?.displayNameEn ?? "unknown_evidence",
            lastEvidenceZone = lastEvidence != null
                ? ZoneManager.Instance.GetZoneDisplayNameForLLM(lastEvidence.zoneAt)
                : "unknown_zone",
            observationReliabilityScore = lastEvidence?.observationReliabilityScore ?? 0f,

            observedFacts = ExtractObservedFacts(lastEvidence),
            evidenceFeatures = ExtractEvidenceFeatures(lastEvidence),

            committedEvidenceMeaning = BuildCommittedEvidenceMeaning(claimSignals),
            newlyActivatedClaims = claimSignals.Select(c => c.claimID).Distinct().ToList(),
            claimSignals = claimSignals,
            hypothesisImpacts = hypothesisImpacts,

            strengthenedHypotheses = hypothesisImpacts
                .Where(h => h.netImpact > EPS)
                .OrderByDescending(h => h.netImpact)
                .Select(h => h.hypothesisID)
                .ToList(),

            weakenedHypotheses = hypothesisImpacts
                .Where(h => h.netImpact < -EPS)
                .OrderBy(h => h.netImpact)
                .Select(h => h.hypothesisID)
                .ToList(),

            remainingAlternatives = BuildRemainingAlternatives(sorted, top.Key),

            dominantHypothesisId = top.Key,
            challengerHypothesisId = second.Key,

            dominantView = MapHypothesisToText(top.Key),
            challengerView = MapHypothesisToText(second.Key),

            confidenceLevel = confidence,
            leadGap = leadGap,
            leadGapDelta = leadGapDelta,

            deltaDominant = deltaTop,
            deltaChallenger = deltaSecond,

            dominantShift = dominantShift,
            competitionState = competitionState,
            stage = stage,

            signalsForDominant = forTop,
            signalsAgainstDominant = againstTop,

            oscillation = DetectOscillation(scoreHistory, top.Key, OSC_WINDOW, EPS),
            tensionTags = tensionTags
        };

        context.stance = ComputeStance(context, steps);
        context.doubtType = ComputeDoubtType(context);

        context.remainingUncertaintyType = BuildRemainingUncertaintyType(context);
        context.reflectionFocusType = BuildReflectionFocusType(context);
        context.suggestedResponseMode = BuildSuggestedResponseMode(context);

        context.reflectionSummary = BuildReflectionSummary(context);

        return context;
    }

    private static EvidenceReflectionSummary BuildReflectionSummary(EvidenceDrivenContext context)
    {
        return new EvidenceReflectionSummary
        {
            evidenceName = context.lastEvidenceDisplayName,
            evidenceZone = context.lastEvidenceZone,
            stage = context.stage,

            observedFacts = context.observedFacts ?? new List<string>(),
            evidenceFeatures = context.evidenceFeatures ?? new List<string>(),

            activatedClaims = context.claimSignals
                .Select(c => new ReflectionClaimSignal
                {
                    claimID = c.claimID,
                    confidenceLevel = ToClaimConfidenceLevel(c.confidence),
                    basedFeatureIDs = c.basedFeatureIDs ?? new List<string>(),
                    reason = c.reason
                })
                .ToList(),

            reasoningState = new ReflectionReasoningState
            {
                confidenceLevel = context.confidenceLevel,
                competitionState = context.competitionState,
                stance = context.stance,
                doubtType = context.doubtType,
                remainingUncertaintyType = context.remainingUncertaintyType,
                reflectionFocusType = context.reflectionFocusType,
                suggestedResponseMode = context.suggestedResponseMode,
                tensionTags = context.tensionTags ?? new List<string>()
            },

            responseGuidance = new ReflectionResponseGuidance
            {
                avoidDirectAnswer = true,
                avoidHypothesisNames = true,
                avoidAnalysisTone = true,
                preferConcreteSceneDetail = true,
                expressUncertainty = context.stage == "early" || context.stance == "torn"
            },

            bannedExplicitTerms = new List<string>
            {
                "假說", "理論", "可能性", "機率", "分數", "信心",
                "第三方", "外力", "闖入", "故意", "安排", "老化", "忽視",
                "顯示", "代表", "說明", "支持", "反駁", "證明", "推論"
            }
        };
    }

    private static string ToClaimConfidenceLevel(float confidence)
    {
        if (confidence < 0.4f) return "low";
        if (confidence < 0.7f) return "medium";
        return "high";
    }

    private static List<string> ExtractObservedFacts(Evidence evidence)
    {
        if (evidence == null || evidence.facts == null)
            return new List<string>();

        return evidence.facts
            .Where(f => f != null)
            .Select(f => f.descriptionZh)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .ToList();
    }

    private static List<string> ExtractEvidenceFeatures(Evidence evidence)
    {
        if (evidence == null || evidence.features == null)
            return new List<string>();

        return evidence.features
            .Where(f => f != null)
            .Select(f => $"{f.featureID}: {f.description}")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .ToList();
    }

    private static List<ClaimSignalContext> BuildClaimSignals(List<ClaimSelectionResult> claimResults)
    {
        List<ClaimSignalContext> signals = new();

        foreach (ClaimSelectionResult result in claimResults)
        {
            if (result == null || string.IsNullOrEmpty(result.claimID))
                continue;

            Claim claim = ClaimManager.Instance.claims
                .FirstOrDefault(c => c.claimID == result.claimID);

            signals.Add(new ClaimSignalContext
            {
                claimID = result.claimID,
                description = claim?.description ?? "unknown_claim_description",
                confidence = result.confidence,
                basedFeatureIDs = result.basedFeatureIDs ?? new List<string>(),
                reason = result.reason
            });
        }

        return signals
            .OrderByDescending(s => s.confidence)
            .ToList();
    }

    private static List<HypothesisImpactContext> BuildHypothesisImpacts(List<ClaimSelectionResult> claimResults)
    {
        Dictionary<string, HypothesisImpactContext> map = new();

        foreach (ClaimSelectionResult result in claimResults)
        {
            if (result == null || string.IsNullOrEmpty(result.claimID))
                continue;

            Claim claim = ClaimManager.Instance.claims
                .FirstOrDefault(c => c.claimID == result.claimID);

            if (claim == null || claim.effects == null)
                continue;

            foreach (ClaimEffect effect in claim.effects)
            {
                if (effect == null || string.IsNullOrEmpty(effect.hypothesisID))
                    continue;

                if (!map.TryGetValue(effect.hypothesisID, out HypothesisImpactContext impact))
                {
                    impact = new HypothesisImpactContext
                    {
                        hypothesisID = effect.hypothesisID,
                        hypothesisText = MapHypothesisToText(effect.hypothesisID),
                        supportingClaims = new List<string>(),
                        counterClaims = new List<string>(),
                        supportStrength = 0f,
                        counterStrength = 0f,
                        netImpact = 0f
                    };

                    map[effect.hypothesisID] = impact;
                }

                float value = effect.Weight * result.confidence;

                if (effect.polarity == Polarity.Support)
                {
                    impact.supportingClaims.Add(result.claimID);
                    impact.supportStrength += value;
                    impact.netImpact += value;
                }
                else
                {
                    impact.counterClaims.Add(result.claimID);
                    impact.counterStrength += value;
                    impact.netImpact -= value;
                }
            }
        }

        return map.Values
            .OrderByDescending(h => Math.Abs(h.netImpact))
            .ToList();
    }

    private static (List<string> forTop, List<string> againstTop) ExtractForAgainstSignals(
        List<HypothesisImpactContext> hypothesisImpacts,
        string dominantHypothesisId,
        int maxSignalsPerSide)
    {
        HypothesisImpactContext dominantImpact = hypothesisImpacts
            .FirstOrDefault(h => h.hypothesisID == dominantHypothesisId);

        if (dominantImpact == null)
        {
            return (
                new List<string> { "no_claim_signal" },
                new List<string> { "no_claim_signal" }
            );
        }

        List<string> forTop = dominantImpact.supportingClaims
            .Distinct()
            .Take(maxSignalsPerSide)
            .ToList();

        List<string> againstTop = dominantImpact.counterClaims
            .Distinct()
            .Take(maxSignalsPerSide)
            .ToList();

        if (forTop.Count == 0)
            forTop.Add("no_claim_signal");

        if (againstTop.Count == 0)
            againstTop.Add("no_claim_signal");

        return (forTop, againstTop);
    }

    private static string BuildCommittedEvidenceMeaning(List<ClaimSignalContext> claimSignals)
    {
        if (claimSignals == null || claimSignals.Count == 0)
            return "no_clear_claim_signal";

        List<string> topClaims = claimSignals
            .Take(2)
            .Select(c => c.claimID)
            .ToList();

        return string.Join(",", topClaims);
    }

    private static List<string> BuildRemainingAlternatives(
        List<KeyValuePair<string, float>> sorted,
        string dominantHypothesisId)
    {
        return sorted
            .Where(kv => kv.Key != dominantHypothesisId)
            .Take(2)
            .Select(kv => kv.Key)
            .ToList();
    }

    private static string BuildRemainingUncertaintyType(EvidenceDrivenContext context)
    {
        if (context.competitionState == "neck_and_neck")
            return "close_competition";

        if (HasValidSignals(context.signalsAgainstDominant))
            return "counter_signal_present";

        if (context.tensionTags != null && context.tensionTags.Count > 0)
            return "reasoning_tension_present";

        if (context.confidenceLevel == "low")
            return "insufficient_confidence";

        return "residual_uncertainty";
    }

    private static string BuildReflectionFocusType(EvidenceDrivenContext context)
    {
        if (context.competitionState == "neck_and_neck")
            return "compare_dominant_and_challenger";

        if (HasValidSignals(context.signalsAgainstDominant))
            return "notice_counter_signal";

        if (context.tensionTags != null && context.tensionTags.Count > 0)
            return "notice_reasoning_tension";

        if (context.dominantShift == "up")
            return "notice_strengthening_direction";

        if (context.dominantShift == "down")
            return "notice_weakening_direction";

        return "notice_evidence_change";
    }

    private static string BuildSuggestedResponseMode(EvidenceDrivenContext context)
    {
        if (context.stance == "torn")
            return "encourage_comparison";

        if (context.doubtType == "inconsistency")
            return "express_concrete_tension";

        if (HasValidSignals(context.signalsAgainstDominant))
            return "acknowledge_counter_signal";

        if (context.confidenceLevel == "high")
            return "cautious_inner_confirmation";

        return "brief_reflective_reaction";
    }

    private static bool HasValidSignals(List<string> signals)
    {
        return signals != null &&
               signals.Count > 0 &&
               !signals.Contains("no_claim_signal");
    }

    private static float SafeDelta(Dictionary<string, float> deltas, string key)
    {
        if (deltas == null) return 0f;
        if (!deltas.TryGetValue(key, out float value)) return 0f;
        if (float.IsNaN(value) || float.IsInfinity(value)) return 0f;
        return value;
    }

    private static string ComputeStage(int steps)
    {
        if (steps <= EARLY_STAGE_MAX) return "early";
        if (steps <= MID_STAGE_MAX) return "mid";
        return "late";
    }

    private static string ComputeConfidenceLevel(float leadGap)
    {
        if (leadGap < GAP_LOW) return "low";
        if (leadGap < GAP_MODERATE) return "moderate";
        return "high";
    }

    private static string ComputeDominantShift(float deltaTopRaw, float eps)
    {
        if (deltaTopRaw > eps) return "up";
        if (deltaTopRaw < -eps) return "down";
        return "flat";
    }

    private static string ComputeCompetitionState(float leadGap, float leadGapDelta, float gapLow, float eps)
    {
        if (leadGap < gapLow)
            return "neck_and_neck";

        if (leadGapDelta > eps)
            return "pulling_away";

        if (leadGapDelta < -eps)
            return "being_challenged";

        return "stable_gap";
    }

    private static string MapHypothesisToText(string id) => id switch
    {
        "H0" => "routine explanation",
        "H1" => "planned voluntary departure",
        "H2" => "possible forced third-party involvement",
        "H3" => "intentional staging",
        "H4" => "homeowner still inside",
        _ => "uncertain explanation",
    };

    private static Dictionary<string, float> Softmax(Dictionary<string, float> scores, float temperature)
    {
        var valid = scores
            .Where(kv => !float.IsNaN(kv.Value) && !float.IsInfinity(kv.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        if (valid.Count == 0)
            return new Dictionary<string, float>();

        float max = valid.Values.Max();
        float t = Math.Max(0.0001f, temperature);

        Dictionary<string, float> exps = new(valid.Count);
        double sum = 0.0;

        foreach (var kv in valid)
        {
            double z = (kv.Value - max) / t;
            double e = Math.Exp(z);
            exps[kv.Key] = (float)e;
            sum += e;
        }

        float denom = (float)Math.Max(0.0001, sum);

        Dictionary<string, float> probs = new(valid.Count);

        foreach (var kv in exps)
        {
            probs[kv.Key] = kv.Value / denom;
        }

        return probs;
    }

    private static float NarrativeDelta(float raw)
    {
        raw = Clamp(raw, -NARRATIVE_DELTA_CLAMP, NARRATIVE_DELTA_CLAMP);
        return (float)Math.Tanh(raw);
    }

    private static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private static List<string> BuildTensionTags(List<string> forTop, List<string> againstTop)
    {
        List<string> tags = new();

        bool intrusion =
            forTop.Contains("CLAIM_Forced_Access") ||
            forTop.Contains("CLAIM_Boundary_Breach");

        bool agingOrNeglect =
            againstTop.Contains("CLAIM_Aging_Or_Neglect");

        if (intrusion && agingOrNeglect)
            tags.Add("intrusion_vs_routine_explanation");

        bool stagedDamage =
            forTop.Contains("CLAIM_Staged_Damage_Possibility");

        bool disorderlyDamage =
            againstTop.Contains("CLAIM_Disorderly_Damage_Pattern");

        if (stagedDamage && disorderlyDamage)
            tags.Add("staging_vs_disorderly_damage");

        if (tags.Count == 0 &&
            againstTop != null &&
            againstTop.Count > 0 &&
            !againstTop.Contains("no_claim_signal"))
        {
            tags.Add("support_vs_counter_pull");
        }

        return tags;
    }

    private static string ComputeStance(EvidenceDrivenContext context, int steps)
    {
        bool hasCounters =
            context.signalsAgainstDominant != null &&
            context.signalsAgainstDominant.Count > 0 &&
            !context.signalsAgainstDominant.Contains("no_claim_signal");

        bool early = steps <= EARLY_STEPS_LOCK;

        if (early)
        {
            if (context.confidenceLevel == "low") return "torn";
            if (hasCounters) return "cautious";
            return "leaning";
        }

        if (context.confidenceLevel == "low")
            return "torn";

        if (context.competitionState == "being_challenged" ||
            context.competitionState == "neck_and_neck" ||
            hasCounters)
            return "cautious";

        if (context.confidenceLevel == "high" &&
            !hasCounters &&
            context.competitionState == "pulling_away")
            return "nearly_sure";

        if (context.confidenceLevel == "high" && !hasCounters)
            return "strong_pull";

        return "leaning";
    }

    private static string ComputeDoubtType(EvidenceDrivenContext context)
    {
        if (context.tensionTags != null && context.tensionTags.Count > 0)
            return "inconsistency";

        if (context.oscillation)
            return "missing_link";

        if (context.confidenceLevel == "low")
            return "lack_of_confirmation";

        return "minor_residual_doubt";
    }

    private static bool DetectOscillation(
        List<Dictionary<string, float>> history,
        string key,
        int window,
        float eps)
    {
        if (history == null || history.Count < 3)
            return false;

        int take = Math.Max(3, Math.Min(window, history.Count));

        float[] recent = history
            .Skip(history.Count - take)
            .Select(h => SafeGet(h, key))
            .ToArray();

        int? lastDir = null;

        for (int i = 1; i < recent.Length; i++)
        {
            float diff = recent[i] - recent[i - 1];
            int dir = diff > eps ? 1 : diff < -eps ? -1 : 0;

            if (dir == 0)
                continue;

            if (lastDir.HasValue && dir != lastDir.Value)
                return true;

            lastDir = dir;
        }

        return false;
    }

    private static float SafeGet(Dictionary<string, float> snapshot, string key)
    {
        if (snapshot == null) return 0f;
        if (!snapshot.TryGetValue(key, out float value)) return 0f;
        if (float.IsNaN(value) || float.IsInfinity(value)) return 0f;
        return value;
    }
}

[Serializable]
public class EvidenceDrivenContext
{
    public string lastEvidenceID;
    public string lastEvidenceDisplayName;
    public string lastEvidenceZone;
    public float observationReliabilityScore;

    public List<string> observedFacts;
    public List<string> evidenceFeatures;

    public string committedEvidenceMeaning;
    public List<string> newlyActivatedClaims;
    public List<ClaimSignalContext> claimSignals;
    public List<HypothesisImpactContext> hypothesisImpacts;

    public List<string> strengthenedHypotheses;
    public List<string> weakenedHypotheses;
    public List<string> remainingAlternatives;

    public string dominantHypothesisId;
    public string challengerHypothesisId;

    public string dominantView;
    public string challengerView;

    public string confidenceLevel;
    public float leadGap;
    public float leadGapDelta;

    public float deltaDominant;
    public float deltaChallenger;

    public string dominantShift;
    public string competitionState;
    public string stage;
    public bool oscillation;

    public List<string> signalsForDominant;
    public List<string> signalsAgainstDominant;

    public List<string> tensionTags;
    public string stance;
    public string doubtType;
    public string remainingUncertaintyType;
    public string reflectionFocusType;
    public string suggestedResponseMode;

    public EvidenceReflectionSummary reflectionSummary;
}

[Serializable]
public class EvidenceReflectionSummary
{
    public string evidenceName;
    public string evidenceZone;
    public string stage;

    public List<string> observedFacts;
    public List<string> evidenceFeatures;

    public List<ReflectionClaimSignal> activatedClaims;

    public ReflectionReasoningState reasoningState;
    public ReflectionResponseGuidance responseGuidance;

    public List<string> bannedExplicitTerms;
}

[Serializable]
public class ReflectionClaimSignal
{
    public string claimID;
    public string confidenceLevel;
    public List<string> basedFeatureIDs;
    public string reason;
}

[Serializable]
public class ReflectionReasoningState
{
    public string confidenceLevel;
    public string competitionState;
    public string stance;
    public string doubtType;
    public string remainingUncertaintyType;
    public string reflectionFocusType;
    public string suggestedResponseMode;
    public List<string> tensionTags;
}

[Serializable]
public class ReflectionResponseGuidance
{
    public bool avoidDirectAnswer;
    public bool avoidHypothesisNames;
    public bool avoidAnalysisTone;
    public bool preferConcreteSceneDetail;
    public bool expressUncertainty;
}

[Serializable]
public class ClaimSignalContext
{
    public string claimID;
    public string description;
    public float confidence;
    public List<string> basedFeatureIDs;
    public string reason;
}

[Serializable]
public class HypothesisImpactContext
{
    public string hypothesisID;
    public string hypothesisText;

    public List<string> supportingClaims;
    public List<string> counterClaims;

    public float supportStrength;
    public float counterStrength;
    public float netImpact;
}