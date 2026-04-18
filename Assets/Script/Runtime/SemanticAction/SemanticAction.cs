using System.Collections.Generic;

[System.Serializable]
public class SemanticAction
{
    public float timestamp;

    public ActionType actionType;

    public string semanticInfo;

    public SemanticAction(float timestamp, ActionType actionType, string semanticInfo)
    {
        this.timestamp = timestamp;
        this.actionType = actionType;
        this.semanticInfo = semanticInfo;
    }
    public override string ToString()
    {
        return $"Timestamp: {timestamp}, ActionType: {actionType}, SemanticInfo: {semanticInfo}";
    }
}

[System.Serializable]
public class SemanticActionArray
{
    public List<SemanticAction> actions;
}