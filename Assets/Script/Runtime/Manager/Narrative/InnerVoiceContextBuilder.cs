using System;
using System.Collections.Generic;
using System.Linq;

public static class InnerVoiceContextBuilder
{
    // leadGap = pTop - pSecond (0~1)
    private const float GAP_LOW = 0.08f;
    private const float GAP_MODERATE = 0.20f;

    private const float EPS = 0.1f;
    private const int OSC_WINDOW = 4;

    private const int MAX_SIGNALS = 3;

    // 前幾步限制語氣，不讓 early 過早太確定
    private const int EARLY_STEPS_LOCK = 6;

    // stage 切分
    private const int EARLY_STAGE_MAX = 3;
    private const int MID_STAGE_MAX = 8;

    // 預設 softmax 溫度
    private const float SOFTMAX_TEMPERATURE = 1.3f;

    // early 額外平滑
    private const float EARLY_SOFTMAX_TEMPERATURE = 2.0f;

    // early 階段最大 gap delta
    private const float EARLY_MAX_GAP_DELTA = 0.08f;

    // 敘事 delta：限制單步語氣跳躍
    private const float NARRATIVE_DELTA_CLAMP = 0.35f;

    private static readonly Dictionary<string, string> ClaimToSignalTag = new()
    {
        // D1: Departure planning
        { "C1a1", "packing_activity_signal" },
        { "C1a2", "selective_emptying_signal" },
        { "C1a3", "high_utility_items_missing_signal" },
        { "C1a4", "travel_itinerary_signal" },

        { "C1b1", "record_destruction_signal" },
        { "C1b2", "digital_sanitization_signal" },
        { "C1b3", "communication_disabled_signal" },
        { "C1b4", "surveillance_interruption_signal" },
        { "C1b5", "voluntary_note_signal" },

        { "C1c1", "passport_left_behind_signal" },
        { "C1c2", "wallet_left_behind_signal" },
        { "C1c3", "keys_left_behind_signal" },
        { "C1c4", "phone_left_behind_signal" },

        // D2: Physical coercion
        { "C2a1", "forced_entry_signal" },
        { "C2a2", "forced_window_entry_signal" },
        { "C2a3", "perimeter_breach_signal" },

        { "C2b1", "indoor_disturbance_signal" },
        { "C2b2", "directional_movement_signal" },
        { "C2b3", "restraint_materials_signal" },
        { "C2b4", "chaotic_tracks_signal" },

        // Counters / Negations
        { "C2c1", "no_foreign_trace_signal" },
        { "C2c2", "no_foreign_bio_signal" },
        { "C2c3", "inventory_consistent_signal" },
        { "C2c4", "intact_entry_points_signal" },
        { "C2c5", "no_drag_signal" },
        { "C2c6", "no_transfer_trail_signal" },

        // D3: Access-control anomaly
        { "C3a1", "tamper_seal_signal" },
        { "C3a2", "key_management_signal" },
        { "C3a3", "recording_media_missing_signal" },
        { "C3a4", "access_log_irregularity_signal" },

        { "C3b1", "blind_spot_path_signal" },
        { "C3b2", "unlock_route_signal" },

        { "C3c1", "system_malfunction_signal" },
        { "C3c2", "distributed_failure_signal" },
        { "C3c3", "natural_wear_signal" },

        // D4 / D5 / D6
        { "C4a1", "timestamp_conflict_signal" },
        { "C5a1", "sop_documentation_signal" },
        { "C6a1", "active_cleaning_signal" },
        { "C6a2", "active_packing_signal" },

        // D7: On-premises presence
        { "C7a1", "recent_activity_signal" },
        { "C7a2", "mail_accumulation_signal" },
        { "C7b1", "internally_locked_exit_signal" },
        { "C7b4", "trail_ends_inside_signal" },
        { "C7c1", "clear_exit_trail_signal" },
        { "C7c2", "vehicle_activity_signal" },
    };

    public static InnerVoiceContext Build(
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

        int steps = scoreHistory.Count;
        bool early = steps <= EARLY_STEPS_LOCK;
        string stage = ComputeStage(steps);

        float temperature = stage == "early"
            ? EARLY_SOFTMAX_TEMPERATURE
            : SOFTMAX_TEMPERATURE;

        var probs = Softmax(currentScores, temperature);

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
            var prevProbs = Softmax(prevScores, temperature);

            if (prevProbs.TryGetValue(top.Key, out float prevTopP) &&
                prevProbs.TryGetValue(second.Key, out float prevSecondP))
            {
                prevGap = prevTopP - prevSecondP;
            }
        }

        float leadGapDelta = leadGap - prevGap;

        // early 階段不允許 gap 變化太猛
        if (early)
        {
            leadGapDelta = Math.Min(leadGapDelta, EARLY_MAX_GAP_DELTA);
        }

        float deltaTopRaw = SafeDelta(deltasThisStep, top.Key);
        float deltaSecondRaw = SafeDelta(deltasThisStep, second.Key);

        float deltaTop = NarrativeDelta(deltaTopRaw);
        float deltaSecond = NarrativeDelta(deltaSecondRaw);

        var (forTop, againstTop) = ExtractForAgainstSignals(
            claimResults,
            top.Key,
            maxSignalsPerSide: MAX_SIGNALS
        );

        string confidence = ComputeConfidenceLevel(leadGap);

        // early 階段不允許 high
        if (early && confidence == "high")
            confidence = "moderate";

        string dominantShift = ComputeDominantShift(deltaTopRaw, EPS);

        // early 階段即使 top 有上升，也不要太早表達成明顯「正在往某方向靠」
        if (early && dominantShift == "up" && leadGap < GAP_MODERATE)
        {
            dominantShift = "flat";
        }

        string competitionState = ComputeCompetitionState(leadGap, leadGapDelta, GAP_LOW, EPS);

        // early 階段壓制太強的趨勢表述
        if (early && competitionState == "pulling_away")
        {
            competitionState = "stable_gap";
        }

        var context = new InnerVoiceContext
        {
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

            lastEvidenceDisplayName = lastEvidence?.displayNameEn ?? "unknown_evidence",
            lastEvidenceZone = lastEvidence != null ? ZoneManager.Instance.GetZoneDisplayNameForLLM(lastEvidence.zoneAt) : "unknown_zone",

            signalsForDominant = forTop,
            signalsAgainstDominant = againstTop,

            oscillation = DetectOscillation(scoreHistory, top.Key, OSC_WINDOW, EPS),

            tensionTags = BuildTensionTags(forTop, againstTop)
        };

        context.stance = ComputeStance(context, steps);
        context.doubtType = ComputeDoubtType(context);

        return context;
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

        var exps = new Dictionary<string, float>(valid.Count);
        double sum = 0.0;

        foreach (var kv in valid)
        {
            double z = (kv.Value - max) / t;
            double e = Math.Exp(z);
            exps[kv.Key] = (float)e;
            sum += e;
        }

        float denom = (float)Math.Max(0.0001, sum);

        var probs = new Dictionary<string, float>(valid.Count);
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

    private static float Clamp(float v, float min, float max)
    {
        if (v < min) return min;
        if (v > max) return max;
        return v;
    }

    private static (List<string> forTop, List<string> againstTop) ExtractForAgainstSignals(
        List<ClaimSelectionResult> claimResults,
        string dominantHypothesisId,
        int maxSignalsPerSide)
    {
        if (claimResults == null || claimResults.Count == 0)
        {
            return (new List<string> { "no_claim_signal" }, new List<string>());
        }

        var scored = new List<(string tag, float strength, float signedImpact)>();

        foreach (var r in claimResults)
        {
            if (!ClaimManager.Instance.claimDictionary.TryGetValue(r.claimId, out var claim))
                continue;

            var eff = claim.effects.FirstOrDefault(e => e.hypothesisId == dominantHypothesisId);
            if (eff == null) continue;

            bool claimSupports = eff.polarity == Polarity.Support;
            bool evidenceSupports = IsSupportPolarity(r.polarity);

            float sign = (claimSupports == evidenceSupports) ? +1f : -1f;
            float impact = sign * (eff.weight * r.strength);

            string tag = ClaimToSignalTag.TryGetValue(r.claimId, out var t) ? t : "unknown_signal";
            scored.Add((tag, r.strength, impact));
        }

        var forTop = scored
            .Where(x => x.signedImpact > 0f)
            .OrderByDescending(x => x.signedImpact)
            .Select(x => x.tag)
            .Distinct()
            .Take(Math.Max(1, maxSignalsPerSide))
            .ToList();

        var againstTop = scored
            .Where(x => x.signedImpact < 0f)
            .OrderBy(x => x.signedImpact)
            .Select(x => x.tag)
            .Distinct()
            .Take(maxSignalsPerSide)
            .ToList();

        if (forTop.Count == 0)
            forTop.Add("no_support_signal");

        return (forTop, againstTop);
    }

    private static bool IsSupportPolarity(string polarity)
    {
        if (string.IsNullOrWhiteSpace(polarity)) return false;

        string normalized = polarity.Trim().ToLowerInvariant();
        return normalized == "support" || normalized == "positive";
    }

    private static List<string> BuildTensionTags(List<string> forTop, List<string> againstTop)
    {
        var tags = new List<string>();

        bool intrusion = forTop.Contains("forced_entry_signal") || forTop.Contains("perimeter_breach_signal");
        bool intact = againstTop.Contains("intact_entry_points_signal");
        if (intrusion && intact) tags.Add("intrusion_vs_intact");

        bool disturbance = forTop.Contains("indoor_disturbance_signal");
        bool noTracks = againstTop.Contains("single_track_only_signal");
        if (disturbance && noTracks) tags.Add("disturbance_vs_no_tracks");

        bool movement = forTop.Contains("directional_movement_signal");
        bool noDrag = againstTop.Contains("no_drag_signal");
        if (movement && noDrag) tags.Add("movement_vs_no_drag");

        if (tags.Count == 0 && againstTop.Count > 0)
            tags.Add("support_vs_counter_pull");

        return tags;
    }

    private static string ComputeStance(InnerVoiceContext c, int steps)
    {
        bool hasCounters = c.signalsAgainstDominant != null && c.signalsAgainstDominant.Count > 0;
        bool early = steps <= EARLY_STEPS_LOCK;

        if (early)
        {
            if (c.confidenceLevel == "low") return "torn";
            if (hasCounters) return "cautious";
            return "leaning";
        }

        if (c.confidenceLevel == "low")
            return "torn";

        if (c.competitionState == "being_challenged" ||
            c.competitionState == "neck_and_neck" ||
            hasCounters)
            return "cautious";

        if (c.confidenceLevel == "high" &&
            !hasCounters &&
            c.competitionState == "pulling_away")
            return "nearly_sure";

        if (c.confidenceLevel == "high" && !hasCounters)
            return "strong_pull";

        return "leaning";
    }

    private static string ComputeDoubtType(InnerVoiceContext c)
    {
        if (c.tensionTags != null && c.tensionTags.Count > 0) return "inconsistency";
        if (c.oscillation) return "missing_link";
        if (c.confidenceLevel == "low") return "lack_of_confirmation";
        return "minor_residual_doubt";
    }

    private static bool DetectOscillation(List<Dictionary<string, float>> history, string key, int window, float eps)
    {
        if (history == null || history.Count < 3) return false;

        int take = Math.Max(3, Math.Min(window, history.Count));
        var recent = history
            .Skip(history.Count - take)
            .Select(h => SafeGet(h, key))
            .ToArray();

        int? lastDir = null;

        for (int i = 1; i < recent.Length; i++)
        {
            float diff = recent[i] - recent[i - 1];
            int dir = diff > eps ? 1 : diff < -eps ? -1 : 0;

            if (dir == 0) continue;

            if (lastDir.HasValue && dir != lastDir.Value)
                return true;

            lastDir = dir;
        }

        return false;
    }

    private static float SafeGet(Dictionary<string, float> snapshot, string key)
    {
        if (snapshot == null) return 0f;
        if (!snapshot.TryGetValue(key, out float v)) return 0f;
        if (float.IsNaN(v) || float.IsInfinity(v)) return 0f;
        return v;
    }
}

[Serializable]
public class InnerVoiceContext
{
    public string dominantHypothesisId;
    public string challengerHypothesisId;

    public string dominantView;
    public string challengerView;

    public string confidenceLevel; // low / moderate / high
    public float leadGap;          // pTop - pSecond
    public float leadGapDelta;     // gap change

    public float deltaDominant;    // narrative-scaled
    public float deltaChallenger;  // narrative-scaled

    public string dominantShift;    // up / down / flat
    public string competitionState; // pulling_away / being_challenged / neck_and_neck / stable_gap
    public string stage;            // early / mid / late
    public bool oscillation;

    public List<string> signalsForDominant;
    public List<string> signalsAgainstDominant;

    public List<string> tensionTags;

    public string stance;     // torn / cautious / leaning / nearly_sure / strong_pull
    public string doubtType;  // inconsistency / missing_link / lack_of_confirmation / minor_residual_doubt

    public string lastEvidenceDisplayName;
    public string lastEvidenceZone;
}