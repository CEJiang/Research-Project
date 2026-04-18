using UnityEngine;

public class InteractableContext : BaseContext
{
    public InteractableObject target;

    public InteractableContext(GameObject invoker, InteractableObject target) : base(invoker)
    {
        this.target = target;
    }
}
