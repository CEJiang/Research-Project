using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvidenceListUI : MonoBehaviour
{
    public Transform evidenceListContent;
    public GameObject evidenceListItemPrefab;
    public void AddEvidenceToUI(Evidence evidence)
    {
        GameObject evidenceItem = Instantiate(evidenceListItemPrefab, evidenceListContent);

        evidenceItem.name = evidence.DisplayName;
        EvidenceListItem evidenceListItem = evidenceItem.GetComponent<EvidenceListItem>();
        evidenceListItem?.SetEvidence(evidence);
    }
}
