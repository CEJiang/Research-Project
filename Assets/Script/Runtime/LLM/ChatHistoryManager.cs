using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UnityEditor;
using UnityEngine;

public class ChatHistoryManager : Singleton<ChatHistoryManager>
{
    bool isRecording = false;

    string chatHistoryDirectory;
    Dictionary<Client, List<Message>> clientMessages = new();

    const string AUTO_RECORD_PREF_KEY = "ChatHistory_AutoRecord";
    const string CHAT_HISTORY_FOLDER = "ChatHistory";

    static ChatHistoryManager()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    void Start()
    {
        InitializeChatDirectory();
        InitializeClientMessages();
    }

#region Events
    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        bool autoRecord = EditorPrefs.GetBool(AUTO_RECORD_PREF_KEY, false);

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            if (autoRecord)
            {
                Logger.Developer(Instance, $"Entered Play Mode: Start Record");
                Instance.StartRecord();
            }
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            if (Instance != null)
            {
                Logger.Developer(Instance, $"Exiting Play Mode: Stop Record");
                Instance.StopRecord();
            }
        }
    }

    void HandleMessageUpdate(Client client, Message message)
    {
        clientMessages[client].Add(message);
    }
#endregion

#region Initialization
    void InitializeChatDirectory()
    {
        chatHistoryDirectory = Path.Combine(Application.persistentDataPath, CHAT_HISTORY_FOLDER);

        if (!Directory.Exists(chatHistoryDirectory))
        {
            Directory.CreateDirectory(chatHistoryDirectory);
        }
    }

    void InitializeClientMessages()
    {
        // clientMessages = Enum.GetValues(typeof(Client)).Cast<Client>().ToDictionary(client => client, client => new List<Message>());
        foreach (Client client in Enum.GetValues(typeof(Client)))
        {
            clientMessages[client] = new List<Message>();
        }
    }
#endregion

#region Public Methods
    public void StartRecord()
    {
        if (isRecording) return;
        isRecording = true;

        foreach (var messages in clientMessages.Values)
        {
            messages.Clear();
        }

        ILLMManager[] chats = FindObjectsOfType<MonoBehaviour>().OfType<ILLMManager>().ToArray();
        foreach (ILLMManager chat in chats)
        {
            chat.OnMessageUpdated += HandleMessageUpdate;
        }
    }

    public void StopRecord()
    {
        if (!isRecording) return;
        isRecording = false;

        ILLMManager[] chats = FindObjectsOfType<MonoBehaviour>().OfType<ILLMManager>().ToArray();
        foreach (ILLMManager chat in chats)
        {
            chat.OnMessageUpdated -= HandleMessageUpdate;
        }

        SaveChatHistory();
    }

    void SaveChatHistory()
    {
        // if (clientMessages.Count == 0) return;

        try
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string filename = $"Chat_{timestamp}.json";
            string filepath = Path.Combine(chatHistoryDirectory, filename);

            var options = new JsonSerializerOptions { WriteIndented = true };

            using FileStream createStream = File.Create(filepath);
            JsonSerializer.Serialize(createStream, clientMessages, options);

            Logger.Log(this, $"Chat history saved to: {filepath}");
        }
        catch (Exception e)
        {
            Logger.Error(this, $"Failed to save chat history: {e.Message}");
        }
    }
#endregion

}
