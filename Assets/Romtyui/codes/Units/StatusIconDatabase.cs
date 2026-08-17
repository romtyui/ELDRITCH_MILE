using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StatusIconEntry
{
    public StatusType statusType;
    public Sprite icon;
}

[CreateAssetMenu(menuName = "CardGame/Status/Status Icon Database")]
public class StatusIconDatabase : ScriptableObject
{
    public List<StatusIconEntry> entries = new();

    public Sprite GetIcon(StatusType statusType)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].statusType == statusType)
                return entries[i].icon;
        }

        return null;
    }
}