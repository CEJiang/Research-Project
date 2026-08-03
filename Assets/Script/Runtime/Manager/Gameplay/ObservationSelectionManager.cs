using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ObservationSelectionManager : Singleton<ObservationSelectionManager>
{
    public SemanticActionObject currentSelectedActionObject;
    public string imagePath;
    [SerializeField] private float implausiblePenaltyWeight = 2.0f;
    public Dictionary<string, ObservationEvaluationResult> EvidenceObservationResultDictionary { get; private set; } = new();
    public enum ObservationSelectionMode
    {
        None,
        Normal,
        Inspect
    }
    public ObservationSelectionMode currentObservationSelectionMode = ObservationSelectionMode.None;

    public int minSelectedObservations = 1;
    public int maxSelectedObservations = 6;

    void Update()
    {
        if (GameInput.UIInput.Inventory.WasPressedThisFrame)
        {
            ChangeObservationSelectionUIMode();
        }
    }
    private void OnEnable()
    {
        var localizationManager = LocalizationManager.Instance;

        if (localizationManager != null)
        {
            localizationManager.OnLanguageChanged += HandleLanguageChanged;
        }
    }

    private void OnDisable()
    {
        var localizationManager = LocalizationManager.Instance;

        if (localizationManager != null)
        {
            localizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }
    }

    private void HandleLanguageChanged()
    {
        UIManager.Instance.observationSelectionUI.UpdateObservationSelectionUI(currentSelectedActionObject);
    }

    public void SetSemanticActionObject(SemanticActionObject actionObject, string imagePath)
    {
        currentSelectedActionObject = actionObject;
        this.imagePath = imagePath;
        UIManager.Instance.ToggleObservationSelectionUI();

        UIManager.Instance.observationSelectionUI.InitializeObservationSelectionUI(actionObject, imagePath);
    }
    public void CloseObservationSelectionUI()
    {
        currentSelectedActionObject = null;
        imagePath                   = null;
        UIManager.Instance.ToggleObservationSelectionUI();
    }

    public void SetObservationSelectionMode(ObservationSelectionMode mode)
    {
        currentObservationSelectionMode = mode;
    }

    public void ChangeObservationSelectionUIMode()
    {
        if (UIManager.Instance.observationSelectionUI.gameObject.activeSelf)
        {
            if (currentObservationSelectionMode == ObservationSelectionMode.Normal)
            {
                currentObservationSelectionMode = ObservationSelectionMode.Inspect;
                UIManager.Instance.ChangeObservationSelectionFade(true);
            }
            else if (currentObservationSelectionMode == ObservationSelectionMode.Inspect)
            {
                currentObservationSelectionMode = ObservationSelectionMode.Normal;
                UIManager.Instance.ChangeObservationSelectionFade(false);
            }
        }
    }

    public void SubmitSelectedObservations()
    {
        if (currentSelectedActionObject == null)
        {
            Debug.LogWarning(
                "[ObservationSelectionManager] " +
                "No selected action object."
            );

            return;
        }

        List<Toggle> observationToggles =
            UIManager.Instance.observationSelectionUI.observationTexts
                .Select(text => text.GetComponentInParent<Toggle>())
                .Where(toggle => toggle != null)
                .ToList();

        List<int> selectedObservationIndices =
            observationToggles
                .Select((toggle, index) => toggle.isOn ? index : -1)
                .Where(index => index >= 0)
                .ToList();

        // Check the number of selected observations are within the allowed range
        if (selectedObservationIndices.Count < minSelectedObservations ||
            selectedObservationIndices.Count > maxSelectedObservations)
        {
            string message =
                $"Please select between {minSelectedObservations} and " +
                $"{maxSelectedObservations} observations.";

            Debug.LogWarning(
                "[ObservationSelectionManager] " +
                message
            );

            DialogueManager.Instance.ShowInfomation(message);
            return;
        }

        List<ObservationCandidate> allCandidates =
            currentSelectedActionObject.observationCandidates;

        List<ObservationCandidate> selectedCandidates =
            selectedObservationIndices
                .Where(index => index < allCandidates.Count)
                .Select(index => allCandidates[index])
                .ToList();

        ObservationEvaluationResult evaluationResult =
            EvaluateObservations(allCandidates, selectedCandidates);

        List<ObservationCandidate> confirmedTrueFacts =
            evaluationResult.confirmedTrueFacts;

        string evidenceName =
            currentSelectedActionObject.displayNameEn;

        EvidenceObservationResultDictionary[evidenceName] =
            evaluationResult;

        EvidenceManager.Instance.AddEvidence(
            currentSelectedActionObject,
            imagePath,
            evaluationResult.observationReliabilityScore,
            confirmedTrueFacts
        );

        Debug.Log(
            $"[Observation Evaluation] Evidence={evidenceName}, " +
            $"TP={evaluationResult.truePositive}, " +
            $"TN={evaluationResult.trueNegative}, " +
            $"FPc={evaluationResult.falsePositiveCounterfactual}, " +
            $"FPi={evaluationResult.falsePositiveImplausible}, " +
            $"FN={evaluationResult.falseNegative}, " +
            $"Precision={evaluationResult.weightedPrecision:F3}, " +
            $"Recall={evaluationResult.recall:F3}, " +
            $"R_obs={evaluationResult.observationReliabilityScore:F3}, " +
            $"Specificity={evaluationResult.specificity:F3}"
        );

        ClearSelection(observationToggles);

        currentSelectedActionObject = null;
        imagePath                   = null;

        UIManager.Instance.ToggleObservationSelectionUI();
    }

    private ObservationEvaluationResult EvaluateObservations(
        List<ObservationCandidate> allCandidates,
        List<ObservationCandidate> selectedCandidates
    ) {
        ObservationEvaluationResult result = new();

        HashSet<string> selectedCandidateIDs =
            selectedCandidates
                .Select(candidate => candidate.candidateID)
                .ToHashSet();

        foreach (ObservationCandidate candidate in allCandidates)
        {
            bool isSelected =
                selectedCandidateIDs.Contains(candidate.candidateID);

            if (candidate.candidateType ==
                ObservationCandidateType.TrueFact)
            {
                if (isSelected)
                {
                    result.truePositive++;
                    result.confirmedTrueFacts.Add(candidate);
                }
                else
                {
                    result.falseNegative++;
                }

                continue;
            }

            if (!isSelected)
            {
                result.trueNegative++;
                continue;
            }

            if (candidate.candidateType ==
                ObservationCandidateType.CounterfactualDistractor)
            {
                result.falsePositiveCounterfactual++;
            }
            else if (candidate.candidateType ==
                     ObservationCandidateType.ImplausibleDistractor)
            {
                result.falsePositiveImplausible++;
            }
        }

        CalculateMetrics(result);

        return result;
    }

    private void CalculateMetrics(
        ObservationEvaluationResult result
    )
    {
        float weightedFalsePositive =
            result.falsePositiveCounterfactual +
            implausiblePenaltyWeight *
            result.falsePositiveImplausible;

        float precisionDenominator =
            result.truePositive + weightedFalsePositive;

        result.weightedPrecision =
            precisionDenominator > 0f
                ? result.truePositive / precisionDenominator
                : 0f;

        float recallDenominator =
            result.truePositive + result.falseNegative;

        result.recall =
            recallDenominator > 0f
                ? result.truePositive / recallDenominator
                : 0f;

        float reliabilityDenominator =
            result.weightedPrecision + result.recall;

        result.observationReliabilityScore =
            reliabilityDenominator > 0f
                ? 2f *
                  result.weightedPrecision *
                  result.recall /
                  reliabilityDenominator
                : 0f;

        float specificityDenominator =
            result.trueNegative +
            result.falsePositiveCounterfactual +
            result.falsePositiveImplausible;

        result.specificity =
            specificityDenominator > 0f
                ? result.trueNegative / specificityDenominator
                : 0f;

        result.balancedAccuracy =
            (result.recall + result.specificity) / 2f;
    }

    private void ClearSelection(
        List<Toggle> observationToggles
    )
    {
        foreach (Toggle toggle in observationToggles)
        {
            toggle.isOn = false;
        }
    }
}

public class ObservationEvaluationResult
{
    public int truePositive;
    public int trueNegative;

    public int falsePositiveCounterfactual;
    public int falsePositiveImplausible;

    public int falseNegative;
    public List<ObservationCandidate> confirmedTrueFacts = new();

    public float weightedPrecision;
    public float recall;
    public float observationReliabilityScore;

    public float specificity;
    public float balancedAccuracy;
}