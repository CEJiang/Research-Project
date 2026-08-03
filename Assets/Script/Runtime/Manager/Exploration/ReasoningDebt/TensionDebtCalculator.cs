using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TensionDebtCalculator : MonoBehaviour
{
    public DebtCalculationResult Calculate()
    {
        // Placeholder for actual integration debt calculation logic
        float integrationDebtValue = 0.5f; // Example value
        return DebtCalculationResult.Defined(integrationDebtValue);
    }
}


[Serializable]
public class TensionAnalysis
{
    public List<TensionEpisode> tensionEpisodes = new();
}

[Serializable]
public class TensionEpisode
{
    public string tensionID;

    /// <summary>
    /// 形成張力的 Reasoning Threads。
    /// </summary>
    public List<string> sourceThreadIDs = new();

    public string sharedSemanticTarget;

    /// <summary>
    /// Constrains、Challenges、Contradicts。
    /// </summary>
    public string challengeRelation;

    /// <summary>
    /// 張力來源的語意描述。
    /// </summary>
    public string tensionDescription;

    /// <summary>
    /// Unaddressed、Acknowledged、Qualified、
    /// ConditionallyContained、Integrated。
    /// </summary>
    public string accommodationState;

    /// <summary>
    /// None、ConflictAcknowledgement、
    /// ScopeQualification、ScopeSeparation、
    /// IntermediatePath、CommonCause、
    /// ConditionalAlternatives。
    /// </summary>
    public string accommodationType;

    public string accommodationExplanation;
    public string residualIssue;

    public List<string> sourceNodeIDs = new();
    public List<string> sourceEdgeIDs = new();

    public List<string> resolvingNodeIDs = new();
    public List<string> resolvingEdgeIDs = new();
    public List<string> resolvingThreadIDs = new();

    public string provenance;
}