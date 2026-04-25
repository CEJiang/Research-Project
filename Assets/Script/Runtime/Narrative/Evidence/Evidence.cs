using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Localization.Editor;

public class Evidence
{
    public string evidenceId;
    public string semanticTypeId;
    public string displayNameZh;
    public string displayNameEn;

    public List<Fact> facts;
    public Zone zoneAt;
    public string imagePath;
    public Evidence(string evidenceId, string semanticTypeId, string displayNameZh, string displayNameEn, List<Fact> facts, Zone zoneAt, string imagePath)
    {
        this.evidenceId = evidenceId;
        this.semanticTypeId = semanticTypeId;

        this.displayNameZh = displayNameZh;
        this.displayNameEn = displayNameEn;
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
    public string GetEvidenceFactsAsStringForLLM()
    {
        
        StringBuilder sb = new StringBuilder();
        foreach (var fact in facts)
        {
            sb.AppendLine("- " + fact.GetDescriptionForLLM());
        }
        return sb.ToString();
    }

    public string GetEvidenceFactsAsStringForUI()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var fact in facts)
        {
            sb.AppendLine("- " + fact.GetDescription());
        }
        return sb.ToString();
    }
}