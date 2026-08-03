using System.Collections.Generic;

[System.Serializable]
public class Zone
{
    public string zoneID;
    public string DisplayNameEn;
    public string DisplayNameCh;
    public List<string> adjacentZoneIDs = new();
}