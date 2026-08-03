using Radishmouse;
using UnityEngine;

public class ReasoningGraphEdge : MonoBehaviour
{
    [SerializeField] private UILineRenderer lineRenderer;
    
    public string edgeID;
    public ReasoningGraphNode fromNode;
    public ReasoningGraphNode toNode;
    public ReasoningGraphType reasoningGraphType;

    public GameObject hitBox;

    private RectTransform edgeLayerRect;
    private Canvas parentCanvas;

    public GameObject arrowHeadPrefab;
    public RectTransform arrowHeadRect;

    [Header("Arrow")]
    public float ArrowHeadBackDistance = 2f;          // 只留一點點微調
    public float ArrowHeadRotationOffset = -90f;

    private void Awake()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<UILineRenderer>();
        }

        edgeLayerRect = transform.parent as RectTransform;
        parentCanvas = GetComponentInParent<Canvas>();

        if (transform.Find("HitBox") != null)
        {
            hitBox = transform.Find("HitBox").gameObject;
        }
    }

    private void LateUpdate()
    {
        if (fromNode == null || toNode == null || lineRenderer == null || edgeLayerRect == null)
            return;

        RectTransform fromRect = fromNode.GetComponent<RectTransform>();
        RectTransform toRect = toNode.GetComponent<RectTransform>();

        if (fromRect == null || toRect == null) return;

        // 1. 先拿中心點（在 EdgeLayer local space）
        Vector2 fromCenter = GetNodeCenterInEdgeLayer(fromRect);
        Vector2 toCenter = GetNodeCenterInEdgeLayer(toRect);

        Vector2 dir = toCenter - fromCenter;
        if (dir.sqrMagnitude < 0.001f) return;

        dir.Normalize();

        // 2. 算矩形邊界點
        Vector2 fromBoundary = GetBoundaryPoint(fromRect, fromCenter, dir);
        Vector2 toBoundary = GetBoundaryPoint(toRect, toCenter, -dir);

        // 3. 用邊界點更新線
        lineRenderer.points = new Vector2[]
        {
            fromBoundary,
            toBoundary
        };
        lineRenderer.SetVerticesDirty();

        // 4. hitbox 與箭頭也都用邊界點
        UpdateHitBox(fromBoundary, toBoundary);
        UpdateArrowHead(fromBoundary, toBoundary);
    }

    public void Setup(ReasoningGraphNode from, ReasoningGraphNode to, ReasoningGraphType type)
    {
        fromNode = from;
        toNode = to;
        reasoningGraphType = type;

        if (reasoningGraphType == ReasoningGraphType.LEADTO && arrowHeadPrefab != null)
        {
            arrowHeadRect = Instantiate(arrowHeadPrefab, transform).GetComponent<RectTransform>();
        }

        ApplyColor();
    }

    private void ApplyColor()
    {
        if (lineRenderer == null) return;

        switch (reasoningGraphType)
        {
            case ReasoningGraphType.LEADTO:
                lineRenderer.color = ReasoningGraphManager.Instance.leadToColor;
                break;
            case ReasoningGraphType.CONFLICT:
                lineRenderer.color = ReasoningGraphManager.Instance.conflictColor;
                break;
            case ReasoningGraphType.COHERENT:
                lineRenderer.color = ReasoningGraphManager.Instance.coherentColor;
                break;
            default:
                lineRenderer.color = Color.white;
                break;
        }
    }

    private Vector2 GetNodeCenterInEdgeLayer(RectTransform nodeRect)
    {
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

    private Vector2 GetBoundaryPoint(RectTransform rect, Vector2 center, Vector2 direction)
    {
        Vector2 dir = direction.normalized;

        float halfWidth = rect.rect.width * 0.5f;
        float halfHeight = rect.rect.height * 0.5f;

        float scaleX = Mathf.Abs(dir.x) > 0.0001f ? halfWidth / Mathf.Abs(dir.x) : float.MaxValue;
        float scaleY = Mathf.Abs(dir.y) > 0.0001f ? halfHeight / Mathf.Abs(dir.y) : float.MaxValue;

        float scale = Mathf.Min(scaleX, scaleY);

        return center + dir * scale;
    }

    public void UpdateHitBox(Vector2 fromPos, Vector2 toPos)
    {
        if (hitBox == null) return;

        Vector2 direction = toPos - fromPos;
        float distance = direction.magnitude;
        if (distance < 0.001f) return;

        RectTransform hitBoxRect = hitBox.GetComponent<RectTransform>();
        if (hitBoxRect == null) return;

        hitBox.transform.localPosition = fromPos + direction * 0.5f;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        hitBox.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

        hitBoxRect.sizeDelta = new Vector2(distance, 16f); // 點擊區可以粗一點
    }

    private void UpdateArrowHead(Vector2 start, Vector2 end)
    {
        if (arrowHeadRect == null) return;

        if (reasoningGraphType != ReasoningGraphType.LEADTO)
        {
            arrowHeadRect.gameObject.SetActive(false);
            return;
        }

        arrowHeadRect.gameObject.SetActive(true);

        Vector2 dir = end - start;
        if (dir.sqrMagnitude < 0.001f) return;

        dir.Normalize();

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + ArrowHeadRotationOffset;

        // end 已經是邊界點，所以只需要很小的微調
        Vector2 arrowTipPos = end - dir * ArrowHeadBackDistance;

        arrowHeadRect.anchoredPosition = arrowTipPos;
        arrowHeadRect.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}