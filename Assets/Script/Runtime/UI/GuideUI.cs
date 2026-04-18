using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuideUI : MonoBehaviour
{
    public GameObject defaultInputGuide;
    public GameObject cameraInputGuide;
    public GameObject playerReasoningInputGuide;
    void Awake()
    {
        defaultInputGuide = transform.Find("Content/DefaultInputGuide").gameObject;
        cameraInputGuide = transform.Find("Content/CameraInputGuide").gameObject;
        playerReasoningInputGuide = transform.Find("Content/PlayerReasoningInputGuide").gameObject;
        ShowDefaultInputGuide();
    }

    public void ShowDefaultInputGuide()
    {
        defaultInputGuide.SetActive(true);
        cameraInputGuide.SetActive(false);
        playerReasoningInputGuide.SetActive(false);
    }
    public void ShowCameraInputGuide()
    {
        defaultInputGuide.SetActive(false);
        cameraInputGuide.SetActive(true);
        playerReasoningInputGuide.SetActive(false);
    }
    public void ShowPlayerReasoningInputGuide()
    {
        defaultInputGuide.SetActive(false);
        cameraInputGuide.SetActive(false);
        playerReasoningInputGuide.SetActive(true);
    }
    public void HideAllGuides()
    {
        defaultInputGuide.SetActive(false);
        cameraInputGuide.SetActive(false);
        playerReasoningInputGuide.SetActive(false);
    }
}
