using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ReasoningGraphCursorController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private RectTransform graphRect;
    [SerializeField] private RectTransform modeDotRect;
    [SerializeField] private Image modeDotImage;

    [Header("Offset From Mouse")]
    [SerializeField] private Vector2 dotOffset = new Vector2(0f, 0f);

    private bool isPointerInsideGraph = false;
    private Canvas parentCanvas;

    private void Awake()
    {
        parentCanvas = GetComponentInParent<Canvas>();

        if (modeDotRect != null)
        {
            modeDotRect.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isPointerInsideGraph || graphRect == null || modeDotRect == null || modeDotImage == null)
            return;

        UpdateDotVisual();
        UpdateDotPosition();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInsideGraph = true;
        UpdateDotVisual();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInsideGraph = false;

        if (modeDotRect != null)
        {
            modeDotRect.gameObject.SetActive(false);
        }
    }

    private void UpdateDotVisual()
    {
        var mode = ReasoningGraphManager.Instance.currentReasoningGraphType;

        switch (mode)
        {
            case ReasoningGraphType.LEADTO:
                modeDotRect.gameObject.SetActive(true);
                modeDotImage.color = ReasoningGraphManager.Instance.leadToColor;
                break;

            case ReasoningGraphType.CONFLICT:
                modeDotRect.gameObject.SetActive(true);
                modeDotImage.color = ReasoningGraphManager.Instance.conflictColor;
                break;

            case ReasoningGraphType.COHERENT:
                modeDotRect.gameObject.SetActive(true);
                modeDotImage.color = ReasoningGraphManager.Instance.coherentColor;
                break;

            default:
                modeDotRect.gameObject.SetActive(false);
                break;
        }
    }

    private void UpdateDotPosition()
    {
        Camera cam = null;
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = parentCanvas.worldCamera;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            graphRect,
            Input.mousePosition,
            cam,
            out Vector2 localPoint))
        {
            modeDotRect.anchoredPosition = localPoint + dotOffset;
        }
    }
}