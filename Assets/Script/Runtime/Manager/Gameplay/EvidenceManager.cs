using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvidenceManager : Singleton<EvidenceManager>
{
    public List<Evidence> evidences = new();
    HashSet<string> collectedEvidences = new();
    public bool IsCreatedEvidence { get; private set; } = false;
    public async void AddEvidence(SemanticActionObject semanticActionObject, Zone zoneAt, string imagePath)
    {
        IsCreatedEvidence = true;

        collectedEvidences.Add(semanticActionObject.displayNameEn);
        Evidence evidence = await EvidenceFactory.CreateEvidenceAsync(semanticActionObject, zoneAt, imagePath);
        evidences.Add(evidence);
        UIManager.Instance.playerReasoningUI.evidenceListUI.AddEvidenceToUI(evidence);
        IsCreatedEvidence = false;
    }

    public bool ExisitsEvidence(string evidenceName)
    {
        return collectedEvidences.Contains(evidenceName);
    }
}
