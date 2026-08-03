using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvidenceManager : Singleton<EvidenceManager>
{
    public List<Evidence> evidences = new();
    HashSet<string> collectedEvidences = new();
    public bool IsCreatedEvidence { get; private set; } = false;
    public void SelectEvidenceObservations(SemanticActionObject semanticActionObject, string imagePath)
    {
        if (semanticActionObject == null) throw new ArgumentNullException(nameof(semanticActionObject));

        collectedEvidences.Add(semanticActionObject.displayNameEn);
        ObservationSelectionManager.Instance.SetSemanticActionObject(semanticActionObject, imagePath);
    }

    public void CancelSelectEvidenceObservations()
    {
        SemanticActionObject semanticActionObject = ObservationSelectionManager.Instance.currentSelectedActionObject;
        collectedEvidences.Remove(semanticActionObject.displayNameEn);
        ObservationSelectionManager.Instance.CloseObservationSelectionUI();
    }

    public async void AddEvidence(SemanticActionObject semanticActionObject, string imagePath, float observationReliabilityScore, List<ObservationCandidate> selectedObservations = null)
    {
        IsCreatedEvidence = true;

        Evidence evidence = await EvidenceFactory.CreateEvidenceAsync(semanticActionObject, imagePath, observationReliabilityScore, selectedObservations);
        evidences.Add(evidence);
        UIManager.Instance.playerReasoningUI.evidenceListUI.AddEvidenceToUI(evidence);
        IsCreatedEvidence = false;
    }

    public bool ExisitsEvidence(string evidenceName)
    {
        return collectedEvidences.Contains(evidenceName);
    }
}
