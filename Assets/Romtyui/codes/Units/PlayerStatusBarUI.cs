using System.Collections.Generic;
using UnityEngine;

public class PlayerStatusBarUI : MonoBehaviour
{
    [Header("References")]
    public BattleUnit playerUnit;
    public StatusIconDatabase iconDatabase;
    public StatusIconUI statusIconPrefab;
    public Transform iconRoot;
    public TooltipKeywordDatabase keywordDatabase;

    [Header("Tooltip Window")]
    [Tooltip("掛在 PlayerStatusBar 根物件上的 TooltipTriggerUI")]
    public TooltipTriggerUI statusTooltipTrigger;

    private readonly List<StatusIconUI> spawnedIcons = new();

    private BattleUnit subscribedPlayerUnit;

    private void Awake()
    {
        if (iconRoot == null)
            iconRoot = transform;

        if (statusTooltipTrigger == null)
            statusTooltipTrigger = GetComponent<TooltipTriggerUI>();
    }

    private void OnEnable()
    {
        SubscribeToPlayerStatusChanged();
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeFromPlayerStatusChanged();
    }

    public void Refresh()
    {
        EnsurePlayerStatusSubscription();
        ClearIcons();

        if (playerUnit == null)
        {
            ClearStatusTooltip();
            Debug.LogWarning("[PlayerStatusBarUI] playerUnit 沒有指定");
            return;
        }

        Dictionary<StatusType, int> statuses = playerUnit.GetAllStatuses();

        RefreshStatusTooltip(statuses);

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

        foreach (KeyValuePair<StatusType, int> pair in statuses)
        {
            StatusType statusType = pair.Key;
            int amount = pair.Value;

            if (amount <= 0)
                continue;

            Sprite icon = iconDatabase.GetIcon(statusType);

            StatusIconUI iconUI = Instantiate(statusIconPrefab, iconRoot);
            iconUI.Set(statusType, icon, amount, keywordDatabase, false);

            spawnedIcons.Add(iconUI);
        }
    }

    private void RefreshStatusTooltip(Dictionary<StatusType, int> statuses)
    {
        if (statusTooltipTrigger == null)
            return;

        List<TooltipEntry> entries = new List<TooltipEntry>();

        if (statuses != null)
        {
            foreach (KeyValuePair<StatusType, int> pair in statuses)
            {
                StatusType statusType = pair.Key;
                int amount = pair.Value;

                if (amount <= 0)
                    continue;

                TooltipEntry entry = StatusIconUI.BuildTooltipEntry(statusType, amount, keywordDatabase);

                if (entry != null)
                    entries.Add(entry);
            }
        }

        statusTooltipTrigger.SetEntries(entries);
    }

    private void ClearStatusTooltip()
    {
        if (statusTooltipTrigger == null)
            return;

        statusTooltipTrigger.SetEntries(new List<TooltipEntry>());
    }

    private void SubscribeToPlayerStatusChanged()
    {
        if (playerUnit == null)
            return;

        if (subscribedPlayerUnit == playerUnit)
            return;

        UnsubscribeFromPlayerStatusChanged();

        subscribedPlayerUnit = playerUnit;
        subscribedPlayerUnit.OnStatusChanged += Refresh;
    }

    private void EnsurePlayerStatusSubscription()
    {
        if (subscribedPlayerUnit == playerUnit)
            return;

        SubscribeToPlayerStatusChanged();
    }

    private void UnsubscribeFromPlayerStatusChanged()
    {
        if (subscribedPlayerUnit == null)
            return;

        subscribedPlayerUnit.OnStatusChanged -= Refresh;
        subscribedPlayerUnit = null;
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