using System.Collections.Generic;
using UnityEngine;

public class ClaimDataLoader : Singleton<ClaimDataLoader>
{
    public List<Claim> claims = new();
    protected override void Awake()
    {
        base.Awake();
        LoadClaimDataFromJSON();
    }
    void Start()
    {
        foreach (var claim in claims)
        {
            Debug.Log($"[ClaimDataLoader] Loaded Claim: ID={claim.claimID}, Description={claim.description}");
        }
        
        ClaimManager.Instance.claims = claims;
    }

    public void LoadClaimDataFromJSON()
    {
        TextAsset jsonTextFile = Resources.Load<TextAsset>("Data/Claims");

        if (jsonTextFile == null)
        {
            Debug.LogError("Failed to load Claim JSON file from Resources/Data/Claims.json");
            return;
        }

         ClaimList claimList =
            JsonUtility.FromJson<ClaimList>(jsonTextFile.text);

        if (claimList == null || claimList.claims == null)
        {
            Debug.LogError("Claim JSON format is invalid.");
            return;
        }

        claims = claimList.claims;
    }
}

[System.Serializable]
public class ClaimList
{
    public List<Claim> claims;
}