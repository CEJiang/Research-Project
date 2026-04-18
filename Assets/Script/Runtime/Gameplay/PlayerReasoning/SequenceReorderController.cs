using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SequenceReorderController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform contentRect;
    [SerializeField] private RectTransform overlayRect;
    [SerializeField] private RectTransform insertionMarker;

    private int currentInsertIndex = -1;
    public int CurrentInsertIndex => currentInsertIndex;

    private void Awake()
    {
        HideMarker();
    }

    public void HideMarker()
    {
        currentInsertIndex = -1;

        if (insertionMarker != null)
        {
            insertionMarker.gameObject.SetActive(false);
        }
    }

    public void ShowInsertionMarker(RectTransform draggingItem, PointerEventData eventData)
    {
        if (contentRect == null || overlayRect == null || insertionMarker == null) return;

        List<RectTransform> items = GetVisibleItemsExcluding(draggingItem);
        int insertIndex = CalculateInsertIndex(items, eventData.position.x);

        currentInsertIndex = insertIndex;

        Vector3 worldMarkerPos = CalculateMarkerWorldPosition(items, insertIndex);
        PlaceMarkerAtWorldPosition(worldMarkerPos, eventData.pressEventCamera);
    }

    public void ApplyReorder(RectTransform draggingItem)
    {
        if (draggingItem == null) return;

        if (currentInsertIndex < 0)
        {
            HideMarker();
            return;
        }

        draggingItem.SetSiblingIndex(currentInsertIndex);
        HideMarker();
    }

    private List<RectTransform> GetVisibleItemsExcluding(RectTransform draggingItem)
    {
        List<RectTransform> result = new List<RectTransform>();

        for (int i = 0; i < contentRect.childCount; i++)
        {
            RectTransform child = contentRect.GetChild(i) as RectTransform;
            if (child == null) continue;
            if (child == draggingItem) continue;

            result.Add(child);
        }

        return result;
    }

    private int CalculateInsertIndex(List<RectTransform> items, float pointerScreenX)
    {
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
        // 沒有任何 item
        if (items.Count == 0)
        {
            return contentRect.TransformPoint(Vector3.zero);
        }

        // 插到最前面：取第一個 item 的左邊界中點
        if (insertIndex <= 0)
        {
            Vector3[] corners = new Vector3[4];
            items[0].GetWorldCorners(corners);

            float x = corners[0].x; // left
            float y = (corners[0].y + corners[1].y) * 0.5f;
            return new Vector3(x, y, 0f);
        }

        // 插到最後面：取最後一個 item 的右邊界中點
        if (insertIndex >= items.Count)
        {
            Vector3[] corners = new Vector3[4];
            items[items.Count - 1].GetWorldCorners(corners);

            float x = corners[3].x; // right
            float y = (corners[0].y + corners[1].y) * 0.5f;
            return new Vector3(x, y, 0f);
        }

        // 插在兩個 item 中間：取前一個右邊界與下一個左邊界的中間
        Vector3[] prevCorners = new Vector3[4];
        Vector3[] nextCorners = new Vector3[4];

        items[insertIndex - 1].GetWorldCorners(prevCorners);
        items[insertIndex].GetWorldCorners(nextCorners);

        float prevRight = prevCorners[3].x;
        float nextLeft = nextCorners[0].x;
        float xMid = (prevRight + nextLeft) * 0.5f;

        float yMid = (prevCorners[0].y + prevCorners[1].y) * 0.5f;

        return new Vector3(xMid, yMid, 0f);
    }

    private void PlaceMarkerAtWorldPosition(Vector3 worldPos, Camera eventCamera)
    {
        if (insertionMarker == null || overlayRect == null) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayRect,
            RectTransformUtility.WorldToScreenPoint(eventCamera, worldPos),
            eventCamera,
            out localPoint
        );

        insertionMarker.gameObject.SetActive(true);
        insertionMarker.anchoredPosition = localPoint;
    }
}