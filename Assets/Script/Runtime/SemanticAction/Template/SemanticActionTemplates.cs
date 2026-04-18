using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SemanticActionTemplates
{
    #region Zone Actions
    public static string EntryZone(Zone zone) =>
        $"The player has entered the zone: {zone.zoneName}.";

    public static string ExitZone(Zone zone) =>
        $"The player has exited the zone: {zone.zoneName}.";
    
    public static string TransitionZone(Zone prevZone, Zone currentZone) =>
        $"The player has moved from zone: {prevZone.zoneName} to zone: {currentZone.zoneName}.";
    #endregion

    #region Object Actions
    public static string RecordObjectAction(ObjectActionType type, GameObject obj)
    {
        string verb = type switch
        {
            ObjectActionType.Interact => "interacted",
            ObjectActionType.PickUp  => "picked up",
            ObjectActionType.Drop    => "dropped",
            ObjectActionType.Use     => "used",
            ObjectActionType.Examine => "examined",
            ObjectActionType.Open    => "opened",
            ObjectActionType.Close   => "closed",
            ObjectActionType.Scan    => "scanned",
            ObjectActionType.Nearby  => "moved near",
            ObjectActionType.Faraway => "moved away from",
            _ => "acted on"
        };

        return $"The player has {verb} the object: {obj.name}.";
    }

    #endregion

}
