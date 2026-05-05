using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InfoUI : MonoBehaviour
{
    public Text zoneInfoText;
    public TextMeshProUGUI infoText;
    public float delay = 3f;
    void Awake()
    {
        zoneInfoText ??= GameObject.Find("ZoneInfoText").GetComponent<Text>();
        infoText ??= GameObject.Find("InfoText").GetComponent<TextMeshProUGUI>();
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
        ShowCurrentZoneInfo();
    }

    public void ShowCurrentZoneInfo()
    {
        zoneInfoText.text = $"{ZoneManager.Instance.GetZoneDisplayNameForUI(ZoneManager.Instance.currentZone)}";
    }

    public void ShowInfomation(string info)
    {
        infoText.text = info;
        StartCoroutine(ClearInfoAfterDelay());
        Debug.Log(info);
    }

    IEnumerator ClearInfoAfterDelay()
    {
        yield return new WaitForSeconds(delay);
        infoText.text = "";
    }
}