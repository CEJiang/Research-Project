using TMPro;
using UnityEngine;

public class EvidenceTooltipManager : MonoBehaviour
{
    public static EvidenceTooltipManager Instance;

    [Header("UI")]
    [SerializeField] private GameObject tooltipRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private RectTransform tooltipRect;

    [Header("Follow Mouse")]
    [SerializeField] private Vector2 offset = new Vector2(20f, -20f);

    private Canvas rootCanvas;

    private void Awake()
    {
        Instance = this;
        rootCanvas = GetComponentInParent<Canvas>();

        if (tooltipRoot != null)
            tooltipRoot.SetActive(false);
    }

    private void Update()
    {
        if (tooltipRoot != null && tooltipRoot.activeSelf)
        {
            FollowMouse();
        }
    }

    public void ShowTooltip(string title, string body)
    {
        if (tooltipRoot == null) return;

        titleText.text = title;
        bodyText.text = body;
        tooltipRoot.SetActive(true);
        FollowMouse();
    }

    public void HideTooltip()
    {
        if (tooltipRoot == null) return;
        tooltipRoot.SetActive(false);
    }

    private void FollowMouse()
    {
        Vector2 screenPos = (Vector2)Input.mousePosition + offset;

        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.transform as RectTransform,
                screenPos,
                rootCanvas.worldCamera,
                out Vector2 localPoint
            );

            tooltipRect.localPosition = localPoint;
        }
        else
        {
            tooltipRect.position = screenPos;
        }
    }
}