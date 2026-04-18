using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerReasoningUI : MonoBehaviour
{
    public EvidenceListUI evidenceListUI;
    public ReasoningSequenceUI reasoningSequenceUI;
    public RelationGraphUI relationGraphUI;

    void Start()
    {
        evidenceListUI = FindObjectOfType<EvidenceListUI>();
        reasoningSequenceUI = FindObjectOfType<ReasoningSequenceUI>();
        relationGraphUI = FindObjectOfType<RelationGraphUI>();
    }
}
