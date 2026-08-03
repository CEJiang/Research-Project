using UnityEngine;

public class ReasoningDebtManager : Singleton<ReasoningDebtManager>
{
    [Header("Debt Calculators")]

    [SerializeField]
    private IntegrationDebtCalculator integrationDebtCalculator;

    [SerializeField]
    private InterpretationDebtCalculator interpretationDebtCalculator;

    [SerializeField]
    private TensionDebtCalculator tensionDebtCalculator;

    [Header("Reasoning Debt Weights")]

    [SerializeField, Range(0.0f, 1.0f)]
    private float integrationWeight = 0.3f;
    

    [SerializeField, Range(0.0f, 1.0f)]
    private float interpretationWeight = 0.3f;

    [SerializeField, Range(0.0f, 1.0f)]
    private float tensionWeight = 0.4f;

    public DebtCalculationResult IntegrationDebt { get; private set; }

    public DebtCalculationResult InterpretationDebt { get; private set; }

    public DebtCalculationResult TensionDebt { get; private set; }

    public DebtCalculationResult TotalReasoningDebt { get; private set; }

    public float Coverage { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        ValidateWeights();
    }

    private void OnValidate()
    {
        ValidateWeights();
    }

    public DebtCalculationResult CalculateReasoningDebt()
    {
        IntegrationDebt =
            integrationDebtCalculator != null
                ? integrationDebtCalculator.Calculate(EvidenceManager.Instance.evidences)
                : DebtCalculationResult.Undefined();

        InterpretationDebt =
            interpretationDebtCalculator != null
                ? interpretationDebtCalculator.Calculate(ReasoningAdjustmentManager.Instance.LatestGraphEvaluation, ReasoningGraphManager.Instance.GetEdgeCount())
                : DebtCalculationResult.Undefined();

        TensionDebt =
            tensionDebtCalculator != null
                ? tensionDebtCalculator.Calculate()
                : DebtCalculationResult.Undefined();

        TotalReasoningDebt = AggregateReasoningDebt();

        return TotalReasoningDebt;
    }

    private DebtCalculationResult AggregateReasoningDebt()
    {
        float weightedDebtSum = 0.0f;
        float availableWeightSum = 0.0f;

        if (IntegrationDebt.HasValue)
        {
            weightedDebtSum +=
                integrationWeight *
                IntegrationDebt.Value;

            availableWeightSum += integrationWeight;
        }

        if (InterpretationDebt.HasValue)
        {
            weightedDebtSum +=
                interpretationWeight *
                InterpretationDebt.Value;

            availableWeightSum += interpretationWeight;
        }

        if (TensionDebt.HasValue)
        {
            weightedDebtSum +=
                tensionWeight *
                TensionDebt.Value;

            availableWeightSum += tensionWeight;
        }

        Coverage = availableWeightSum;

        if (availableWeightSum <= 0.0f)
        {
            return DebtCalculationResult.Undefined();
        }

        float reasoningDebt =
            weightedDebtSum /
            availableWeightSum;

        return DebtCalculationResult.Defined(
            reasoningDebt
        );
    }

    private void ValidateWeights()
    {
        integrationWeight = Mathf.Clamp01(
            integrationWeight
        );

        interpretationWeight = Mathf.Clamp01(
            interpretationWeight
        );

        tensionWeight = Mathf.Clamp01(
            tensionWeight
        );
    }
}


[System.Serializable]
public readonly struct DebtCalculationResult
{
    public bool HasValue { get; }

    public float Value { get; }

    public DebtReport Report {get;}

    public DebtCalculationResult(
        bool hasValue,
        float value,
        DebtReport report = null)
    {
        HasValue = hasValue;
        Value = hasValue
            ? Mathf.Clamp01(value)
            : 0.0f;
        Report = report;
    }

    

    public static DebtCalculationResult Defined(
        float value,
        DebtReport report = null)
    {
        return new DebtCalculationResult(
            true,
            value,
            report
        );
    }

    public static DebtCalculationResult Undefined()
    {
        return new DebtCalculationResult(
            false,
            0.0f
        );
    }
}

[System.Serializable]
public class DebtReport {}