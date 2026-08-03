using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ReasoningGraphEvaluator : MonoBehaviour
{
    public void OnSaveButtonClicked()
    {
        // Call the EvaluateGraph method when the save button is clicked
        _ = EvaluateGraph();
    }

    public async Task EvaluateGraph()
    {
        if (ReasoningGraphManager.Instance.IsGraphEmpty())
        {
            Debug.LogWarning("Reasoning graph is empty. Please add nodes and edges before evaluating.");
            return;
        }

        // Placeholder for graph evaluation logic
        Debug.Log("Evaluating the reasoning graph...");

        ReasoningAdjustmentResponse reasoningAdjustmentResponse = await ReasoningAdjustmentManager.Instance.GenerateReasoningAdjustment();

        if (reasoningAdjustmentResponse == null)
        {
            Debug.LogError("[GraphReasoningEvaluator] ReasoningAdjustmentResponse is null.");
            return;
        }

        Dictionary<string, float> reasoningAdjustment =
            ReasoningAdjustmentManager.Instance.ConvertResponseToDictionary(reasoningAdjustmentResponse);

        Debug.Log("[GraphReasoningEvaluator] Updating hypothesis reasoning adjustment...");

        // 3. 將最終 score 更新到 hypothesis state
        HypothesisStateManager.Instance.UpdateReasoningAdjustment(reasoningAdjustment);

        Debug.Log("[GraphReasoningEvaluator] Hypothesis reasoning adjustment updated.");
        
    }
}
