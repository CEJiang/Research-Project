using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ZoneTriggerAutoAssigner : MonoBehaviour
{
    public Zone zoneData;

#if UNITY_EDITOR
    [ContextMenu("Attach ZoneTrigger Scripts")]
    public void AttachZoneTriggers()
    {
        if (Application.isPlaying) return;

        if (zoneData == null)
        {
            Debug.LogError($"Zone data not found for {name} in Resources/Zones/");
            return;
        }

        List<Collider> colliders = new(GetComponentsInChildren<Collider>());
        foreach (var collider in colliders)
        {
            collider.isTrigger = true;
            ZoneTrigger zoneTrigger = collider.GetComponent<ZoneTrigger>() ?? collider.gameObject.AddComponent<ZoneTrigger>();
            zoneTrigger.zoneData = zoneData;
            EditorUtility.SetDirty(zoneTrigger);
        }

        EditorUtility.SetDirty(this);
        Debug.Log($"Attached ZoneTrigger scripts to {colliders.Count} colliders under {name}");
    }

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        if (zoneData == null) return;

        var ZoneTriggers = GetComponentsInChildren<ZoneTrigger>(true);
        foreach (var tz in ZoneTriggers)
        {
            tz.zoneData = zoneData;
            EditorUtility.SetDirty(tz);
        }
    }
#endif
}
