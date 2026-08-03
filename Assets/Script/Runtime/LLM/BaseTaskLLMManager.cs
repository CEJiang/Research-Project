using System;
using System.Threading.Tasks;
using UnityEngine;

public abstract class BaseTaskLLMManager<T> : Singleton<T>, ITaskLLMManager where T : Component
{
    protected override void Awake()
    {
        base.Awake();
        Initialize();
    }

    protected virtual void Initialize() {}

    /// <summary>
    /// Stateless task execution
    /// </summary>
    public abstract Task<string> RunTask(
        string userInput,
        JsonSchemaFormat format = default
    );
}
