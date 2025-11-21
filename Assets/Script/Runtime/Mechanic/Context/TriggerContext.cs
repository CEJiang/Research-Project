using UnityEngine;

public class TriggerContext : BaseContext
{
    public TriggerObject target;

    public TriggerContext(GameObject invoker, TriggerObject target) : base(invoker)
    {
        this.target = target;
    }
}
