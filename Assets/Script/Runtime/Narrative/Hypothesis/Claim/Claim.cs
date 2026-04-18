using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Claim", menuName = "Narrative/Claim")]
public class Claim : ScriptableObject
{
    public string id;
    public string description;
    public List<string> requiredFeatures = new();
    public string domainId; // D1~D7

    public List<ClaimEffect> effects;  
}

[System.Serializable]
public class ClaimEffect
{
    public string hypothesisId; // H1, H2, H3...
    public Polarity polarity;   // Support or Counter
    public float weight = 1f;   // Strength of support/counter
}

[System.Serializable]
public enum Polarity
{
    Support,
    Counter
}
