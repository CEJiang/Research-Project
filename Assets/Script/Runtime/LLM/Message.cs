using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using OpenAI.Chat;

public class Message
{
    Dictionary<Type, MessageRole> roleMapping = new()
    {
        { typeof(SystemChatMessage), MessageRole.System },
        { typeof(UserChatMessage), MessageRole.User },
        { typeof(AssistantChatMessage), MessageRole.Assistant },
        { typeof(ToolChatMessage), MessageRole.Tool },
        // { typeof(FunctionChatMessage), MessageRole.Function },
        // { typeof(DeveloperChatMessage), MessageRole.Developer }
    };

    public MessageRole role { get; set; }
    public string content { get; set; }

    [JsonConstructor]
    public Message(MessageRole role, string content)
    {
        this.role = role;
        this.content = content;
    }

    public Message(MessageRole role, ChatMessageContent content)
    {
        this.role = role;
        this.content = content[0].Text;
    }

    public Message(ChatMessage chatMessage)
    {
        Type messageType = chatMessage.GetType();

        if (!roleMapping.ContainsKey(messageType))
        {
            throw new ArgumentException($"Unsupported chat message type: {messageType.Name}");
        }

        role = roleMapping[messageType];
        content = chatMessage.Content[0].Text;
    }

    public override string ToString()
    {
        return content;
    }

    public ChatMessage ToChatMessage()
    {
        return role switch
        {
            MessageRole.System => new SystemChatMessage(content),
            MessageRole.User => new UserChatMessage(content),
            MessageRole.Assistant => new AssistantChatMessage(content),
            MessageRole.Tool => new ToolChatMessage(content),
            // MessageRole.Function => new FunctionChatMessage(content),
            // MessageRole.Developer => new DeveloperChatMessage(content),
            _ => throw new ArgumentException($"Unsupported chat message role: {role}"),
        };
    }

}