using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ReasoningGraphHitBoxEdge : MonoBehaviour, IPointerClickHandler
{
    public ReasoningGraphEdge parentEdge;

    void Awake()
    {
        if (parentEdge == null)
        {
            parentEdge = GetComponentInParent<ReasoningGraphEdge>();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (parentEdge != null)
        {
            ReasoningGraphManager.Instance.OnReasoningGraphEdgeClicked(parentEdge);
        }
    }
}
