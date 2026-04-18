using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using LLMUnity;
using Unity.VisualScripting;

[RequireComponent(typeof(LLMCharacter))]
public class LLMManager : BaseLLMManager<LLMManager>
{
    // Provider
    [SerializeField] LLMProviderType providerType;
    ILLMProvider provider;

    // Conversation record
    Dictionary<Client, List<Message>> clientMessages;

    protected override void Initialize()
    {
        base.Initialize();

        switch (providerType)
        {
            case LLMProviderType.Generic:
                var chat = GetComponent<LLMCharacter>();
                provider = new GenericLLMProvider(chat);
                break;
            case LLMProviderType.OpenAI:
                provider = new OpenAILLMProvider();
                break;
        }

        clientMessages = Enum.GetValues(typeof(Client)).Cast<Client>().ToDictionary(client => client, client => new List<Message>());
    }

#region Chat
    public override void StartNewConversation(Client client, string content = null)
    {
        clientMessages[client].Clear();

        if (content != null)
        {
            Message message = new(MessageRole.System, content);
            clientMessages[client].Add(message);
            InvokeMessageUpdated(client, message);
        }
    }

    public override async Task<string> SendMessage(Client client, string message, JsonSchemaFormat format = default)
    {
        Message requestMessage = new(MessageRole.User, message);
        clientMessages[client].Add(requestMessage);
        InvokeMessageUpdated(client, requestMessage);

        var pastMessages = clientMessages[client].Take(clientMessages[client].Count - 1).ToList();
        string response = await provider.GetChatResponseAsync(pastMessages, message, format);

        Debug.Log($"[LLM Manager] {client} Response: {response}");
        
        Message responseMessage = new(MessageRole.Assistant, response);
        clientMessages[client].Add(responseMessage);
        InvokeMessageUpdated(client, responseMessage);
        
        return responseMessage.content;
    }
#endregion

}
