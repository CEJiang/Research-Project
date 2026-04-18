using System;
using System.Collections.Generic;
using UnityEngine;

public class InnerVoiceManager : Singleton<InnerVoiceManager>
{
    private const string SYSTEM_TEMPLATE_PATH = "Prompts/IVoiceSystem";
    private const string TRANSLATION_TEMPLATE_PATH = "Prompts/IVoiceTranslation";

    [Header("Prompt")]
    [SerializeField] private SystemPromptTemplate systemTemplate;
    [SerializeField] private ActionPromptTemplate actionTemplate;
    [SerializeField] private SimplePromptTemplate translationTemplate;

    private string prompt;

    public string Response { get; private set; }
    public string Translation { get; private set; }
    public AudioClip AudioClip { get; private set; }

    public event Action<string, AudioClip> OnInnerVoiceGenerated;
    public event Action<string> OnTranslationReady;

    void Start()
    {
        // Load templates
        var startTemplateAsset = Instantiate(Resources.Load<SystemPromptTemplateAsset>(SYSTEM_TEMPLATE_PATH));
        startTemplateAsset.ApplyTo(systemTemplate);

        var translationTemplateAsset = Instantiate(Resources.Load<SimplePromptTemplateAsset>(TRANSLATION_TEMPLATE_PATH));
        translationTemplateAsset.ApplyTo(translationTemplate);

        InitLLM();
    }

    private void InitLLM()
    {
        prompt = PromptBuilder.Build(systemTemplate);
        LLMManager.Instance.StartNewConversation(Client.General, prompt);

        prompt = PromptBuilder.Build(translationTemplate);
        LLMManager.Instance.StartNewConversation(Client.Translation, prompt);

        Logger.Log(this, "Inner Voice Initialized");
    }

    public void GenerateInnerVoice()
    {
        Debug.Log("Generating Inner Voice...");
        HandleNarrativeAsync();
    }

    private async void HandleNarrativeAsync()
    {
        // Set last relevant action
        actionTemplate.lastRelevantAction = NarrativeManager.Instance.GetLastActionRecord();

        // Generate prompt text and send to LLM
        prompt = PromptBuilder.Build(actionTemplate);
        Response = await LLMManager.Instance.SendMessage(Client.General, prompt);

        Logger.Log(this, $"Response: {Response}");

        // Async translation and TTS tasks
        HandleTranslationAsync(Response);
        // HandleTTSAsync(Response);
    }

    private async void HandleTranslationAsync(string response)
    {
        Translation = await LLMManager.Instance.SendMessage(Client.Translation, response);
        OnTranslationReady?.Invoke(Translation);

        Logger.Log(this, $"Translation: {Translation}");
    }

    // private async void HandleTTSAsync(string response)
    // {
    //     AudioClip = await PiperManager.Instance.TextToSpeech(response);
    //     OnInnerVoiceGenerated?.Invoke(Response, AudioClip);
    // }

#region Setters
    public void SetPurpose(string purpose)
    {
        actionTemplate.purpose = purpose;
    }

    public void SetObservation(string observation)
    {
        actionTemplate.observation = observation;
    }

    public void SetPossibleActions(List<string> possibleActions)
    {
        actionTemplate.possibleActions = possibleActions;
    }

    public void SetImpossibleActions(List<string> impossibleActions)
    {
        actionTemplate.impossibleActions = impossibleActions;
    }

    public void SetRequestType(RequestType requestType)
    {
        actionTemplate.requestType = requestType;
    }
#endregion
}
