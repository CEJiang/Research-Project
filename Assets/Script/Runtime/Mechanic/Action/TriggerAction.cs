public abstract class TriggerAction : BaseAction<TriggerContext>
{
    public override bool CanExecute(TriggerContext context)
    {
        // foreach (var requiredAction in requiredActions)
        //     if (!NarrativeManager.Instance.HasActionRecord(requiredAction))
        //         return false;
    
        // foreach (var forbiddenAction in forbiddenActions)
        //     if (NarrativeManager.Instance.HasActionRecord(forbiddenAction))
        //         return false;

        return true;
    }

    public override void Execute(TriggerContext context)
    {
        InternalExecute(context);

        // if (!string.IsNullOrEmpty(revealAction))
        //     NarrativeManager.Instance.RecordTriggerAction(revealAction);
    }

    internal abstract void InternalExecute(TriggerContext context);
}
