using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvidenceManager : Singleton<EvidenceManager>
{
    public List<Evidence> evidences = new();
    HashSet<string> collectedEvidences = new();
    public bool IsCreatedEvidence { get; private set; } = false;
    public void SelectEvidenceFacts(SemanticActionObject semanticActionObject, string imagePath)
    {
        if (semanticActionObject == null) throw new ArgumentNullException(nameof(semanticActionObject));

        collectedEvidences.Add(semanticActionObject.displayNameEn);
        FactSelectionManager.Instance.SetSemanticActionObject(semanticActionObject, imagePath);
    }

    public void CancelSelectEvidenceFacts(SemanticActionObject semanticActionObject)
    {
        collectedEvidences.Remove(semanticActionObject.displayNameEn);
    }

    public async void AddEvidence(SemanticActionObject semanticActionObject, string imagePath, List<Fact> selectedFacts = null)
    {
        IsCreatedEvidence = true;

        Evidence evidence = await EvidenceFactory.CreateEvidenceAsync(semanticActionObject, imagePath, selectedFacts);
        evidences.Add(evidence);
        UIManager.Instance.playerReasoningUI.evidenceListUI.AddEvidenceToUI(evidence);
        IsCreatedEvidence = false;
    }

    public bool ExisitsEvidence(string evidenceName)
    {
        return collectedEvidences.Contains(evidenceName);
    }
}
