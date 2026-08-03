using System;

public enum Dimension
{
    Structure,   // STR // 結構：形狀、尺寸、對齊、完整性、附著性、穩定性、對稱性
    Surface,     // SUR // 表面：磨損、鏽蝕、污染、痕跡、材質、紋理
    Spatial,     // SPA // 空間：位置、方向、排列、開口、距離、邊界
    Interaction, // INT // 交互：接觸、來源、作用、結果、工具、對象
    Temporal     // TMP // 時間：痕跡年齡、時序、持續時間、頻率、演變

}

public enum ObservationCandidateType
{
    TrueFact,
    CounterfactualDistractor,
    ImplausibleDistractor
}

[Serializable]
public class ObservationCandidate
{
    public string candidateID;
    public Dimension dimension;
    public string descriptionZh;
    public string descriptionEn;
    public string featureID;
    public ObservationCandidateType candidateType;

    public ObservationCandidate(
        string candidateID,
        Dimension dimension,
        string descriptionZh,
        string descriptionEn,
        string featureID,
        ObservationCandidateType type)
    {
        this.candidateID = candidateID;
        this.dimension = dimension;
        this.descriptionZh = descriptionZh;
        this.descriptionEn = descriptionEn;
        this.featureID = featureID;
        this.candidateType = type;
    }

    public string GetDescriptionForLLM()
    {
        return string.IsNullOrWhiteSpace(descriptionEn)
            ? descriptionZh
            : descriptionEn;
    }

    public string GetDescription()
    {
        LocalizationManager localizationManager =
            LocalizationManager.Instance;

        if (localizationManager == null)
            return descriptionEn;

        return localizationManager.GetCurrentLanguage() == Language.Chinese
            ? descriptionZh
            : descriptionEn;
    }
}