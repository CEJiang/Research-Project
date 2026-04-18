using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RelationGraphButtonController : MonoBehaviour
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
        SetButtonColor(noneButton, RelationGraphManager.Instance.currentRelationGraphType == RelationGraphType.NONE);
        SetButtonColor(leadToButton, RelationGraphManager.Instance.currentRelationGraphType == RelationGraphType.LEADTO);
        SetButtonColor(conflictButton, RelationGraphManager.Instance.currentRelationGraphType == RelationGraphType.CONFLICT);
        SetButtonColor(coherentButton, RelationGraphManager.Instance.currentRelationGraphType == RelationGraphType.COHERENT);
    }

    void SetButtonColor(Button button, bool isActive)
    {
        button.targetGraphic.color = isActive ? activeColor : normalColor;
    }

    public void OnNoneButtonClicked()
    {
        RelationGraphManager.Instance.SetCurrentRelationGraphType(RelationGraphType.NONE);
    }
    
    // Click the "lead to" button
    public void OnLeadToButtonClicked()
    {
        RelationGraphManager.Instance.SetCurrentRelationGraphType(RelationGraphType.LEADTO);
    } 

    // Click the "conflict" button
    public void OnConflictButtonClicked()
    {
        RelationGraphManager.Instance.SetCurrentRelationGraphType(RelationGraphType.CONFLICT);
    }

    // Click the "coherent" button
    public void OnCoherentButtonClicked()
    {
        RelationGraphManager.Instance.SetCurrentRelationGraphType(RelationGraphType.COHERENT);
    }  
}
