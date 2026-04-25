using UnityEngine;
using UnityEngine.EventSystems;

public class EvidenceHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum ObjectType
    {
        EvidneceListItem,
        RelationGraphNode,
        SequenceNode
    }
    public ObjectType objectType;
    public Evidence evidence;

    void Start()
    {
        switch (objectType)
        {
            case ObjectType.EvidneceListItem:
                // Initialization code for evidence list item
                evidence = GetComponent<EvidenceListItem>()?.evidence;
                break;
            case ObjectType.RelationGraphNode:
                // Initialization code for relation graph node
                evidence = GetComponent<RelationNode>()?.evidence;
                break;
            case ObjectType.SequenceNode:
                // Initialization code for sequence node
                // evidence = GetComponent<SequenceNode>()?.evidence;
                break;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (EvidenceTooltipManager.Instance != null && RelationGraphManager.Instance.currentRelationGraphType == RelationGraphType.NONE)
        {
            
            EvidenceTooltipManager.Instance.ShowTooltip(evidence.DisplayName, evidence.GetEvidenceFactsAsStringForUI());
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