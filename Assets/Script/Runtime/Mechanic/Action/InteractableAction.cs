using UnityEngine.InputSystem;

public abstract class InteractableAction : BaseAction<InteractableContext>
{
    public override bool CanExecute(InteractableContext context)
    {
        // foreach (var requiredAction in requiredActions)
        //     if (!NarrativeManager.Instance.HasActionRecord(requiredAction))
        //         return false;
    
        // foreach (var forbiddenAction in forbiddenActions)
        //     if (NarrativeManager.Instance.HasActionRecord(forbiddenAction))
        //         return false;
        if(context.target.GetComponent<InteractableObject>().hasAction == false)
            return false;
        return true;
    }

    public override void Execute(InteractableContext context)
    {
        InternalExecute(context);

        // if (!string.IsNullOrEmpty(revealAction))
        //     NarrativeManager.Instance.RecordInteractableAction(revealAction);

        KeyboardDataCollector.Instance.FlushSpatialSegment();
    }

    internal abstract void InternalExecute(InteractableContext context);
}
