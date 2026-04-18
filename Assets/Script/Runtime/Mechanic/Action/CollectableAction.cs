using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
[CreateAssetMenu(fileName = "CollectableAction", menuName = "Action/Collectable")]
public class CollectableAction : InteractableAction
{
    internal override async void InternalExecute(InteractableContext context)
    {
        context.target.gameObject.GetComponent<InteractableObject>().Highlight(false);

        SnapshotUtility.SetLayerRecursively(context.target.gameObject, LayerMask.NameToLayer("ItemPreview"));

        await SnapshotUtility.CaptureSnapshot(context.target.gameObject);

        InventoryManager.Instance.AddItem(context.target.GetComponent<SemanticActionObject>());

        // Add Semantic Action Object to Semantic Action Manager
        _ = SemanticActionManager.Instance.LogSemanticAction(
            new SemanticAction(
                Time.time,
                ActionType.PlayerObject,
                SemanticActionTemplates.RecordObjectAction(ObjectActionType.PickUp, context.target.gameObject)
            )
        );
    }
}
