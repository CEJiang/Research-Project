using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TriggerObject : MonoBehaviour
{
    private new Collider collider;

    // Attributes
    public string displayName;
    public string description;
    [SerializeField] private List<TriggerAction> actions = new();

    void Awake()
    {
        collider = GetComponent<Collider>();
        collider.isTrigger = true;
    }

#region Trigger
    private void OnTriggerEnter(Collider other)
    {
        var context = new TriggerContext(other.gameObject, this);
        Trigger(context);
    }

    public void Trigger(TriggerContext context)
    {
        foreach (var action in actions)
        {
            if (action.CanExecute(context))
            {
                action.Execute(context);
                return;
            }
        }
    }
#endregion

}
