using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class FactSelectionManager : Singleton<FactSelectionManager>
{
    public SemanticActionObject currentSelectedActionObject;
    public string imagePath;

    public enum FactSelectionMode
    {
        None,
        Normal,
        Inspect
    }
    public FactSelectionMode currentFactSelectionMode = FactSelectionMode.None;

    void Update()
    {
        if (GameInput.UIInput.Inventory.WasPressedThisFrame)
        {
            ChangeFactSelectionUIMode();
        }
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
        UIManager.Instance.factSelectionUI.UpdateFactSelectionUI(currentSelectedActionObject);
    }

    public void SetSemanticActionObject(SemanticActionObject actionObject, string imagePath)
    {
        currentSelectedActionObject = actionObject;
        this.imagePath = imagePath;
        UIManager.Instance.ToggleFactSelectionUI();

        UIManager.Instance.factSelectionUI.InitializeFactSelectionUI(actionObject, imagePath);
    }

    public void SetFactSelectionMode(FactSelectionMode mode)
    {
        currentFactSelectionMode = mode;
    }

    public void ChangeFactSelectionUIMode()
    {
        if (UIManager.Instance.factSelectionUI.gameObject.activeSelf)
        {
            if (currentFactSelectionMode == FactSelectionMode.Normal)
            {
                currentFactSelectionMode = FactSelectionMode.Inspect;
                UIManager.Instance.ChangeFactSelectionFade(true);
            }
            else if (currentFactSelectionMode == FactSelectionMode.Inspect)
            {
                currentFactSelectionMode = FactSelectionMode.Normal;
                UIManager.Instance.ChangeFactSelectionFade(false);
            }
        }
    }

    public void SubmitSelectedFact()
    {
        List<Toggle> factToggles = UIManager.Instance.factSelectionUI.factTexts
            .Select(text => text.GetComponentInParent<Toggle>())
            .ToList();
        
        List<int> selectedFactIndices = factToggles
            .Select((toggle, index) => toggle.isOn ? index : -1)
            .Where(index => index != -1)
            .ToList();

        List<Fact> selectedFacts = selectedFactIndices
            .Select(index => currentSelectedActionObject.facts[index])
            .ToList();

        EvidenceManager.Instance.AddEvidence(currentSelectedActionObject, imagePath, selectedFacts);

        // Clear selection and close UI
        foreach (var toggle in factToggles)
        {
            toggle.isOn = false;
        }
        currentSelectedActionObject = null;
        imagePath = null;
        UIManager.Instance.ToggleFactSelectionUI();
    }
}
