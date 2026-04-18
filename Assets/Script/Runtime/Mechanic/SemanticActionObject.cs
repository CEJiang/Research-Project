using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using UnityEngine;

[RequireComponent(typeof(InteractableObject))]
public class SemanticActionObject : MonoBehaviour
{
    [Header("Identity (Authoring)")]
    public string semanticTypeId;
    public string displayNameZh;
    public string displayNameEn;
    public string displayName;

    /// <summary>
    /// A concise description of the fact or action associated with this semantic object.
    /// This should capture the essential information that the LLM needs to interpret
    /// Example: "Inside the backpack was a map, badly worn and faded, with a sticker of a mountain still stuck to it. It was placed next to a bench in the yard."
    /// </summary>
    public List<string> factBulletsZh = new();
    public List<string> factBulletsEn = new();
    public List<string> factBullets;

    /// <summary>
    /// Indicates whether the semantic action has been checked/triggered.
    /// This flag helps prevent redundant processing of the same object.
    /// If true, the object will be ignored in subsequent semantic detections.
    /// </summary>
    public bool isChecked = false;

    /// <summary>
    /// Represents the narrative relevance of this object within the story structure.
    /// This value is author-defined and does not imply mandatory player interaction.
    /// Higher significance objects are more closely related to core clues or story progression,
    /// while lower significance objects primarily contribute to narrative context or atmosphere.
    /// </summary>
    public ObjectSignificance significance;

    /// <summary>
    /// Represents a system-level memory accumulator reflecting the persistence
    /// and frequency of an object's on-screen appearance. This value serves as a
    /// latent salience signal for narrative reasoning, independent of player intent
    /// or awareness.
    /// </summary>
    public float memoryStrength = 0f;
    
    void Start()
    {
        if (TaskLLMManager.Instance.language == TaskLLMManager.Language.Chinese)
        {
            displayName = displayNameZh;
            factBullets = factBulletsZh;
        }
        else
        {
            displayName = displayNameEn;
            factBullets = factBulletsEn;
        }
    }
    public void DecayMemoryStrength(float decayAmount)
    {
        memoryStrength = Mathf.Max(0f, memoryStrength - decayAmount);
    }
}

public enum ObjectSignificance
{
    Critical,   // Core clue or mandatory narrative element
    Supportive, // Optional but informative story or clue extension
    Ambient     // Atmospheric or descriptive element only
}
