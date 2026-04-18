using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


public class ZoneManager : Singleton<ZoneManager>
{
    public Zone prevZone;
    public Zone currentZone;
    public Zone defaultZone;

    private Dictionary<Zone, int> zonesInside = new();

    void Start()
    {
        currentZone = defaultZone;
        DialogueManager.Instance.ShowCurrentZoneInfo(currentZone);
    }

    public void EntryZone(Zone zone)
    {
        // reference counting
        if (!zonesInside.ContainsKey(zone))
            zonesInside[zone] = 0;

        zonesInside[zone]++;

        // only act when zone changes
        if (currentZone != zone)
        {
            prevZone = currentZone;
            currentZone = zone;

            if (prevZone == defaultZone)
                RecordZoneEntryAsync(currentZone);
            else
                RecordZoneTransition(prevZone, currentZone);

            DialogueManager.Instance.ShowCurrentZoneInfo(currentZone);
        }
}

    public void ExitZone(Zone zone)
    {
        StartCoroutine(RemoveZoneAfterDelay(zone, 0.5f));
    }

    IEnumerator RemoveZoneAfterDelay(Zone zone, float delay)
    {
        yield return new WaitForSeconds(delay);

         // decrement ref count
        if (zonesInside.ContainsKey(zone))
        {
            zonesInside[zone]--;
            if (zonesInside[zone] <= 0)
                _ = zonesInside.Remove(zone);
        }

        // only exit if leaving the CURRENT zone
        if (zonesInside.Count == 0)
        {
            RecordZoneExit(currentZone);
            currentZone = defaultZone;
            DialogueManager.Instance.ShowCurrentZoneInfo(currentZone);
        }
    }

    public void RecordZoneEntryAsync(Zone zone)
    {
        _ = SemanticActionManager.Instance.LogSemanticAction(
            new SemanticAction(
                Time.time,
                ActionType.PlayerEnvironment,
                SemanticActionTemplates.EntryZone(zone)
            )
        );
    }

    public void RecordZoneExit(Zone zone)
    {
        _ = SemanticActionManager.Instance.LogSemanticAction(
            new SemanticAction(
                Time.time,
                ActionType.PlayerEnvironment,
                SemanticActionTemplates.ExitZone(zone)
            )
        );
    }

    public void RecordZoneTransition(Zone fromZone, Zone toZone)
    {
        _ = SemanticActionManager.Instance.LogSemanticAction(
            new SemanticAction(
                Time.time,
                ActionType.PlayerEnvironment,
                SemanticActionTemplates.TransitionZone(fromZone, toZone)
            )
        );
    }

    #region Getters
    public Zone GetCurrentZone()
    {
        return currentZone;
    }
    #endregion
}
