using System.Collections;
using System.Security.Cryptography;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class DropdownAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform dropdownPanel;
    [SerializeField] private TextMeshProUGUI dropdownText;

    [Header("Animation")]
    [SerializeField] private float expandedHeight = 150f;
    [SerializeField] private float duration = 0.2f;

    private Coroutine currentCoroutine;
    public bool isExpanded = false;

    private void Awake()
    {
        if (dropdownPanel != null)
        {
            SetHeight(0f);
        }
    }

    public void Toggle()
    {
        if (dropdownPanel == null) return;

        float targetHeight = isExpanded ? 0f : expandedHeight;

        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        currentCoroutine = StartCoroutine(AnimateHeight(targetHeight));
        isExpanded = !isExpanded;
        if (isExpanded)
        {
            dropdownText.text = "Relation ▲";
        }
        else
        {
            dropdownText.text = "Relation ▼";
        }
    }

    public void Open()
    {
        if (dropdownPanel == null || isExpanded) return;

        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        currentCoroutine = StartCoroutine(AnimateHeight(expandedHeight));
        isExpanded = true;
    }

    public void Close()
    {
        if (dropdownPanel == null || !isExpanded) return;

        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        currentCoroutine = StartCoroutine(AnimateHeight(0f));
        isExpanded = false;
    }

    private IEnumerator AnimateHeight(float targetHeight)
    {
        float startHeight = dropdownPanel.sizeDelta.y;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // 可以換成別的 easing
            float eased = EaseOutCubic(t);

            float height = Mathf.Lerp(startHeight, targetHeight, eased);
            SetHeight(height);

            yield return null;
        }

        SetHeight(targetHeight);
        currentCoroutine = null;
    }

    private void SetHeight(float height)
    {
        Vector2 size = dropdownPanel.sizeDelta;
        size.y = height;
        dropdownPanel.sizeDelta = size;
    }

    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}