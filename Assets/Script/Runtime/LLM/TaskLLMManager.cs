using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using LLMUnity;

[RequireComponent(typeof(LLMCharacter))]
public class TaskLLMManager : BaseTaskLLMManager<TaskLLMManager>
{
    [SerializeField] private LLMProviderType providerType;
    private ILLMProvider provider;
    protected override void Initialize()
    {
        base.Initialize();

        switch (providerType)
        {
            case LLMProviderType.Generic:
                provider = new GenericLLMProvider(GetComponent<LLMCharacter>());
                break;

            case LLMProviderType.OpenAI:
                provider = new OpenAILLMProvider();
                break;
        }
    }

    public override async Task<string> RunTask(
        string userInput,
        JsonSchemaFormat format = default
    )
    {
        var messages = new List<Message>
        {
            new(MessageRole.User, userInput)
        };

        string response = await provider.RunTaskAsync(messages, format);

        Debug.Log($"[TaskLLMManager] Response: {response}");
        return response;
    }
}
