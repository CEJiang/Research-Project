// using UnityEngine;

// [CreateAssetMenu(fileName = "ExamineAction", menuName = "Action/Examine")]
// public class ExamineAction : InteractableAction
// {
//     internal override void InternalExecute(InteractableContext context)
//     {
//         Logger.Log(this, $"Examine: {context.target.name}");

//         InnerVoiceManager.Instance.SetPurpose(promptTemplate.observation);
//         InnerVoiceManager.Instance.SetObservation(promptTemplate.observation);
//         InnerVoiceManager.Instance.SetPossibleActions(null);
//         InnerVoiceManager.Instance.SetImpossibleActions(null);
//         InnerVoiceManager.Instance.SetRequestType(promptTemplate.requestType);

//         InnerVoiceManager.Instance.GenerateInnerVoice();
//     }
// }
