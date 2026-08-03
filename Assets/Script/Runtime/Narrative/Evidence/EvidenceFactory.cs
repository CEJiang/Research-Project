using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public static class EvidenceFactory
{
    public static async Task<Evidence> CreateEvidenceAsync(
        SemanticActionObject semanticActionObject,
        string imagePath,
        float observationReliabilityScore,
        List<ObservationCandidate> selectedObservations = null
    )
    {
        if (semanticActionObject == null)
            throw new ArgumentNullException(nameof(semanticActionObject));

        string evidenceID = Guid.NewGuid().ToString("N");
        string semanticTypeID = semanticActionObject.semanticTypeID;

        Logger.Log($"[Evidence Factory]: Creating Evidence: {evidenceID}, Spatial Context: {semanticActionObject.spatialContext}");

        Evidence evidence = new(
            evidenceID: evidenceID,
            semanticTypeID: semanticTypeID,
            displayNameZh: semanticActionObject.displayNameZh,
            displayNameEn: semanticActionObject.displayNameEn,
            spatialContext: semanticActionObject.spatialContext,
            observationReliabilityScore: observationReliabilityScore,
            facts: ConvertObservationCandidatesToFacts(selectedObservations),
            zoneAt: semanticActionObject.zone,
            imagePath: imagePath
        );

        evidence.features = FeatureManager.Instance.ExtractFeaturesFromEvidence(evidence);

        var previousHypothesisState =
            HypothesisStateManager.Instance.GetCurrentHypothesisState();

        foreach (var feature in evidence.features)
        {
            Logger.Log(
                $"Evidence Feature: {feature.featureID}, " +
                $"Description: {feature.description}"
            );
        }

        List<ClaimSelectionResult> claimResults =
            await ClaimManager.Instance.EvaluateEvidenceClaimsAsync(evidence);

        claimResults ??= new List<ClaimSelectionResult>();

        foreach (var claimResult in claimResults)
        {
            string basedFeatureIDs = claimResult.basedFeatureIDs == null
                ? "None"
                : string.Join(", ", claimResult.basedFeatureIDs);

            Logger.Log(
                $"Claim ID: {claimResult.claimID}, " +
                $"Confidence: {claimResult.confidence}, " +
                $"Information Value: {claimResult.informationValue}, " +
                $"Based Feature IDs: {basedFeatureIDs}, " +
                $"Reason: {claimResult.reason}, " +
                $"Spatial Context Used: {claimResult.spatialContextUsed}"
            );
        }

        Dictionary<string, float> propensityScores =
            HypothesisStateManager.Instance.ComputePropensityScore(claimResults, evidence);

        HypothesisStateManager.Instance.UpdatePropensityScore(propensityScores);

        HypothesisStateManager.Instance.CommitEvidenceDrivenStep();

        var updatedHypothesisState =
            HypothesisStateManager.Instance.GetCurrentHypothesisState();

        EvidenceMetadata metadata = new()
        {
            evidence = evidence,
            previousHypothesisState = previousHypothesisState,
            updatedHypothesisState = updatedHypothesisState,
            claimResults = claimResults
        };

        string narration =
            await SynthesisNarratorManager.Instance.GenerateNarration();

        DialogueManager.Instance.ShowReflectiveVoiceMessage(narration);

        return evidence;
    }

    public static List<Fact> ConvertObservationCandidatesToFacts(
        List<ObservationCandidate> observationCandidates
    )
    {
        List<Fact> facts = new();

        if (observationCandidates == null)
            return facts;

        foreach (var candidate in observationCandidates)
        {
            if (candidate == null)
                continue;

            facts.Add(new Fact(
                candidate.candidateID,
                candidate.GetDescription(),
                candidate.GetDescriptionForLLM(),
                candidate.featureID
            ));
        }

        return facts;
    }
}