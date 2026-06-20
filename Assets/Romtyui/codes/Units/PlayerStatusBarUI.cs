using System.Collections.Generic;
using UnityEngine;

public class PlayerStatusBarUI : MonoBehaviour
{
    [Header("References")]
    public BattleUnit playerUnit;
    public StatusIconDatabase iconDatabase;
    public StatusIconUI statusIconPrefab;
    public Transform iconRoot;

    private readonly List<StatusIconUI> spawnedIcons = new();

    private void Awake()
    {
        if (iconRoot == null)
            iconRoot = transform;
    }

    private void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        ClearIcons();

        if (playerUnit == null)
        {
            Debug.LogWarning("[PlayerStatusBarUI] playerUnit 沒有指定");
            return;
        }

        if (iconDatabase == null)
        {
            Debug.LogWarning("[PlayerStatusBarUI] iconDatabase 沒有指定");
            return;
        }

        if (statusIconPrefab == null)
        {
            Debug.LogWarning("[PlayerStatusBarUI] statusIconPrefab 沒有指定");
            return;
        }

        Dictionary<StatusType, int> statuses = playerUnit.GetAllStatuses();

        foreach (var pair in statuses)
        {
            StatusType statusType = pair.Key;
            int amount = pair.Value;

            if (amount <= 0)
                continue;

            Sprite icon = iconDatabase.GetIcon(statusType);

            StatusIconUI iconUI = Instantiate(statusIconPrefab, iconRoot);
            iconUI.Set(icon, amount);

            spawnedIcons.Add(iconUI);
        }
    }

    private void ClearIcons()
    {
        for (int i = 0; i < spawnedIcons.Count; i++)
        {
            if (spawnedIcons[i] != null)
                Destroy(spawnedIcons[i].gameObject);
        }

        spawnedIcons.Clear();
    }
}