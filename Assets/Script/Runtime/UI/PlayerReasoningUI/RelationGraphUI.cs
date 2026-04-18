using System;
using System.Collections;
using System.Collections.Generic;
using Radishmouse;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class RelationGraphUI : MonoBehaviour, IDropHandler
{
    public GameObject relationNodePrefab;
    public GameObject relationEdgePrefab;
    public RectTransform nodeLayer;
    public RectTransform edgeLayer;
    public DropdownAnimator relationGraphDropdown;

    public RelationNode AddRelationNode(Evidence evidence, Vector2 localPosition = default(Vector2))
    {
       GameObject newNode = Instantiate(relationNodePrefab, nodeLayer);
       newNode.GetComponent<RelationNode>().Setup(evidence, nodeLayer);
       newNode.GetComponent<RectTransform>().anchoredPosition = localPosition;
       return newNode.GetComponent<RelationNode>();
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
        
        RelationGraphManager.Instance.AddRelationNode(item.evidence, localPoint, item.gameObject);
    }

    public RelationGraphEdge AddRelationEdge(RelationNode fromNode, RelationNode toNode, RelationGraphType type)
    {
        GameObject newEdge = Instantiate(relationEdgePrefab, edgeLayer);
        newEdge.GetComponent<RelationGraphEdge>().Setup(fromNode, toNode, type);
        return newEdge.GetComponent<RelationGraphEdge>();
    }

    public void RemoveRelationEdge(RelationGraphEdge currentSelectedEdge)
    {
        Destroy(currentSelectedEdge.gameObject);
    }

    public void RemoveRelationNode(RelationNode currentSelectedNode)
    {
        Destroy(currentSelectedNode.gameObject);
    }
}
