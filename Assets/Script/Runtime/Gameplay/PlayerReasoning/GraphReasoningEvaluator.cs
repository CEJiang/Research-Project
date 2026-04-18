using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class GraphReasoningEvaluator : MonoBehaviour
{
    public void OnSaveButtonClicked()
    {
        // Call the EvaluateGraph method when the save button is clicked
        _ = EvaluateGraph();
    }

    public async Task EvaluateGraph()
    {
        if (RelationGraphManager.Instance.IsGraphEmpty())
        {
            Debug.LogWarning("Relation graph is empty. Please add nodes and edges before evaluating.");
            return;
        }

        // Placeholder for graph evaluation logic
        Debug.Log("Evaluating the relation graph...");

        string graphData = RelationGraphManager.Instance.GetRelationGraphDataForLLM();
        Debug.Log("Graph Data for LLM:\n" + graphData);

        // Call the Reasoning Validity Manager to evaluate the validity of the reasoning based on the graph
        ReasoningValidityResult validityResult = await ReasoningValidityManager.Instance.GenerateReasoningValidity();

        // If the validity is valid, call the Reasoning Adjustment Manager to generate reasoning adjustments
        if (validityResult.scoringAllowed)
        {
            Debug.Log("[GraphReasoningEvaluator] Reasoning validity passed. Generating reasoning adjustment...");

            ReasoningAdjustmentResponse reasoningAdjustmentResponse =
                await ReasoningAdjustmentManager.Instance.GenerateReasoningAdjustment();

            if (reasoningAdjustmentResponse == null)
            {
                Debug.LogError("[GraphReasoningEvaluator] ReasoningAdjustmentResponse is null.");
                return;
            }

            if (reasoningAdjustmentResponse.adjustments == null)
            {
                Debug.LogWarning("[GraphReasoningEvaluator] ReasoningAdjustmentResponse.adjustments is null.");
            }
            else
            {
                foreach (var item in reasoningAdjustmentResponse.adjustments)
                {
                    Debug.Log(
                        $"Hypothesis: {item.hypothesisId}, " +
                        $"RelationSupport: {item.relationSupport:F2}, " +
                        $"RelationOpposition: {item.relationOpposition:F2}, " +
                        $"SequenceSupport: {item.sequenceSupport:F2}, " +
                        $"FinalScore: {item.score:F2}, " +
                        $"Reason: {item.reason}"
                    );
                }
            }

            Dictionary<string, float> reasoningAdjustment =
                ReasoningAdjustmentManager.Instance.ConvertResponseToDictionary(reasoningAdjustmentResponse);

            Debug.Log("[GraphReasoningEvaluator] Updating hypothesis reasoning adjustment...");

            // 3. 將最終 score 更新到 hypothesis state
            HypothesisStateManager.Instance.UpdateReasoningAdjustment(reasoningAdjustment);

            Debug.Log("[GraphReasoningEvaluator] Hypothesis reasoning adjustment updated.");
            
        }
        // If the validity is invalid, show the reason to the player and do not update the Hypothesis Confidence Level
        else
        {   
            Debug.LogWarning("Iusse found in reasoning validity: \n");
            foreach (var issue in validityResult.issues)
            {
                Debug.LogWarning($"- {issue.description}");
            }
        }
    }
}
