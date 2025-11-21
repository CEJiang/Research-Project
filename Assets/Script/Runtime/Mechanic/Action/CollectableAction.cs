using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "CollectableAction", menuName = "Action/Collectable")]
public class CollectableAction : InteractableAction
{
    internal override void InternalExecute(InteractableContext context)
    {
        Destroy(context.target.gameObject);
    }
}
