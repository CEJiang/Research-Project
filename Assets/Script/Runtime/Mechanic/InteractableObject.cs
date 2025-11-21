using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Outline))]
public class InteractableObject : MonoBehaviour
{

    // Attributes
    private Outline outline;
    public string displayName;
    public string description;
    [SerializeField] private List<InteractableAction> actions = new();
    public bool hasAction => actions != null && actions.Count > 0;

    void Awake()
    {
        outline = gameObject.GetComponent<Outline>();
        outline.OutlineMode = Outline.Mode.OutlineAll;
        outline.OutlineColor = Color.yellow;
        outline.OutlineWidth = 5f;
        outline.enabled = false;
    }

    public void Highlight(bool enable)
    {
        outline.enabled = enable;
    }

    void OnDestroy()
    {
        outline.enabled = false;      
    }

    #region Interact
    public List<InteractableAction> GetAvailableActions(GameObject invoker = null)
    {
        var context = new InteractableContext(invoker, this);
        return actions.Where(action => action.CanExecute(context)).ToList();
    }

    public void ExecuteAction(InteractableAction action, GameObject invoker = null)
    {
        var context = new InteractableContext(invoker, this);
        if (action.CanExecute(context)) action.Execute(context);
    }
    #endregion

}
