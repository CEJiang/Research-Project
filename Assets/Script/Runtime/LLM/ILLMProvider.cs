using System.Collections.Generic;
using System.Threading.Tasks;

public interface ILLMProvider
{
    // Chat mode (stateful)
    Task<string> GetChatResponseAsync(
        List<Message> messages,
        string latestMessage,
        JsonSchemaFormat format = default
    );

    // Task mode (stateless)
    Task<string> RunTaskAsync(
        List<Message> messages,
        JsonSchemaFormat format = default
    );
}
