using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InfoUI : MonoBehaviour
{
    public TextMeshProUGUI zoneInfoText;
    public TextMeshProUGUI infoText;
    public float delay = 3f;
    void Awake()
    {
        zoneInfoText ??= GameObject.Find("ZoneInfoText").GetComponent<TextMeshProUGUI>();
        infoText ??= GameObject.Find("InfoText").GetComponent<TextMeshProUGUI>();
    }
    public void ShowCurrentZoneInfo(Zone zone)
    {
        zoneInfoText.text = $"{zone.zoneName}";
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