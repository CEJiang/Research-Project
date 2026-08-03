using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneTrigger : MonoBehaviour
{
    public string zoneID;
    private Zone zoneData;

    protected virtual void Awake()
    {
        if (string.IsNullOrEmpty(zoneID))
        {
            Debug.LogError("ZoneTrigger: zoneID is not set.");
            return;
        }

        zoneData = SceneSpatialContextDataLoader.Instance.zones.Find(z => z.zoneID == zoneID);

        if (zoneData == null)
        {
            Debug.LogError($"ZoneTrigger: No zone data found for zoneID '{zoneID}'.");
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ZoneManager.Instance.EntryZone(zoneData);
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ZoneManager.Instance.ExitZone(zoneData);
        }
    }
}