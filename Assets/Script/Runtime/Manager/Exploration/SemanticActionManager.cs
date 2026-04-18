using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using LLMUnity;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

public class SemanticActionManager : Singleton<SemanticActionManager>
{
    private const string SYSTEM_TEMPLATE_PATH = "Prompts/SemanticActionSystem";
    public List<SemanticAction> actionLog = new();

    private string prompt;
    
    [Header("Prompt")]
    public SystemPromptTemplate systemTemplate;

    // Steps: Log Action 
    // Zone: Enter, Exit, and Transition
        // undo:
        // done: Enter, Exit, Transition

    // Object: Interact, PickUp, Drop, Use, Examine, Open, Close, nearby, faraway, scan
        // undo: Interact, PickUp, Drop, Use, Examine, Open, Close
        // done: nearby, faraway, scan

    void Start()
    {
        // Load templates
        var startTemplateAsset = Instantiate(Resources.Load<SystemPromptTemplateAsset>(SYSTEM_TEMPLATE_PATH));
        startTemplateAsset.ApplyTo(systemTemplate);

        InitLLM();
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        SaveToJSON();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            TriggerLLM();
        }
    }
    public async void TriggerLLM()
    {
        await ExecuteLLM();
    }


    public async Task LogSemanticAction(SemanticAction action)
    {
        actionLog.Add(action);
    }

    #region LLM Interaction
    public void InitLLM()
    {
        prompt = PromptBuilder.Build(systemTemplate);
        LLMManager.Instance.StartNewConversation(Client.General, prompt);

        Logger.Log(this, "Semantic Action Manager Initialized");
    }

    public async Task ExecuteLLM()
    {
        string message = await LLMManager.Instance.SendMessage(Client.General, $"{ActionToString()}");
        Logger.Log(this, $"LLM Response: {message}");
    }

    public string ActionToString()
    {
        List<string> actionStrings = new();
        foreach (var action in actionLog)
        {
            actionStrings.Add(action.ToString());
        }
        return string.Join("\n", actionStrings);
    }
    #endregion

    #region Save Logs
    public void SaveToJSON()
    {
        string json = JsonUtility.ToJson(new SemanticActionArray { actions = actionLog }, true);

        string directoryPath = $"{Application.dataPath}/Logs/{Setup.Player}";
        if (!System.IO.Directory.Exists(directoryPath))
            System.IO.Directory.CreateDirectory(directoryPath);
        
        string filePath = $"{directoryPath}/SemanticActions_{RandomNumberGenerator.GetInt32(int.MaxValue)}.json";
        while (System.IO.File.Exists(filePath))
        {
            filePath = $"{directoryPath}/SemanticActions_{RandomNumberGenerator.GetInt32(int.MaxValue)}.json";
        }
        
        System.IO.File.AppendAllText(
            filePath,
            json
        );
    }
    #endregion
}
