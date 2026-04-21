using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(InteractableObject))]
public class SemanticActionObject : MonoBehaviour
{
    [Header("Identity (Authoring)")]
    public string semanticTypeId;
    public string displayNameZh;
    public string displayNameEn;

    public List<string> factBulletsZh = new();
    public List<string> factBulletsEn = new();

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
            if (TaskLLMManager.Instance != null &&
                TaskLLMManager.Instance.language == TaskLLMManager.Language.Chinese)
            {
                return displayNameZh;
            }

            return displayNameEn;
        }
    }

    public IReadOnlyList<string> FactBullets
    {
        get
        {
            if (TaskLLMManager.Instance != null &&
                TaskLLMManager.Instance.language == TaskLLMManager.Language.Chinese)
            {
                return factBulletsZh;
            }

            return factBulletsEn;
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