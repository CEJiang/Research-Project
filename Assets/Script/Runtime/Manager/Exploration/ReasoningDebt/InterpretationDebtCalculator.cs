using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#region Output Data Models

[Serializable]
public class InterpretationDebtReport : DebtReport
{
    [Header("Core Metrics")]
    [Tooltip("Raw uninterpreted workload W_P(t). Sum of all local edge debts.")]
    public float uninterpretedWorkload; // W_P(t)

    [Tooltip("Bounded Interpretation Debt score D_P(t) in range [0, 1).")]
    public float debtScore;            // D_P(t)

    [Tooltip("Execution layer mapped debt score (0 when undefined/empty graph).")]
    public float debtScoreExec;        // D_P_exec(t)

    [Header("Diagnostics & Coverage")]
    [Tooltip("Count of edges with local debt d_P(e) > 0. O_P(t)")]
    public int problemEdgeCount;       // O_P(t)

    [Tooltip("Evaluation coverage C_P(t) = Evaluated Edges / Total Graph Edges.")]
    public float evaluationCoverage;   // C_P(t)

    [Header("Trend & Degradation")]
    [Tooltip("Debt score change rate Delta D_P(t) = D_P(t) - D_P(t-1).")]
    public float deltaDebt;            // ΔD_P(t)

    [Tooltip("True if Delta D_P(t) exceeds degradation threshold.")]
    public bool isExplanatoryDegraded;

    public bool isDefined;

    [Header("Edge Breakdown")]
    public List<SingleEdgeDebtDetail> edgeDetails = new List<SingleEdgeDebtDetail>();
}

[Serializable]
public class SingleEdgeDebtDetail
{
    public string edgeId;
    public string reasoningType;
    public string validity;
    public string strength;
    public string primaryIssue;

    public float validityScore; // V_e
    public float strengthScore; // S_e
    public float qualityScore;  // q_e = 0.6*V_e + 0.4*S_e
    public float localDebt;     // d_P(e)
}

#endregion

public class InterpretationDebtCalculator
{
    [Header("Quality Scoring Weights")]
    [SerializeField, Range(0f, 1f)] private float validityWeight = 0.60f;
    [SerializeField, Range(0f, 1f)] private float strengthWeight = 0.40f;

    [Header("Quality Threshold")]
    [SerializeField, Range(0f, 1f)] private float qualityThreshold = 0.60f; // τ_P

    [Header("Saturation Configuration")]
    [SerializeField] private float halfSaturationWorkload = 2.0f; // H_P

    [Header("Trend Analysis")]
    [SerializeField] private float degradationDeltaThreshold = 0.05f;

    private float previousDebtScore = 0f;

    /// <summary>
    /// 重置歷史狀態（用於重新開始關卡或載入存檔時）
    /// </summary>
    public void ResetHistory()
    {
        previousDebtScore = 0f;
    }

    public DebtCalculationResult Calculate(
        ReasoningGraphEvaluationResponse graphEvaluation, 
        int totalEdgesInGraph)
    {
        List<ReasoningEdgeEvaluation> evaluatedEdges = graphEvaluation?.edgeEvaluations;
        int evaluatedCount = evaluatedEdges?.Count ?? 0;

        // 1. 無資料 / 圖形為空處理 (|E_t| = 0)
        if (totalEdgesInGraph <= 0)
        {
            ResetHistory();
            return DebtCalculationResult.Undefined();
        }

        InterpretationDebtReport report = new()
        {
            isDefined = true,
            // 2. 計算評估覆蓋率 C_P(t)
            evaluationCoverage = Mathf.Clamp01((float)evaluatedCount / totalEdgesInGraph)
        };

        float totalWorkload = 0f; // W_P(t)
        int problemEdgeCount = 0;  // O_P(t)

        // 3. 計算已由 LLM 評估的 Edge 品質 q_e 與局部債務 d_P(e)
        if (evaluatedEdges != null)
        {
            foreach (var edge in evaluatedEdges)
            {
                if (edge == null) continue;

                float vScore = GetValidityScore(edge.validity);
                float sScore = GetStrengthScore(edge.strength);

                // q_e = 0.6 * V_e + 0.4 * S_e
                float q_e = (validityWeight * vScore) + (strengthWeight * sScore);

                // d_P(e) = max(0, (tau_P - q_e) / tau_P)
                float safeTau = Mathf.Max(0.0001f, qualityThreshold);
                float d_P = Mathf.Max(0f, (safeTau - q_e) / safeTau);

                totalWorkload += d_P;

                if (d_P > 0f)
                {
                    problemEdgeCount++;
                }

                report.edgeDetails.Add(new SingleEdgeDebtDetail
                {
                    edgeId = edge.edgeId,
                    reasoningType = edge.playerReasoningType,
                    validity = edge.validity,
                    strength = edge.strength,
                    primaryIssue = edge.primaryIssue,
                    validityScore = vScore,
                    strengthScore = sScore,
                    qualityScore = q_e,
                    localDebt = d_P
                });
            }
        }

        // 4. 🔥 修復漏洞：採計未評估/待處理 Edge 的最高待辦債務 (d_P = 1.0)
        int unevaluatedCount = Mathf.Max(0, totalEdgesInGraph - evaluatedCount);
        if (unevaluatedCount > 0)
        {
            totalWorkload += unevaluatedCount * 1.0f; // 未評估連線視為完全未解釋，產生最大債務
            problemEdgeCount += unevaluatedCount;
        }

        // 5. 輸出累積待辦工作量 W_P(t) 與問題 Edge 數 O_P(t)
        report.uninterpretedWorkload = totalWorkload;
        report.problemEdgeCount = problemEdgeCount;

        // 6. 指數半飽和映射 D_P(t) = 1 - 2^(-W_P / H_P)
        float safeH_P = Mathf.Max(0.0001f, halfSaturationWorkload);
        float saturationExponent = -totalWorkload / safeH_P;
        
        report.debtScore = 1f - Mathf.Pow(2f, saturationExponent); // D_P(t)
        report.debtScoreExec = report.debtScore;                   // D_P_exec(t)

        // 7. 計算趨勢 ΔD_P(t) 與退化診斷
        report.deltaDebt = report.debtScore - previousDebtScore;
        report.isExplanatoryDegraded = report.deltaDebt >= degradationDeltaThreshold;

        // 更新歷史狀態
        previousDebtScore = report.debtScore;

        return DebtCalculationResult.Defined(report.debtScoreExec, report);
    }

    #region Robust Helper Score Mappings

    private float GetValidityScore(string validity)
    {
        if (string.IsNullOrWhiteSpace(validity)) return 0.0f;

        string normalized = validity.Trim().ToLowerInvariant();

        if (normalized.Contains("valid") && !normalized.Contains("invalid"))
        {
            return 1.0f;
        }
        if (normalized.Contains("partial") || normalized.Contains("partially"))
        {
            return 0.5f;
        }

        return 0.0f; // Invalid
    }

    private float GetStrengthScore(string strength)
    {
        if (string.IsNullOrWhiteSpace(strength)) return 0.0f;

        string normalized = strength.Trim().ToLowerInvariant();

        if (normalized.Contains("core")) return 1.00f;
        if (normalized.Contains("strong")) return 0.75f;
        if (normalized.Contains("moderate")) return 0.50f;
        if (normalized.Contains("weak")) return 0.25f;

        return 0.00f; // Unsupported / None / Invalid
    }

    #endregion
}