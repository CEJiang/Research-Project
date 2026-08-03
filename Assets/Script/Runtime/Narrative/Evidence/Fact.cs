using System;

[Serializable]
public class Fact
{
    public string factID;
    public string descriptionZh;
    public string descriptionEn;
    public string featureID;

    public Fact(string factID, string descriptionZh, string descriptionEn, string featureID)
    {
        this.factID = factID;
        this.descriptionZh = descriptionZh;
        this.descriptionEn = descriptionEn;
        this.featureID = featureID;
    }

    public string GetDescriptionForLLM()
    {
        // For LLM, we will use the English description as the default
        return descriptionEn;
    }

    public string GetDescriptionForUI()
    {
        return (LocalizationManager.Instance.GetCurrentLanguage() == Language.Chinese) ? descriptionZh : descriptionEn;
    }
}