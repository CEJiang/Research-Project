using System.Collections.Generic;
using UnityEngine;

public class NarrativeManager : Singleton<NarrativeManager>
{
    private HashSet<string> actions = new();

    [SerializeField] private List<string> actionRecord = new();
    [SerializeField] private List<string> interactableActionRecord = new();
    [SerializeField] private List<string> triggerActionRecord = new();

    private void RecordAction(string action)
    {
        actions.Add(action);
        actionRecord.Add(action);
    }

    public void RecordInteractableAction(string action)
    {
        interactableActionRecord.Add(action);
        RecordAction(action);
    }

    public void RecordTriggerAction(string action)
    {
        triggerActionRecord.Add(action);
        RecordAction(action);
    }

    public bool HasActionRecord(string action)
    {
        return actions.Contains(action);
    }

    public string GetLastActionRecord()
    {
        if (actionRecord.Count == 0) return null;
        return actionRecord[actionRecord.Count - 1];
    }
}
