using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "DoorOpenAction", menuName = "Action/DoorOpen")]
public class DoorOpenAction : InteractableAction
{
    private const float openAngle = -75f;    
    internal override void InternalExecute(InteractableContext context)
    {
        context.target.GetComponent<MonoBehaviour>().StartCoroutine(OpenDoorCoroutine(context.target.gameObject));
    }

    private IEnumerator OpenDoorCoroutine(GameObject door)
    {
        Quaternion targetRotation = Quaternion.Euler(door.transform.eulerAngles + new Vector3(0, openAngle, 0));
        float duration = 0.75f;
        float elapsed = 0f;
        Quaternion initialRotation = door.transform.rotation;
        while (elapsed < duration)
        {
            door.transform.rotation = Quaternion.Slerp(initialRotation, targetRotation, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        door.transform.rotation = targetRotation;

        Destroy(door.GetComponent<InteractableObject>());
    }
}
