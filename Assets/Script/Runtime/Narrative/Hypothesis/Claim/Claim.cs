using System.Collections.Generic;

[System.Serializable]
public class Claim
{
    public string claimID;
    public string description;
    public string domainID; // D1~D7
    public string informationValue; // Discrete semantic information value
    public List<ClaimEffect> effects = new();
}

[System.Serializable]
public class ClaimEffect
{
    public string hypothesisID;     // H1, H2, H3...
    public Polarity polarity;       // Support or Counter
    public string strength; // Discrete semantic strength

    public float Weight => strength switch
    {
        "None" => 0.0f,
        "Weak" => 0.25f,
        "Moderate" => 0.5f,
        "Strong" => 0.75f,
        "Core" => 1.0f,
        _ => 0.0f
    };
}

[System.Serializable]
public enum Polarity
{
    Support,
    Counter
}