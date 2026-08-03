using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ReasoningSequenceUI : MonoBehaviour,
    IDropHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private GameObject sequenceNodePrefab;
    [SerializeField] private RectTransform sequenceLayer;     // Content
    [SerializeField] private RectTransform overlayLayer;      // OverlayLayer
    [SerializeField] private RectTransform insertionMarker;   // White line marker

    private readonly HashSet<string> existingEvidenceIds = new HashSet<string>();

    private bool isPointerInside = false;
    private int currentInsertIndex = -1;

    public bool IsPointerInside => isPointerInside;
    public int CurrentInsertIndex => currentInsertIndex;

    private void Awake()
    {
        HideInsertionMarker();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
        HideInsertionMarker();
    }

    public void OnDrop(PointerEventData eventData)
    {
        EvidenceListItem item = eventData.pointerDrag?.GetComponent<EvidenceListItem>();
        if (item == null || item.evidence == null) return;

        Evidence evidence = item.evidence;

        if (existingEvidenceIds.Contains(evidence.evidenceID))
        {
            HideInsertionMarker();
            return;
        }

        int insertIndex = GetInsertIndexFromPointer(eventData.position.x);
        ReasoningSequenceManager.Instance.AddEvidenceToReasoningSequence(evidence, insertIndex);

        HideInsertionMarker();
    }

    public void AddEvidence(Evidence evidence, int insertIndex)
    {
        GameObject newNode = Instantiate(sequenceNodePrefab, sequenceLayer, false);

        ReasoningSequenceNode node = newNode.GetComponent<ReasoningSequenceNode>();
        if (node != null)
        {
            node.Setup(evidence, sequenceLayer);
        }

        insertIndex = Mathf.Clamp(insertIndex, 0, sequenceLayer.childCount - 1);
        newNode.transform.SetSiblingIndex(insertIndex);

        existingEvidenceIds.Add(evidence.evidenceID);
    }

    public void RemoveEvidence(Evidence evidence)
    {
        if (evidence == null) return;
        existingEvidenceIds.Remove(evidence.evidenceID);
    }

    public void UpdateInsertionMarker(Vector2 pointerScreenPosition, Camera eventCamera)
    {
        if (!isPointerInside || insertionMarker == null || overlayLayer == null || sequenceLayer == null)
        {
            HideInsertionMarker();
            return;
        }

        List<RectTransform> items = GetSequenceItems();
        currentInsertIndex = GetInsertIndexFromPointer(pointerScreenPosition.x);

        Vector3 markerWorldPos = CalculateMarkerWorldPosition(items, currentInsertIndex);
        PlaceMarkerAtWorldPosition(markerWorldPos, eventCamera);

        insertionMarker.gameObject.SetActive(true);
    }

    public void HideInsertionMarker()
    {
        currentInsertIndex = -1;

        if (insertionMarker != null)
        {
            insertionMarker.gameObject.SetActive(false);
        }
    }

    private List<RectTransform> GetSequenceItems()
    {
        List<RectTransform> items = new List<RectTransform>();

        for (int i = 0; i < sequenceLayer.childCount; i++)
        {
            RectTransform child = sequenceLayer.GetChild(i) as RectTransform;
            if (child == null) continue;
            items.Add(child);
        }

        return items;
    }

    private int GetInsertIndexFromPointer(float pointerScreenX)
    {
        List<RectTransform> items = GetSequenceItems();

        for (int i = 0; i < items.Count; i++)
        {
            Vector3[] corners = new Vector3[4];
            items[i].GetWorldCorners(corners);

            float left = corners[0].x;
            float right = corners[3].x;
            float center = (left + right) * 0.5f;

            if (pointerScreenX < center)
            {
                return i;
            }
        }

        return items.Count;
    }

    private Vector3 CalculateMarkerWorldPosition(List<RectTransform> items, int insertIndex)
    {
        if (items.Count == 0)
        {
            Vector3[] sequenceCorners = new Vector3[4];
            sequenceLayer.GetWorldCorners(sequenceCorners);

            float xEmpty = sequenceCorners[0].x + 8f;
            float yEmpty = (sequenceCorners[0].y + sequenceCorners[1].y) * 0.5f;
            return new Vector3(xEmpty, yEmpty, 0f);
        }

        if (insertIndex <= 0)
        {
            Vector3[] corners = new Vector3[4];
            items[0].GetWorldCorners(corners);

            float xStart = corners[0].x;
            float yStart = (corners[0].y + corners[1].y) * 0.5f;
            return new Vector3(xStart, yStart, 0f);
        }

        if (insertIndex >= items.Count)
        {
            Vector3[] corners = new Vector3[4];
            items[items.Count - 1].GetWorldCorners(corners);

            float xEnd = corners[3].x;
            float yEnd = (corners[0].y + corners[1].y) * 0.5f;
            return new Vector3(xEnd, yEnd, 0f);
        }

        Vector3[] prevCorners = new Vector3[4];
        Vector3[] nextCorners = new Vector3[4];

        items[insertIndex - 1].GetWorldCorners(prevCorners);
        items[insertIndex].GetWorldCorners(nextCorners);

        float xMiddle = (prevCorners[3].x + nextCorners[0].x) * 0.5f;
        float yMiddle = (prevCorners[0].y + prevCorners[1].y) * 0.5f;

        return new Vector3(xMiddle, yMiddle, 0f);       
    }

    private void PlaceMarkerAtWorldPosition(Vector3 worldPos, Camera eventCamera)
    {
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, worldPos);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayLayer,
            screenPoint,
            eventCamera,
            out Vector2 localPoint))
        {
            insertionMarker.anchoredPosition = localPoint;
        }
    }
}