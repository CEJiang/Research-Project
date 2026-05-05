using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class FactSelectionUI : MonoBehaviour
{
    public CanvasGroup factSelectionCanvasGroup;
    public Text titleText;
    public Image snapshotImage;
    public List<Text> factTexts;

    void Awake()
    {
        factSelectionCanvasGroup = GetComponent<CanvasGroup>();
    }

    public void InitializeFactSelectionUI(SemanticActionObject actionObject, string imagePath)
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

        UpdateFactSelectionUI(actionObject);
    }

    public void UpdateFactSelectionUI(SemanticActionObject actionObject)
    {
        if (actionObject == null) return;

        titleText.text = actionObject.DisplayName;

        List<Fact> facts = actionObject.facts;
        Debug.Log($"Updating Fact Selection UI for {actionObject.DisplayName} with {facts.Count} facts.");
        for (int i = 0; i < factTexts.Count && i < facts.Count; i++)
        {
            factTexts[i].text = facts[i].GetDescription();
        }
    }

    public void SetFactUIFade(bool faded)
    {
        if (faded)
        {
            factSelectionCanvasGroup.alpha = 0.5f;
            factSelectionCanvasGroup.interactable = false;
            factSelectionCanvasGroup.blocksRaycasts = false;
        }
        else
        {
            factSelectionCanvasGroup.alpha = 1f;
            factSelectionCanvasGroup.interactable = true;
            factSelectionCanvasGroup.blocksRaycasts = true;
        }
    }
}
