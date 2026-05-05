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
        DialogueManager.Instance.ShowCurrentZoneInfo();
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

            DialogueManager.Instance.ShowCurrentZoneInfo();
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
            DialogueManager.Instance.ShowCurrentZoneInfo();
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

    public string GetZoneDisplayNameForLLM(Zone zone)
    {
        switch (zone)
        {
            case Zone.GuestRoom: return "Guest Room";
            case Zone.DiningRoom: return "Dining Room";
            case Zone.StudySideRoom: return "Study Side Room";
            case Zone.StudyRoom: return "Study Room";
            case Zone.ChildRoom: return "Child Room";
            case Zone.MasterRoom: return "Master Room";
            case Zone.StorageRoom: return "Storage Room";
            default: return zone.ToString();
        }
    }

    public string GetZoneDisplayNameForUI(Zone zone)
    {
        if (LocalizationManager.Instance.GetCurrentLanguage() == Language.Chinese)
        {
            switch (zone)
            {
                case Zone.Street: return "街道";
                case Zone.Yard: return "庭院";
                case Zone.Lobby: return "大廳";
                case Zone.GuestRoom: return "客房";
                case Zone.DiningRoom: return "飯廳";
                case Zone.StudySideRoom: return "書房側室";
                case Zone.StudyRoom: return "書房";
                case Zone.ChildRoom: return "兒童房";
                case Zone.MasterRoom: return "主臥室";
                case Zone.StorageRoom: return "儲藏室";
                case Zone.Stairwell: return "樓梯間";
                case Zone.Aisle: return "走道";
                default: return zone.ToString();
            }
        }
        else return GetZoneDisplayNameForLLM(zone); // for English, we use the same display name for both LLM and UI, so we can just call GetZoneDisplayNameForLLM to avoid duplication
    }
    #endregion
}
