using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReasoningSequenceManager : Singleton<ReasoningSequenceManager>
{
    private HashSet<Evidence> evidenceInReasoningSequence = new();

    public void AddEvidenceToReasoningSequence(Evidence evidence, int insertIndex = -1)
    {
        if (!evidenceInReasoningSequence.Contains(evidence))
        {
            evidenceInReasoningSequence.Add(evidence);
            // Maybe we can trigger some events here to update the UI or other systems that are related to the Reasoning Sequence.
            
            // For example, we can update the Reasoning Sequence UI to show the newly added evidence item in the sequence.
            UIManager.Instance.playerReasoningUI.reasoningSequenceUI.AddEvidence(evidence, insertIndex);
        }
    }

    public void RemoveEvidenceFromReasoningSequence(Evidence evidence)
    {
        if (evidenceInReasoningSequence.Contains(evidence))
        {
            evidenceInReasoningSequence.Remove(evidence);
            // Maybe we can trigger some events here to update the UI or other systems that are related to the Reasoning Sequence.
            
            // For example, we can update the Reasoning Sequence UI to remove the evidence item from the sequence.
            UIManager.Instance.playerReasoningUI.reasoningSequenceUI.RemoveEvidence(evidence);
        }
    }
}
