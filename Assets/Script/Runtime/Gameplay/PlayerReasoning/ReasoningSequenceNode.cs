using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class ReasoningSequenceNode : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI")]
    [SerializeField] private TMP_Text label;

    [Header("Drag")]
    [SerializeField] private float draggingAlpha = 0.7f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;

    private Transform originalParent;
    private int originalSiblingIndex;
    private Vector2 originalAnchoredPosition;

    private SequenceReorderController reorderController;
    public Evidence evidence;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        reorderController = GetComponentInParent<SequenceReorderController>();
    }

    public void Setup(Evidence evidence, RectTransform parentLayer)
    {
        if (label != null && evidence != null)
        {
            this.evidence = evidence;
            label.text = evidence.DisplayName;
        }
    }

    private void OnEnable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
        }
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
        }
    }

    // 這裡接收的是你定義好的 Language Enum，邏輯更乾淨
    private void HandleLanguageChanged()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (evidence == null || label == null) return;
        
        // 統一透過 Evidence 內部的方法獲取當前語言顯示名稱
        // 假設你的 Evidence 類別有實作 GetDisplayName(Language lang)
        label.text = evidence.DisplayName;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        originalAnchoredPosition = rectTransform.anchoredPosition;

        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();

        canvasGroup.alpha = draggingAlpha;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;

        if (reorderController != null)
        {
            reorderController.ShowInsertionMarker(rectTransform, eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        transform.SetParent(originalParent, true);

        if (reorderController != null)
        {
            reorderController.ApplyReorder(rectTransform);
        }
        else
        {
            transform.SetSiblingIndex(originalSiblingIndex);
        }

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }
}