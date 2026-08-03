using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
[RequireComponent(typeof(CanvasGroup))]
public class EvidenceListItem : MonoBehaviour, 
    IPointerEnterHandler,
    IPointerExitHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{

    [Header("Drag Settings")]
    [SerializeField] private float draggingAlpha = 0.6f;

    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    private Transform originalParent;
    private int originalSiblingIndex;
    private Vector2 originalAnchoredPosition;


    [Header("Evidence Data")]
    public Evidence evidence;
    public TextMeshProUGUI evidenceNameText;

    public void SetEvidence(Evidence evidence)
    {
        this.evidence = evidence;
        evidenceNameText.text = evidence.DisplayName;
    }

    private void OnEnable()
    {
        if (LocalizationManager.HasInstance)
        {
            LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
        }
    }

    private void OnDisable()
    {
        if (LocalizationManager.HasInstance)
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
        if (evidence == null || evidenceNameText == null) return;
        
        // 統一透過 Evidence 內部的方法獲取當前語言顯示名稱
        // 假設你的 Evidence 類別有實作 GetDisplayName(Language lang)
        evidenceNameText.text = evidence.DisplayName;
    }

    // We can show the evidence details in a tooltip when the player hovers over the evidence item in the list. The tooltip can display information such as the evidence description, type, and any relevant clues or connections to other evidence.
    public void OnPointerEnter(PointerEventData eventData)
    {
        
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            Debug.LogError("[EvidenceItemUI] No parent Canvas found.");
        }
    }

    #region Drag and Drop
    
    // Drag the evidence item to the evidence reasoning graph or Reasoning Sequence to use it.
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (canvas == null) return;

        // Cache original hierarchy info so this source item can return to the list afterward.
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        originalAnchoredPosition = rectTransform.anchoredPosition;

        // Move to top-level canvas while dragging so it is not clipped by the ScrollView / Viewport.
        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();

        // Make it semi-transparent and allow drop targets underneath to receive raycasts.
        canvasGroup.alpha = draggingAlpha;
        canvasGroup.blocksRaycasts = false;
    }

    // While dragging, update the position of the evidence item to follow the mouse cursor.
    // Maybe we can show a block to indicate the valid drop targets (e.g., evidence reasoning graph or Reasoning Sequence) when dragging the evidence item.
    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;

        // Move with cursor, corrected by canvas scale factor.
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    // When the player releases the mouse button, check if the evidence item is dropped on a valid target (e.g., evidence reasoning graph or Reasoning Sequence) or check if it yet exists in the evidence reasoning graph. If it is, use the evidence item in that context.
    public void OnEndDrag(PointerEventData eventData)
    {
        // Restore this source item back to the evidence list.
        transform.SetParent(originalParent, true);
        transform.SetSiblingIndex(originalSiblingIndex);
        rectTransform.anchoredPosition = originalAnchoredPosition;

        // Restore normal visual / raycast state.
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }

    #endregion
}
