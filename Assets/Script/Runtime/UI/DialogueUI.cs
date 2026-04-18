using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    
    public Text primaryText;
    public Text secondaryText;

    void Awake()
    {
        primaryText ??= GameObject.Find("Primary Text").GetComponent<Text>();
        secondaryText ??= GameObject.Find("Secondary Text").GetComponent<Text>();
    }

    public IEnumerator ShowTransparency(float fadeInDuration, float fadeOutDuration, float delay)
    {
        float elapsed = 0f;
        Color primaryColor = primaryText.color;
        Color secondaryColor = secondaryText.color;

        while (elapsed < fadeInDuration)
        {
            float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            primaryText.color = new Color(primaryColor.r, primaryColor.g, primaryColor.b, alpha);
            secondaryText.color = new Color(secondaryColor.r, secondaryColor.g, secondaryColor.b, alpha);

            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(delay);

        // Fade out the inner voice text over a specified duration
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            primaryText.color = new Color(primaryColor.r, primaryColor.g, primaryColor.b, alpha);
            secondaryText.color = new Color(secondaryColor.r, secondaryColor.g, secondaryColor.b, alpha);

            elapsed += Time.deltaTime;
            yield return null;
        }

        ClearMessages();
    }

    public void ClearMessages()
    {
        primaryText.text = "";
        secondaryText.text = "";
    }   
    public void SetPrimaryText(string message)
    {
        primaryText.text = message;
    }
    public void SetSecondaryText(string message)
    {
        secondaryText.text = message;
    }
}
