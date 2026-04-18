using System.Collections.Generic;
using System.Threading.Tasks;
using LLMUnity;

public class GenericLLMProvider : ILLMProvider
{
    private readonly LLMCharacter chat;

    public GenericLLMProvider(LLMCharacter character)
    {
        chat = character;
        chat.seed = -1;
    }

    public async Task<string> GetChatResponseAsync(List<Message> messages, string latestMessage, JsonSchemaFormat format = null)
    {
        chat.ClearChat();

        // Rebuild the full conversation context up to the last user message.
        foreach (Message message in messages)
        {
            MessageRole role = message.role;
            string content = message.content;

            switch (role)
            {
                case MessageRole.System:
                    chat.SetPrompt(content);
                    break;
                case MessageRole.User:
                    chat.AddPlayerMessage(content);
                    break;
                case MessageRole.Assistant:
                    chat.AddAIMessage(content);
                    break;
                default:
                    break;
            }
        }

        // Set response format if provided json schema
        if (format != default)
        {
            chat.grammarString = Converter.JsonSchemaToGBNF(
                jsonSchema: format.jsonSchema, 
                rootClassName: format.formatName
            );
        }
        else
        {
            chat.grammarString = null;
        }

        // Send the request
        string response = await chat.Chat(latestMessage);
        return response;
    }

    public Task<string> RunTaskAsync(List<Message> messages, JsonSchemaFormat format = null)
    {
        throw new System.NotImplementedException();
    }
}
