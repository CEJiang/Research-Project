using System;
using System.Threading.Tasks;
using UnityEngine;

public abstract class BaseLLMManager<T> : Singleton<T>, ILLMManager where T : Component
{
    public event Action<Client, Message> OnMessageUpdated;

    protected override void Awake()
    {
        base.Awake();

        Initialize();
    }

    protected virtual void Initialize() {}

#region Events
    protected void InvokeMessageUpdated(Client client, Message message)
    {
        OnMessageUpdated?.Invoke(client, message);
    }
#endregion

#region Chat
    public abstract void StartNewConversation(Client client, string content = default);
    public abstract Task<string> SendMessage(Client client, string message, JsonSchemaFormat format = default);
#endregion

}