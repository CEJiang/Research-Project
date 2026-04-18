using System;
using System.Threading.Tasks;

public interface ILLMManager
{
    event Action<Client, Message> OnMessageUpdated;

    void StartNewConversation(Client client, string content = default);
    Task<string> SendMessage(Client client, string message, JsonSchemaFormat format = default);
}