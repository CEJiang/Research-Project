using Radishmouse;
using UnityEngine;

public class ReasoningGraphPreviewEdge : MonoBehaviour
{
    [SerializeField] private UILineRenderer lineRenderer;

    private ReasoningGraphNode fromNode;
    private RectTransform edgeLayerRect;
    private Canvas parentCanvas;
    private bool isActive = false;
    public GameObject ArrowHeadPrefab;
    private RectTransform arrowHeadRect;
    private float ArrowHeadRotationOffset = -90f;

    private void Awake()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<UILineRenderer>();
        }

        edgeLayerRect = transform.parent as RectTransform;
        parentCanvas = GetComponentInParent<Canvas>();
        arrowHeadRect = Instantiate(ArrowHeadPrefab, transform).GetComponent<RectTransform>();
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isActive || fromNode == null || lineRenderer == null || edgeLayerRect == null)
            return;

        Vector2 fromPos = GetNodePositionInEdgeLayer(fromNode);
        Vector2 mousePos = GetMousePositionInEdgeLayer();

        lineRenderer.points = new Vector2[]
        {
            fromPos,
            mousePos
        };

        lineRenderer.SetVerticesDirty();
        UpdateArrowHead(fromPos, mousePos);
    }

    public void BeginPreview(ReasoningGraphNode from, ReasoningGraphType type)
    {
        fromNode = from;
        isActive = true;
        gameObject.SetActive(true);

        switch (type)
        {
            case ReasoningGraphType.LEADTO:
                lineRenderer.color = ReasoningGraphManager.Instance.leadToColor;
                arrowHeadRect.gameObject.SetActive(true);
                break;
            case ReasoningGraphType.CONFLICT:
                lineRenderer.color = ReasoningGraphManager.Instance.conflictColor;
                arrowHeadRect.gameObject.SetActive(false);
                break;
            case ReasoningGraphType.COHERENT:
                lineRenderer.color = ReasoningGraphManager.Instance.coherentColor;
                arrowHeadRect.gameObject.SetActive(false);
                break;
        }
    }

    public void EndPreview()
    {
        isActive = false;
        fromNode = null;
        gameObject.SetActive(false);
    }

    private Vector2 GetNodePositionInEdgeLayer(ReasoningGraphNode node)
    {
        RectTransform nodeRect = node.GetComponent<RectTransform>();
        Camera cam = null;
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = parentCanvas.worldCamera;
        }

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, nodeRect.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            edgeLayerRect,
            screenPoint,
            cam,
            out Vector2 localPoint
        );

        return localPoint;
    }

    private Vector2 GetMousePositionInEdgeLayer()
    {
        Camera cam = null;
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = parentCanvas.worldCamera;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            edgeLayerRect,
            Input.mousePosition,
            cam,
            out Vector2 localPoint
        );

        return localPoint;
    }

    private void UpdateArrowHead(Vector2 start, Vector2 end)
    {
        if (arrowHeadRect == null) return;

        if (ReasoningGraphManager.Instance.currentReasoningGraphType != ReasoningGraphType.LEADTO)
        {
            arrowHeadRect.gameObject.SetActive(false);
            return;
        }

        arrowHeadRect.gameObject.SetActive(true);

        Vector2 dir = end - start;
        if (dir.sqrMagnitude < 0.001f) return;

        dir.Normalize();

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + ArrowHeadRotationOffset;


        Vector2 arrowTipPos = end - dir;

        arrowHeadRect.anchoredPosition = arrowTipPos;
        arrowHeadRect.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}