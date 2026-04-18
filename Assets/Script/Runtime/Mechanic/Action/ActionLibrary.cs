// using System.Collections.Generic;
// using System.Linq;
// using UnityEditor;
// using UnityEngine;

// public static class ActionLibrary
// {
//     public const string ACTION_PATH = "Actions";

//     public static List<string> allActions = new();

//     static ActionLibrary()
//     {
//         Reload();

// #if UNITY_EDITOR
//         EditorApplication.projectChanged += Reload;
// #endif
//     }

//     public static void Reload()
//     {
//         allActions.Clear();

//         // Interactable Actions
//         var examineActions = Resources.LoadAll<ExamineAction>(ACTION_PATH).ToList();
//         foreach (var action in examineActions)
//             allActions.Add(action.name);

//         // Trigger Actions
//         var enterZoneActions = Resources.LoadAll<EnterZoneAction>(ACTION_PATH).ToList();
//         foreach (var action in enterZoneActions)
//             allActions.Add(action.name);
//     }
// }