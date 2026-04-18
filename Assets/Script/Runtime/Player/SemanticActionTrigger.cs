using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SemanticActionTrigger : MonoBehaviour
{
    #region Nearby and Faraway Actions
    void OnTriggerEnter(Collider other)
    {
        var semanticObject = other.gameObject.GetComponent<SemanticActionObject>();
        if (semanticObject != null)
        {
            Debug.Log($"Triggered semantic action object: {semanticObject.displayNameEn}");
            // Here you can add code to handle the semantic action, e.g., notify a manager or execute an action.
        }
    }
    void OnTriggerExit(Collider other)
    {
        var semanticObject = other.gameObject.GetComponent<SemanticActionObject>();
        if (semanticObject != null)
        {
            Debug.Log($"Exited semantic action object: {semanticObject.displayNameEn}");
            // Here you can add code to handle the exit event if needed.
        }
    }
    #endregion
}
