using UnityEngine;
using UnityEngine.EventSystems;

public class GraphPanZoomController : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform graphContent;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomStep = 0.1f;
    [SerializeField] private float minZoom = 0.6f;
    [SerializeField] private float maxZoom = 2.0f;
    [SerializeField] private bool zoomTowardCursor = true;

    [Header("Pan Settings")]
    [SerializeField] private bool useMiddleMouseToPan = true;
    [SerializeField] private float panSpeed = 1f;

    private bool isPanning = false;

    public void OnScroll(PointerEventData eventData)
    {
        if (viewport == null || graphContent == null) return;

        float scroll = eventData.scrollDelta.y;
        if (Mathf.Abs(scroll) < 0.01f) return;

        float currentScale = graphContent.localScale.x;
        float targetScale = currentScale + scroll * zoomStep;
        targetScale = Mathf.Clamp(targetScale, minZoom, maxZoom);

        if (Mathf.Approximately(targetScale, currentScale)) return;

        if (zoomTowardCursor)
        {
            ZoomAtScreenPoint(targetScale, eventData.position, eventData.pressEventCamera);
        }
        else
        {
            graphContent.localScale = new Vector3(targetScale, targetScale, 1f);
        }

        ClampGraphPosition();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanStartPan(eventData)) return;
        isPanning = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isPanning || graphContent == null) return;

        graphContent.anchoredPosition += eventData.delta * panSpeed;
        ClampGraphPosition();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isPanning = false;
    }

    private bool CanStartPan(PointerEventData eventData)
    {
        if (!useMiddleMouseToPan) return true;

        return eventData.button == PointerEventData.InputButton.Middle;
    }

    private void ZoomAtScreenPoint(float targetScale, Vector2 screenPoint, Camera eventCamera)
    {
        RectTransform parentRect = graphContent.parent as RectTransform;
        if (parentRect == null) return;

        Vector2 localPointBefore;
        Vector2 localPointAfter;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            graphContent,
            screenPoint,
            eventCamera,
            out localPointBefore
        );

        graphContent.localScale = new Vector3(targetScale, targetScale, 1f);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            graphContent,
            screenPoint,
            eventCamera,
            out localPointAfter
        );

        Vector2 deltaLocal = localPointAfter - localPointBefore;
        Vector2 scaledDelta = Vector2.Scale(deltaLocal, graphContent.localScale);

        graphContent.anchoredPosition += scaledDelta;
    }

    private void ClampGraphPosition()
    {
        if (viewport == null || graphContent == null) return;

        Vector2 viewportSize = viewport.rect.size;
        Vector2 contentSize = Vector2.Scale(graphContent.rect.size, graphContent.localScale);

        Vector2 pos = graphContent.anchoredPosition;

        float limitX = Mathf.Max(0f, (contentSize.x - viewportSize.x) * 0.5f);
        float limitY = Mathf.Max(0f, (contentSize.y - viewportSize.y) * 0.5f);

        pos.x = Mathf.Clamp(pos.x, -limitX, limitX);
        pos.y = Mathf.Clamp(pos.y, -limitY, limitY);

        graphContent.anchoredPosition = pos;
    }

    public void ResetView()
    {
        if (graphContent == null) return;

        graphContent.localScale = Vector3.one;
        graphContent.anchoredPosition = Vector2.zero;
    }
}