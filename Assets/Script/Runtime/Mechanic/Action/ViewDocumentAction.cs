using UnityEngine;

[CreateAssetMenu(fileName = "ViewDocumentAction", menuName = "Action/ViewDocument")]
public class ViewDocumentAction : InteractableAction
{
    [Header("Inspect Target")]
    [SerializeField] private GameObject documentPrefabOrSource;

    [Header("Local Transform In Inspect View")]
    [SerializeField] private Vector3 inspectLocalPosition = new Vector3(0f, 0f, 0.6f);
    [SerializeField] private Vector3 inspectLocalEulerAngles = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 inspectLocalScale = Vector3.one;

    internal override async void InternalExecute(InteractableContext context)
    {
        if (InspectionManager.Instance == null)
        {
            Debug.LogError("ViewDocumentAction: InspectionManager not found in scene.");
            return;
        }

        if (documentPrefabOrSource == null)
        {
            Debug.LogWarning($"ViewDocumentAction [{name}] documentPrefabOrSource is null.");
            return;
        }

        InspectionManager.Instance.ShowInspectObject(
            documentPrefabOrSource,
            inspectLocalPosition,
            inspectLocalEulerAngles,
            inspectLocalScale
        );
        context.target.GetComponent<InteractableObject>()?.Highlight(false);
        
        await System.Threading.Tasks.Task.CompletedTask;
    }
}