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
            string title = key;
            string body = $"目前層數：{stack}";

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
}