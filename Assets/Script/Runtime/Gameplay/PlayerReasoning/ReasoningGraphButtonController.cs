using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReasoningGraphButtonController : MonoBehaviour
{
    public Button noneButton;
    public Button leadToButton;
    public Button conflictButton;
    public Button coherentButton;

    public Color normalColor = Color.white;
    public Color activeColor = new(144/255f, 144/255f, 144/255f, 190/255f);

    void Start()
    {
        RefreshButtonVisual();
    }

    public void RefreshButtonVisual()
    {
        SetButtonColor(noneButton, ReasoningGraphManager.Instance.currentReasoningGraphType == ReasoningGraphType.NONE);
        SetButtonColor(leadToButton, ReasoningGraphManager.Instance.currentReasoningGraphType == ReasoningGraphType.LEADTO);
        SetButtonColor(conflictButton, ReasoningGraphManager.Instance.currentReasoningGraphType == ReasoningGraphType.CONFLICT);
        SetButtonColor(coherentButton, ReasoningGraphManager.Instance.currentReasoningGraphType == ReasoningGraphType.COHERENT);
    }

    void SetButtonColor(Button button, bool isActive)
    {
        button.targetGraphic.color = isActive ? activeColor : normalColor;
    }

    public void OnNoneButtonClicked()
    {
        ReasoningGraphManager.Instance.SetCurrentReasoningGraphType(ReasoningGraphType.NONE);
    }
    
    // Click the "lead to" button
    public void OnLeadToButtonClicked()
    {
        ReasoningGraphManager.Instance.SetCurrentReasoningGraphType(ReasoningGraphType.LEADTO);
    } 

    // Click the "conflict" button
    public void OnConflictButtonClicked()
    {
        ReasoningGraphManager.Instance.SetCurrentReasoningGraphType(ReasoningGraphType.CONFLICT);
    }

    // Click the "coherent" button
    public void OnCoherentButtonClicked()
    {
        ReasoningGraphManager.Instance.SetCurrentReasoningGraphType(ReasoningGraphType.COHERENT);
    }  
}
