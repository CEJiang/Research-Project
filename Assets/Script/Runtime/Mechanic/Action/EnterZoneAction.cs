// using UnityEngine;

// [CreateAssetMenu(fileName = "EnterZoneAction", menuName = "Action/Enter Zone")]
// public class EnterZoneAction : TriggerAction
// {
//     internal override void InternalExecute(TriggerContext context)
//     {
//         Logger.Log(this, $"Enter Zone: {context.target.name}");

//         InnerVoiceManager.Instance.SetPurpose(promptTemplate.observation);
//         InnerVoiceManager.Instance.SetObservation(promptTemplate.observation);
//         InnerVoiceManager.Instance.SetPossibleActions(promptTemplate.possibleActions);
//         InnerVoiceManager.Instance.SetImpossibleActions(null);
//         InnerVoiceManager.Instance.SetRequestType(promptTemplate.requestType);

//         InnerVoiceManager.Instance.GenerateInnerVoice();
//     }
// }
