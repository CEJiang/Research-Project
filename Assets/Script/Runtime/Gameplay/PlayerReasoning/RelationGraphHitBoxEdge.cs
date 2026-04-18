using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class RelationGraphHitBoxEdge : MonoBehaviour, IPointerClickHandler
{
    public RelationGraphEdge parentEdge;

    void Awake()
    {
        if (parentEdge == null)
        {
            parentEdge = GetComponentInParent<RelationGraphEdge>();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (parentEdge != null)
        {
            RelationGraphManager.Instance.OnRelationEdgeClicked(parentEdge);
        }
    }
}
