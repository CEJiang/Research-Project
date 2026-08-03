using System;
using System.Collections.Generic;
using UnityEngine;

#region Output Data Models

[Serializable]
public class IntegrationDebtReport : DebtReport
{
    [Header("Core Metrics")]
    [Tooltip("Raw unintegrated workload W_I(t). Sum of all local evidence debts.")]
    public float unintegratedWorkload; // W_I(t)

    [Tooltip("Bounded Integration Debt score D_I(t) in range [0, 1).")]
    public float debtScore;            // D_I(t)

    [Tooltip("Execution layer mapped debt score (0 when undefined/empty evidence list).")]
    public float debtScoreExec;        // D_I_exec(t)

    [Header("Diagnostics & Coverage")]
    [Tooltip("Count of evidence items with local debt d_I(e) > 0. O_I(t)")]
    public int outstandingEvidenceCount; // O_I(t)

    [Tooltip("Integration coverage C_I(t) = Fully Integrated Evidences / Total Valid Evidences.")]
    public float integrationCoverage;   // C_I(t)

    [Header("Trend & Degradation")]
    [Tooltip("Debt score change rate Delta D_I(t) = D_I(t) - D_I(t-1).")]
    public float deltaDebt;            // ΔD_I(t)

    [Tooltip("True if Delta D_I(t) exceeds degradation threshold.")]
    public bool isIntegrationDegraded;

    public bool isDefined;

    [Header("Evidence Breakdown")]
    public List<SingleEvidenceDebtDetail> evidenceDetails = new List<SingleEvidenceDebtDetail>();
}

[Serializable]
public class SingleEvidenceDebtDetail
{
    public string evidenceId;
    public bool hasNode;            // N_e(t)
    public bool hasConnectedEdge;   // L_e(t)
    public float completionScore;   // c_I(e) = eta * N_e + (1 - eta) * L_e
    public float localDebt;         // d_I(e) = 1 - c_I(e)
}

#endregion

public class IntegrationDebtCalculator
{
    [Header("Integration Weights")]
    [Tooltip("Weight eta for Node externalization N_e(t). Default 0.40 (40% for Node, 60% for Edge link).")]
    [SerializeField, Range(0f, 1f)] private float nodeWeight = 0.40f; // η

    [Header("Saturation Configuration")]
    [Tooltip("Fixed half-saturation workload H_I. Workload value where D_I reaches 0.5.")]
    [SerializeField] private float halfSaturationWorkload = 2.5f; // H_I

    [Header("Trend Analysis")]
    [Tooltip("Threshold for Delta D_I to trigger integration degradation warning.")]
    [SerializeField] private float degradationDeltaThreshold = 0.05f;

    // State memory for trend analysis ΔD_I(t)
    private float previousDebtScore = 0f;

    /// <summary>
    /// 重置歷史狀態（用於重新開始關卡、切換案件或載入存檔時）
    /// </summary>
    public void ResetHistory()
    {
        previousDebtScore = 0f;
    }

    /// <summary>
    /// 根據標準 4 階段半飽和模型計算 Integration Debt
    /// </summary>
    /// <param name="evidences">玩家目前已取得且可用的 Evidence 列表</param>
    public DebtCalculationResult Calculate(IReadOnlyList<Evidence> evidences)
    {
        // 1. 無資料 / 證物集為空處理 (|E_t| = 0)
        if (evidences == null || evidences.Count == 0)
        {
            ResetHistory();
            return DebtCalculationResult.Undefined();
        }

        // 過濾出有效 Evidence
        List<Evidence> validEvidences = new List<Evidence>();
        foreach (var e in evidences)
        {
            if (IsValidEvidence(e))
            {
                validEvidences.Add(e);
            }
        }

        int validCount = validEvidences.Count;
        if (validCount <= 0)
        {
            ResetHistory();
            return DebtCalculationResult.Undefined();
        }

        IntegrationDebtReport report = new IntegrationDebtReport
        {
            isDefined = true
        };

        float totalWorkload = 0f;        // W_I(t)
        int outstandingCount = 0;        // O_I(t)
        int fullyIntegratedCount = 0;    // 用於計算覆蓋率

        // 2. 逐項計算局部完成度 c_I(e) 與局部債務 d_I(e)
        foreach (Evidence evidence in validEvidences)
        {
            bool hasNode = HasNode(evidence);
            bool hasLink = hasNode && HasConnectedEdge(evidence); // L_e <= N_e

            float nodeState = hasNode ? 1f : 0f;
            float linkState = hasLink ? 1f : 0f;

            // c_I(e) = eta * N_e + (1 - eta) * L_e
            float safeEta = Mathf.Clamp01(nodeWeight);
            float c_I = (safeEta * nodeState) + ((1f - safeEta) * linkState);
            c_I = Mathf.Clamp01(c_I);

            // d_I(e) = 1 - c_I(e)
            float d_I = 1f - c_I;

            totalWorkload += d_I;

            if (d_I > 0f)
            {
                outstandingCount++;
            }

            if (Mathf.Approximately(c_I, 1.0f))
            {
                fullyIntegratedCount++;
            }

            report.evidenceDetails.Add(new SingleEvidenceDebtDetail
            {
                evidenceId = evidence.evidenceID,
                hasNode = hasNode,
                hasConnectedEdge = hasLink,
                completionScore = c_I,
                localDebt = d_I
            });
        }

        // 3. 計算整合覆蓋率 C_I(t)
        report.integrationCoverage = (float)fullyIntegratedCount / validCount;

        // 4. 輸出累積待辦工作量 W_I(t) 與未完成數 O_I(t)
        report.unintegratedWorkload = totalWorkload;
        report.outstandingEvidenceCount = outstandingCount;

        // 5. 指數半飽和映射 D_I(t) = 1 - 2^(-W_I / H_I)
        float safeH_I = Mathf.Max(0.0001f, halfSaturationWorkload);
        float saturationExponent = -totalWorkload / safeH_I;

        report.debtScore = 1f - Mathf.Pow(2f, saturationExponent); // D_I(t)
        report.debtScoreExec = report.debtScore;                   // D_I_exec(t)

        // 6. 計算趨勢 ΔD_I(t) 與退化診斷
        report.deltaDebt = report.debtScore - previousDebtScore;
        report.isIntegrationDegraded = report.deltaDebt >= degradationDeltaThreshold;

        // 更新歷史狀態
        previousDebtScore = report.debtScore;

        return DebtCalculationResult.Defined(report.debtScoreExec, report);
    }

    #region Helper Validation & Graph Access

    private bool IsValidEvidence(Evidence evidence)
    {
        return evidence != null && !string.IsNullOrWhiteSpace(evidence.evidenceID);
    }

    private bool HasNode(Evidence evidence)
    {
        ReasoningGraphManager graphManager = ReasoningGraphManager.Instance;
        if (graphManager == null) return false;

        return graphManager.IsEvidenceInGraph(evidence.evidenceID);
    }

    private bool HasConnectedEdge(Evidence evidence)
    {
        ReasoningGraphManager graphManager = ReasoningGraphManager.Instance;
        if (graphManager == null) return false;

        return graphManager.HasConnectedEdgeInGraph(evidence.evidenceID);
    }

    #endregion
}