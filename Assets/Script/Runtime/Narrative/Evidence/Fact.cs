using System;
using UnityEngine;

// 使用 Enum 方便後續研究數據統計 (Spatial, Temporal, Interaction, Existence)
public enum FactDimension
{
    Spatial,
    Temporal,
    Interaction,
    Existence
}

[Serializable] // 讓這個類別可以在 Inspector 或 JsonUtility 中被序列化
public class Fact
{
    public string factID;
    public FactDimension dimension;
    public string descriptionZh;
    public string descriptionEn;
    public string featureID;

    // 建構子
    public Fact(string id, FactDimension dim, string zh, string en, string featID)
    {
        factID = id;
        dimension = dim;
        descriptionZh = zh;
        descriptionEn = en;
        featureID = featID;
    }

    public string GetDescriptionForLLM()
    {
        // For LLM, we will use the English description as the default
        return descriptionEn;
    }

    public string GetDescription(string language = "Zh")
    {
        return (language == "Zh") ? descriptionZh : descriptionEn;
    }
}