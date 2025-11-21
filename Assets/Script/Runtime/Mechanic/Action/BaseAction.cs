using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public abstract class BaseAction<T> : ScriptableObject where T : BaseContext
{
    public new string name { get; private set; }

    // Prompts
    // public ActionPromptTemplate promptTemplate;

    // Conditions
    public List<string> requiredActions = new();
    public List<string> forbiddenActions = new();
    public string revealAction = "";

#if UNITY_EDITOR
    void OnEnable()
    {
        RefreshName();
        RefreshActions();
    }

    void OnValidate()
    {
        RefreshName();
        RefreshActions();
    }

    void RefreshName()
    {
        name = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(this));
        name = Regex.Replace(name, @"([a-z0-9])([A-Z])", "$1_$2");   // adds an underscore before every capital letter, except the first
        name = Regex.Replace(name, @"([A-Z])([A-Z][a-z])", "$1_$2"); // process consecutive capital letters (e.g. "XMLParser" -> "xml_parser")
        name = name.ToLower();
    }

    void RefreshActions()
    {
        revealAction = name;
    }
#endif

    public abstract bool CanExecute(T context);
    public abstract void Execute(T context);

}
