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

    public void Set(StatusType statusType, Sprite icon, int stack, TooltipKeywordDatabase database)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (stackText != null)
        {
            stackText.text = stack > 1 ? stack.ToString() : "";
            stackText.gameObject.SetActive(stack > 1);
        }

        if (tooltipTrigger == null)
            tooltipTrigger = GetComponent<TooltipTriggerUI>();

        if (tooltipTrigger != null)
        {
            List<TooltipEntry> entries = new List<TooltipEntry>();

            string key = statusType.ToString();
            //string title = key;
            //string body = $"目前層數：{stack}";
            string title = GetStatusTitle(statusType);
            string body = GetStatusDescription(statusType, stack);

            tooltipTrigger.SetTooltip(title, body);

            if (database != null && database.TryGet(key, out TooltipKeywordEntry entry))
            {
                title = entry.title;
                body = $"{entry.description}\n\n目前層數：{stack}";
            }

            entries.Add(new TooltipEntry(title, body));
            tooltipTrigger.SetEntries(entries, TooltipAnchorSide.Bottom);
        }

        gameObject.SetActive(true);
    }
    private string GetStatusTitle(StatusType statusType)
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

            default:
                return statusType.ToString();
        }
    }
    private string GetStatusDescription(StatusType statusType, int amount)
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
                return $"每回合開始時，獲得 {amount} 點護盾。";

            default:
                return $"目前層數：{amount}";
        }
    }
}