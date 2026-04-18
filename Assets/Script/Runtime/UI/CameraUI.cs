using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CameraUI : MonoBehaviour
{
    public TextMeshProUGUI displayNameText;

    void Awake()
    {
        displayNameText = GetComponentInChildren<TextMeshProUGUI>();
        displayNameText.text = "";
    }
    
}
