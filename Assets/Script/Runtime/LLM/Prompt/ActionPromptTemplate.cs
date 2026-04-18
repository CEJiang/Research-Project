using System.Collections.Generic;

[System.Serializable]
public class ActionPromptTemplate : PromptTemplate
{
    public string purpose;
    public string observation;
    public List<string> possibleActions;
    public List<string> impossibleActions;
    public string lastRelevantAction;
    public RequestType requestType;

    public override string ToPromptText()
    {
        List<string> segments = new();

        if (!string.IsNullOrEmpty(purpose))
            segments.Add($"<Purpose: {purpose}>");

        if (!string.IsNullOrEmpty(observation))
            segments.Add($"You observe: {observation}.");

        if (possibleActions?.Count > 0)
            segments.Add($"Possible actions: {string.Join(", ", possibleActions)}.");

        if (impossibleActions?.Count > 0)
            segments.Add($"Avoid: {string.Join(", ", impossibleActions)}.");

        if (!string.IsNullOrEmpty(lastRelevantAction))
            segments.Add($"Previously, the player did: {lastRelevantAction}.");

        if (requestType != RequestType.None)
            segments.Add($"{requestType.ToPromptText()}.");

        segments.Add($"Reply with no more than 1 sentence.");

        return string.Join("\n", segments).Trim();
    }
}
