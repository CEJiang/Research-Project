using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Chat;

public class OpenAILLMProvider : ILLMProvider
{
    private readonly ChatClient client;
    private readonly string model;

    public OpenAILLMProvider()
    {
        client = new ChatClient(
            model: EnvLoader.GetValue("OPENAI_MODEL"),
            apiKey: EnvLoader.GetValue("OPENAI_API_KEY")
        );
    }

    public async Task<string> GetChatResponseAsync(List<Message> messages, string latestMessage, JsonSchemaFormat format = null)
    {
        // Rebuild the full conversation context up to the last user message.
        List<ChatMessage> chatMessages = messages.Select(m => m.ToChatMessage()).ToList();
        chatMessages.Add(new UserChatMessage(latestMessage));

        // Set response format if provided json schema
        ChatCompletionOptions options = null;
        if (format != default)
        {
            options = new()
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: format.formatName,
                    jsonSchema: BinaryData.FromBytes(Encoding.UTF8.GetBytes(format.jsonSchema)),
                    jsonSchemaIsStrict: true
                )
            };
        }
        
        // Send the request
        ChatCompletion chatResult = await client.CompleteChatAsync(
            messages: chatMessages,
            options: options
        );

        return chatResult.Content[0].Text;
    }

    public async Task<string> RunTaskAsync(
        List<Message> messages,
        JsonSchemaFormat format = null
    )
    {
        // 1️⃣ 將內部 Message 轉成 OpenAI ChatMessage
        List<ChatMessage> chatMessages =
            messages.Select(m => m.ToChatMessage()).ToList();

        // 2️⃣ 設定 JSON Schema（如果有）
        ChatCompletionOptions options = null;
        if (format != null)
        {
            options = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: format.formatName,
                    jsonSchema: BinaryData.FromBytes(
                        Encoding.UTF8.GetBytes(format.jsonSchema)
                    ),
                    jsonSchemaIsStrict: true
                )
            };
        }

        // 3️⃣ 呼叫 OpenAI（無對話假設）
        ChatCompletion result = await client.CompleteChatAsync(
            messages: chatMessages,
            options: options
        );

        // 4️⃣ 回傳文字結果
        return result.Content[0].Text;
    }

}

