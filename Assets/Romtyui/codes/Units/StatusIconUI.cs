using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatusIconUI : MonoBehaviour
{
    [Header("UI")]
    public Image iconImage;
    public TMP_Text stackText;
    public TooltipTriggerUI tooltipTrigger;
    public TooltipKeywordDatabase keywordDatabase;
    [Header("Stack Text Display")]
    public bool showNumberWhenOne = true;

    public void Set(StatusType statusType, Sprite icon, int stack, TooltipKeywordDatabase database)
    {
        Set(statusType, icon, stack, database, true);
    }

    public void Set(StatusType statusType, Sprite icon, int stack, TooltipKeywordDatabase database, bool useIndividualTooltip)
    {
        keywordDatabase = database;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (stackText != null)
        {
            bool shouldShowStackText = stack > 1 || stack == 1 && showNumberWhenOne;

            stackText.text = shouldShowStackText ? stack.ToString() : "";
            stackText.gameObject.SetActive(shouldShowStackText);
        }

        if (tooltipTrigger == null)
            tooltipTrigger = GetComponent<TooltipTriggerUI>();

        SetupIndividualTooltip(statusType, stack, database, useIndividualTooltip);

        gameObject.SetActive(true);
    }

    private void SetupIndividualTooltip(StatusType statusType, int stack, TooltipKeywordDatabase database, bool useIndividualTooltip)
    {
        if (tooltipTrigger == null)
            return;

        if (!useIndividualTooltip)
        {
            tooltipTrigger.SetEntries(new List<TooltipEntry>());
            tooltipTrigger.enabled = false;
            return;
        }

        tooltipTrigger.enabled = true;

        TooltipEntry entry = BuildTooltipEntry(statusType, stack, database);
        List<TooltipEntry> entries = new List<TooltipEntry>();

        if (entry != null)
            entries.Add(entry);

        tooltipTrigger.SetEntries(entries);
    }

    public static TooltipEntry BuildTooltipEntry(StatusType statusType, int amount, TooltipKeywordDatabase database)
    {
        string key = statusType.ToString();
        string title = GetStatusTitle(statusType);
        string body = GetStatusDescription(statusType, amount);

        if (database != null && database.TryGet(key, out TooltipKeywordEntry databaseEntry))
        {
            title = string.IsNullOrWhiteSpace(databaseEntry.title) ? title : databaseEntry.title;

            if (!string.IsNullOrWhiteSpace(databaseEntry.description))
                body = $"{databaseEntry.description}\n\n目前層數：{amount}";
        }

        return new TooltipEntry(title, body);
    }

    public static string GetStatusTitle(StatusType statusType)
    {
        switch (statusType)
        {
            case StatusType.Strength:
                return "力量";

            case StatusType.TemporaryStrength:
                return "臨時力量";

            case StatusType.Weak:
                return "虛弱";

            case StatusType.Vulnerable:
                return "易傷";

            case StatusType.Frail:
                return "脆弱";

            case StatusType.Poison:
                return "中毒";

            case StatusType.Harden:
                return "硬化";

            case StatusType.Regeneration:
                return "再生";

            case StatusType.Counter:
                return "反擊";

            default:
                return statusType.ToString();
        }
    }

    public static string GetStatusDescription(StatusType statusType, int amount)
    {
        switch (statusType)
        {
            case StatusType.Strength:
                return $"造成的攻擊傷害增加 {amount} 點。";

            case StatusType.TemporaryStrength:
                return $"本回合造成的攻擊傷害增加 {amount} 點，回合結束後移除。";

            case StatusType.Weak:
                return $"造成的傷害降低。目前剩餘 {amount} 層。";

            case StatusType.Vulnerable:
                return $"受到的傷害增加。目前剩餘 {amount} 層。";

            case StatusType.Frail:
                return $"獲得的格擋降低。目前剩餘 {amount} 層。";

            case StatusType.Poison:
                return $"回合開始時受到 {amount} 點傷害，之後中毒層數減少。";

            case StatusType.Harden:
                return $"每回合開始時，獲得 {amount} 點格擋。";

            case StatusType.Regeneration:
                int percent = amount * 5;
                return $"每回合開始時，恢復自身最大生命值 {percent}% 的生命。受到生命傷害時，層數減少 1。";

            case StatusType.Counter:
                return $"受到攻擊時，對攻擊者造成 {amount} 點反擊傷害。";

            default:
                return $"目前層數：{amount}";
        }
    }
}