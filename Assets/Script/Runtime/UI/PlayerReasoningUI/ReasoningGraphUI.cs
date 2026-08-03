using System;
using System.Collections;
using System.Collections.Generic;
using Radishmouse;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class ReasoningGraphUI : MonoBehaviour, IDropHandler
{
    public GameObject reasoningNodePrefab;
    public GameObject reasoningEdgePrefab;
    public RectTransform nodeLayer;
    public RectTransform edgeLayer;
    public DropdownAnimator reasoningGraphTypeDropdown;

    public ReasoningGraphNode AddReasoningGraphNode(Evidence evidence, Vector2 localPosition = default(Vector2))
    {
       GameObject newNode = Instantiate(reasoningNodePrefab, nodeLayer);
       newNode.GetComponent<ReasoningGraphNode>().Setup(evidence, nodeLayer);
       newNode.GetComponent<RectTransform>().anchoredPosition = localPosition;
       return newNode.GetComponent<ReasoningGraphNode>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        EvidenceListItem item = eventData.pointerDrag?.GetComponent<EvidenceListItem>();
        if (item == null || item.evidence == null) return;

        Vector2 localPoint;
        bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            nodeLayer,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        if (!success) return;
        
        ReasoningGraphManager.Instance.AddReasoningGraphNode(item.evidence, localPoint, item.gameObject);
    }

    public ReasoningGraphEdge AddReasoningGraphEdge(ReasoningGraphNode fromNode, ReasoningGraphNode toNode, ReasoningGraphType type)
    {
        GameObject newEdge = Instantiate(reasoningEdgePrefab, edgeLayer);
        newEdge.GetComponent<ReasoningGraphEdge>().Setup(fromNode, toNode, type);
        return newEdge.GetComponent<ReasoningGraphEdge>();
    }

    public void RemoveReasoningGraphEdge(ReasoningGraphEdge currentSelectedEdge)
    {
        Destroy(currentSelectedEdge.gameObject);
    }

    public void RemoveReasoningGraphNode(ReasoningGraphNode currentSelectedNode)
    {
        Destroy(currentSelectedNode.gameObject);
    }
}
