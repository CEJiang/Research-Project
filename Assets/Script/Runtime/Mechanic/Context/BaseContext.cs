using UnityEngine;

public abstract class BaseContext
{
    public GameObject invoker;

    public BaseContext(GameObject invoker)
    {
        this.invoker = invoker;
    }
}
