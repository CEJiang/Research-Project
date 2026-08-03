using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class ObservationSelectionUI : MonoBehaviour
{
    public CanvasGroup observationSelectionCanvasGroup;
    public Text titleText;
    public Image snapshotImage;
    public GameObject backgroundMask;
    public List<Text> observationTexts;
    public RawImage ObjectPreviewRawImage;

    void Awake()
    {
        observationSelectionCanvasGroup = GetComponent<CanvasGroup>();
    }

    public void InitializeObservationSelectionUI(SemanticActionObject actionObject, string imagePath)
    {
        if (snapshotImage != null)
        {
            byte[] fileData = File.ReadAllBytes(imagePath);
            Texture2D tex = new(2, 2);
            tex.LoadImage(fileData);

            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f)
            );

            snapshotImage.sprite = sprite;
        }

        UpdateObservationSelectionUI(actionObject);

        // Reset the observation Selction UI scroll position to the top
        ResetScrollPosition();
    }

    public void UpdateObservationSelectionUI(SemanticActionObject actionObject)
    {
        if (actionObject == null) return;

        titleText.text = actionObject.DisplayName;

        List<ObservationCandidate> observationCandidates = actionObject.observationCandidates;
        Debug.Log($"Updating Observation Selection UI for {actionObject.DisplayName} with {observationCandidates.Count} observation candidates.");
        for (int i = 0; i < observationTexts.Count && i < observationCandidates.Count; i++)
        {
            observationTexts[i].text = observationCandidates[i].GetDescription();
            observationTexts[i].gameObject.GetComponent<RectTransform>().parent.gameObject.SetActive(true);
        }

        for (int i = observationCandidates.Count; i < observationTexts.Count; i++)
        {
            observationTexts[i].gameObject.GetComponent<RectTransform>().parent.gameObject.SetActive(false);
        }
    }

    public void ResetScrollPosition()
    {
        ScrollRect scrollRect = GetComponentInChildren<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f; // Reset to the top
        }
    }

    public void SetObservationUIFade(bool faded)
    {
        if (faded)
        {
            observationSelectionCanvasGroup.alpha = 0.5f;
            observationSelectionCanvasGroup.interactable = false;
            observationSelectionCanvasGroup.blocksRaycasts = false;
            ObjectPreviewRawImage.gameObject.SetActive(false);
            backgroundMask.SetActive(false);
        }
        else
        {
            observationSelectionCanvasGroup.alpha = 1f;
            observationSelectionCanvasGroup.interactable = true;
            observationSelectionCanvasGroup.blocksRaycasts = true;
            ObjectPreviewRawImage.gameObject.SetActive(true);
            backgroundMask.SetActive(true);
        }
    }
}
