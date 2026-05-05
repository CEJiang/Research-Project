using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Threading.Tasks;

public static class EvidenceFactory
{
    public static async Task<Evidence> CreateEvidenceAsync(SemanticActionObject semanticActionObject, string imagePath, List<Fact> selectedFacts = null)
    {
        if (semanticActionObject == null) throw new ArgumentNullException(nameof(semanticActionObject));

        string evidenceId = Guid.NewGuid().ToString("N");
        string semanticTypeId = semanticActionObject.semanticTypeId;

        Evidence evidence = new(
            evidenceId: evidenceId,
            semanticTypeId: semanticTypeId,
            displayNameZh: semanticActionObject.displayNameZh,
            displayNameEn: semanticActionObject.displayNameEn,
            facts: selectedFacts,
            zoneAt: semanticActionObject.zone,
            imagePath: imagePath
        );

        // Get Evidence Metadata
        EvidenceModel evidenceModel = new()
        {
            displayName = evidence.displayNameEn,
            zoneAt = ZoneManager.Instance.GetZoneDisplayNameForLLM(evidence.zoneAt),
            facts = evidence.GetEvidenceFactsAsStringForLLM()
        };

        // previous hypothesis state (final scores before this evidence is applied)
        var previousHypothesisState = HypothesisStateManager.Instance.GetCurrentHypothesisState();

        // Feature status based on evidence
        List<FeatureSelectionResult> featureResults =
            await FeatureManager.Instance.EvaluateEvidenceFeaturesAsync(evidenceModel);

        List<ClaimSelectionResult> claimResults =
            ClaimManager.Instance.EvaluateEvidenceClaimsAsync(featureResults);

        foreach (var claimResult in claimResults)
        {
            Logger.Log(
                $"Claim ID: {claimResult.claimId}, " +
                $"Polarity: {claimResult.polarity}, " +
                $"Strength: {claimResult.strength}, " +
                $"Reason: {claimResult.reason}"
            );
        }

        // Update evidence-driven propensity score
        Dictionary<string, float> propensityScores =
            HypothesisStateManager.Instance.ComputePropensityScore(claimResults, evidence);

        HypothesisStateManager.Instance.UpdatePropensityScore(propensityScores);

        // Commit this evidence step as one meaningful Inner Voice step
        HypothesisStateManager.Instance.CommitInnerVoiceStep();

        // current hypothesis state after this evidence is applied
        var updatedHypothesisState = HypothesisStateManager.Instance.GetCurrentHypothesisState();

        EvidenceMetadata metadata = new()
        {
            evidence = evidenceModel,
            previousHypothesisState = previousHypothesisState,
            updatedHypothesisState = updatedHypothesisState,
            claimResults = claimResults
        };

        // player feedback first
        string narration = await SynthesisNarratorManager.Instance.GenerateNarration();
        DialogueManager.Instance.ShowInnerVoiceMessage(narration);

        return evidence;
    }
}