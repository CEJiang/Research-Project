using System;
using System.Collections.Generic;
using System.Text;

public class Evidence
{
    public string evidenceID;
    public string semanticTypeID;
    public string displayNameZh;
    public string displayNameEn;
    public string spatialContext;
    public float observationReliabilityScore;

    public List<Fact> facts;
    public List<Feature> features;
    public List<ClaimSelectionResult> claimResults;
    public Zone zoneAt;
    
    public string imagePath;
    public Evidence(string evidenceID, string semanticTypeID, string displayNameZh, string displayNameEn, string spatialContext, float observationReliabilityScore, List<Fact> facts, Zone zoneAt, string imagePath)
    {
        this.evidenceID = evidenceID;
        this.semanticTypeID = semanticTypeID;

        this.displayNameZh = displayNameZh;
        this.displayNameEn = displayNameEn;
        this.spatialContext = spatialContext;
        this.observationReliabilityScore = observationReliabilityScore;
        this.facts = facts;

        this.zoneAt = zoneAt;
        this.imagePath = imagePath;
    }

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

    // This method is used to get the facts as a formatted string for display purposes
    // It's used in the LLM prompt to provide the facts in a readable format, so we just use the English version here.
    public string GetFactsAsStringForLLM()
    {
        
        StringBuilder sb = new StringBuilder();
        foreach (var fact in facts)
        {
            sb.AppendLine("- " + fact.GetDescriptionForLLM());
        } 
        return sb.ToString();
    }

    public string GetFactsAsStringForUI()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var fact in facts)
        {
            sb.AppendLine("- " + fact.GetDescriptionForUI());
        }
        return sb.ToString();
    }

    public string GetFeaturesAsStringForLLM()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var fact in facts)
        {
            sb.AppendLine($"- Feature ID: {fact.featureID}, Description: {fact.GetDescriptionForLLM()}");
        }
        return sb.ToString();
    }

    public void SetClaimResults(List<ClaimSelectionResult> claimResults)
    {
        this.claimResults = claimResults;
    }
}