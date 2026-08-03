using UnityEngine;
using UnityEngine.EventSystems;

public class EvidenceHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum ObjectType
    {
        EvidenceListItem,
        ReasoningGraphNode,
        SequenceNode
    }
    public ObjectType objectType;
    public Evidence evidence;

    void Start()
    {
        switch (objectType)
        {
            case ObjectType.EvidenceListItem:
                // Initialization code for evidence list item
                evidence = GetComponent<EvidenceListItem>()?.evidence;
                break;
            case ObjectType.ReasoningGraphNode:
                // Initialization code for reasoning graph node
                evidence = GetComponent<ReasoningGraphNode>()?.evidence;
                break;
            case ObjectType.SequenceNode:
                // Initialization code for sequence node
                // evidence = GetComponent<SequenceNode>()?.evidence;
                break;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (EvidenceTooltipManager.Instance != null && ReasoningGraphManager.Instance.currentReasoningGraphType == ReasoningGraphType.NONE)
        {
            
            EvidenceTooltipManager.Instance.ShowTooltip(evidence.DisplayName, evidence.GetFactsAsStringForUI());
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (EvidenceTooltipManager.Instance != null)
        {
            EvidenceTooltipManager.Instance.HideTooltip();
        }
    }
}