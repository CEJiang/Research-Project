using System;
using System.Collections;
using System.Collections.Generic;

public class Evidence
{
    public string evidenceId;
    public string semanticTypeId;
    public string displayNameZh;
    public string displayNameEn;

    public List<string> factBulletsZh;
    public List<string> factBulletsEn;

    public string displayName;
    public List<string> factBullets;

    public Zone zoneAt;
    public string imagePath;
    public Evidence(string evidenceId, string semanticTypeId, string displayNameZh, string displayNameEn, List<string> factBulletsZh, List<string> factBulletsEn, Zone zoneAt, string imagePath)
    {
        this.evidenceId = evidenceId;
        this.semanticTypeId = semanticTypeId;

        this.displayNameZh = displayNameZh;
        this.displayNameEn = displayNameEn;

        this.factBulletsZh = factBulletsZh;
        this.factBulletsEn = factBulletsEn;

        this.zoneAt = zoneAt;
        this.imagePath = imagePath;

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

    // This method is used to get the fact bullets as a formatted string for display purposes
    // It's used in the LLM prompt to provide the fact bullets in a readable format, so we just use the English version here.
    public string GetEvidenceFactBulletsAsString()
    {
        string result = "- " + string.Join("\n- ", factBulletsEn);
        return result;
    }

    public string GetEvidenceFactBulletsAsStringForUI()
    {
        string result = "- " + string.Join("\n- ", factBullets);
        return result;
    }
}