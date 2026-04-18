using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class RelationNode : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private TMP_Text label;

    [Header("Drag")]
    [SerializeField] private float draggingAlpha = 0.8f;
    [SerializeField] private bool clampInsideNodeLayer = true;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    public RectTransform nodeLayerRect;
    private Canvas canvas;

    private Vector2 pointerOffset;
    private Vector2 originalAnchoredPosition;

    public Evidence evidence;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void Setup(Evidence evidence, RectTransform nodeLayer)
    {
        this.evidence = evidence;
        nodeLayerRect = nodeLayer;

        if (label != null)
        {
            label.text = evidence.displayName;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (RelationGraphManager.Instance.currentRelationGraphType != RelationGraphType.NONE) return;
        if (nodeLayerRect == null) return;

        originalAnchoredPosition = rectTransform.anchoredPosition;

        canvasGroup.alpha = draggingAlpha;
        canvasGroup.blocksRaycasts = false;

        transform.SetAsLastSibling();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            nodeLayerRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPointerPos
        );

        pointerOffset = rectTransform.anchoredPosition - localPointerPos;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (RelationGraphManager.Instance.currentRelationGraphType != RelationGraphType.NONE) return;
        if (nodeLayerRect == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            nodeLayerRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPointerPos))
        {
            Vector2 targetPos = localPointerPos + pointerOffset;

            if (clampInsideNodeLayer)
            {
                targetPos = ClampToNodeLayer(targetPos);
            }

            rectTransform.anchoredPosition = targetPos;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (RelationGraphManager.Instance.currentRelationGraphType != RelationGraphType.NONE) return;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }

    private Vector2 ClampToNodeLayer(Vector2 desiredPos)
    {
        if (nodeLayerRect == null) return desiredPos;

        Rect containerRect = nodeLayerRect.rect;
        Rect nodeRect = rectTransform.rect;

        float halfWidth = nodeRect.width * 0.5f;
        float halfHeight = nodeRect.height * 0.5f;

        float clampedX = Mathf.Clamp(
            desiredPos.x,
            containerRect.xMin + halfWidth,
            containerRect.xMax - halfWidth
        );

        float clampedY = Mathf.Clamp(
            desiredPos.y,
            containerRect.yMin + halfHeight,
            containerRect.yMax - halfHeight
        );

        return new Vector2(clampedX, clampedY);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        RelationGraphManager.Instance.OnRelationNodeClicked(this);
    }
}