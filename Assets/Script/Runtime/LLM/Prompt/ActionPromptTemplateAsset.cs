
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "LLM/Action Prompt Template")]
public class ActionPromptTemplateAsset : ScriptableObject
{
    public string purpose;
    public string observation;
    public List<string> possibleActions;
    public List<string> impossibleActions;
    public string lastRelevantAction;
    public RequestType requestType;

    public void ApplyTo(ActionPromptTemplate template)
    {
        template.purpose = purpose;
        template.observation = observation;
        template.possibleActions = possibleActions;
        template.impossibleActions = impossibleActions;
        template.lastRelevantAction = lastRelevantAction;
        template.requestType = requestType;
    }
}
