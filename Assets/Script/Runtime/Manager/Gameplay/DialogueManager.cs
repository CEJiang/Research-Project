using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class DialogueManager : Singleton<DialogueManager>
{
    
    public DialogueUI dialogueUI;
    public InfoUI infoUI;

    public void Start()
    {
        dialogueUI ??= GameObject.Find("DialogueUI").GetComponent<DialogueUI>();
        infoUI ??= GameObject.Find("InfoUI").GetComponent<InfoUI>();
        dialogueUI.ClearMessages();
    }

    #region Inner Voice
    public void ShowInnerVoiceMessage(string message, string translationMessage = "")
    {
        dialogueUI.SetPrimaryText(message);
        dialogueUI.SetSecondaryText(translationMessage);

        StartCoroutine(dialogueUI.ShowTransparency(1f, 1f, 5f));
    }
    #endregion

    // Display current zone information in the Zone Info Panel
    public void ShowCurrentZoneInfo(Zone zone)
    {
        infoUI.ShowCurrentZoneInfo(zone);
    }
    public void ShowInfomation(string info)
    {
        infoUI.ShowInfomation(info);
    }
}
