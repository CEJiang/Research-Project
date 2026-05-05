using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(InteractableObject))]
public class SemanticActionObject : MonoBehaviour
{
    [Header("Identity (Authoring)")]
    public string semanticTypeId;
    public string displayNameZh;
    public string displayNameEn;
    public List<Fact> facts;
    public Zone zone;

    [SerializeField] private bool isChecked = false;
    [SerializeField] private ObjectSignificance significance;
    public float memoryStrength = 0f;

    public string SemanticTypeId => semanticTypeId;
    public bool IsChecked
    {
        get => isChecked;
        set => isChecked = value;
    }

    public ObjectSignificance Significance => significance;

    public string DisplayName
    {
        get
        {
            if (LocalizationManager.Instance.GetCurrentLanguage() == Language.Chinese)
            {
                return displayNameZh;
            }

            return displayNameEn;
        }
    }
    
    public void DecayMemoryStrength(float decayAmount)
    {
        memoryStrength = Mathf.Max(0f, memoryStrength - decayAmount);
    }
}

public enum ObjectSignificance
{
    Critical,
    Supportive,
    Ambient
}