using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardViewUI : MonoBehaviour
{
    [Header("Image Refs")]
    public Image artworkImage;      // 武器層
    public Image cardFaceImage;     // 卡面層
    public Image cardFrameImage;    // 卡框層
    public Image maskImage;         // 蒙版

    [Header("Text Refs")]
    public TMP_Text nameText;
    public TMP_Text costText;
    public TMP_Text descriptionText;

    [Header("Fallback Visual")]
    public CardVisualData defaultVisualData;



    public CardInstance CardInstance { get; private set; }
    [Header("Tooltip")]
    public TooltipTriggerUI tooltipTrigger;
    public TooltipKeywordDatabase tooltipKeywordDatabase;

    [Header("Keyword Highlight")]
    public string keywordColor = "#FFD45A";

    [Header("Tooltip Position")]
    public TooltipAnchorSide cardTooltipSide = TooltipAnchorSide.Top;

    public void Bind(CardInstance instance)
    {
        CardInstance = instance;

        if (instance == null || instance.data == null)
            return;

        CardData data = instance.data;

        if (nameText != null)
            nameText.text = data.cardName;

        if (costText != null)
            costText.text = instance.currentCost.ToString();

        if (descriptionText != null)
            //descriptionText.text = data.description;
            descriptionText.text = BuildHighlightedDescription(instance.data.description);

        CardVisualData visual = data.visualData != null
            ? data.visualData
            : defaultVisualData;

        ApplyVisual(visual);
        if (tooltipTrigger == null)
            tooltipTrigger = GetComponent<TooltipTriggerUI>();

        if (tooltipTrigger != null && instance != null && instance.data != null)
        {
            List<TooltipEntry> entries = new List<TooltipEntry>();

            entries.Add(new TooltipEntry(
                instance.data.cardName,
                $"消耗 {instance.currentCost}\n{instance.data.description}"
            ));

            if (instance.data.isGodCard)
                entries.Add(new TooltipEntry("神牌", "打出後會觸發污染或變化效果。"));

            if (instance.data.retain)
                entries.Add(new TooltipEntry("保留", "回合結束時不會被棄掉。"));

            if (instance.data.exhaust)
                entries.Add(new TooltipEntry("消耗", "打出後本場戰鬥暫時移除。"));

            tooltipTrigger.SetEntries(entries, TooltipAnchorSide.Top);
        }
        SetupKeywordTooltip(instance);
    }
    private string BuildHighlightedDescription(string originalDescription)
    {
        if (string.IsNullOrWhiteSpace(originalDescription))
            return "";

        if (tooltipKeywordDatabase == null)
            return originalDescription;

        string result = originalDescription;

        List<TooltipKeywordEntry> foundKeywords = tooltipKeywordDatabase.FindKeywordsInText(originalDescription);

        for (int i = 0; i < foundKeywords.Count; i++)
        {
            TooltipKeywordEntry entry = foundKeywords[i];

            if (entry == null)
                continue;

            if (string.IsNullOrWhiteSpace(entry.keyword))
                continue;

            string coloredKeyword = $"<color={keywordColor}>{entry.keyword}</color>";
            result = result.Replace(entry.keyword, coloredKeyword);
        }

        return result;
    }
    private void SetupKeywordTooltip(CardInstance card)
    {
        if (tooltipTrigger == null)
            tooltipTrigger = GetComponent<TooltipTriggerUI>();

        if (tooltipTrigger == null)
            return;

        List<TooltipEntry> tooltipEntries = BuildCardKeywordTooltipEntries(card);

        tooltipTrigger.SetEntries(tooltipEntries, cardTooltipSide);
    }
    public void SetTooltipSide(TooltipAnchorSide side)
    {
        cardTooltipSide = side;

        if (tooltipTrigger != null)
            tooltipTrigger.preferredSide = side;
    }
    private List<TooltipEntry> BuildCardKeywordTooltipEntries(CardInstance card)
    {
        List<TooltipEntry> results = new();

        if (card == null || card.data == null)
            return results;

        if (tooltipKeywordDatabase == null)
            return results;

        string description = card.data.description;

        List<TooltipKeywordEntry> foundKeywords = tooltipKeywordDatabase.FindKeywordsInText(description);

        for (int i = 0; i < foundKeywords.Count; i++)
        {
            TooltipKeywordEntry keywordEntry = foundKeywords[i];

            if (keywordEntry == null)
                continue;

            string title = string.IsNullOrWhiteSpace(keywordEntry.title)
                ? keywordEntry.keyword
                : keywordEntry.title;

            string body = keywordEntry.description;

            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(body))
                continue;

            results.Add(new TooltipEntry(title, body));
        }

        return results;
    }
    private string BuildCardKeywordText(CardData data)
    {
        if (data == null)
            return "";

        string result = "";

        if (data.retain)
            result += "保留：回合結束時不會被棄掉。\n";

        if (data.exhaust)
            result += "消耗：打出後本場戰鬥暫時移除。\n";

        if (data.ethereal)
            result += "虛無：如果回合結束仍在手牌中，會被消耗。\n";

        if (data.isToken)
            result += "Token：由效果生成的特殊牌。\n";

        if (data.isGodCard)
            result += "神牌：打出後會觸發特殊污染或變化效果。\n";

        return result;
    }
    private void ApplyVisual(CardVisualData visual)
    {
        if (visual == null)
        {
            Debug.LogWarning($"[{nameof(CardViewUI)}] CardData 沒有 visualData，CardViewUI 也沒有 defaultVisualData");
            return;
        }

        if (artworkImage != null)
            artworkImage.sprite = visual.artworkSprite;

        if (cardFaceImage != null)
            cardFaceImage.sprite = visual.cardFaceSprite;

        if (cardFrameImage != null)
            cardFrameImage.sprite = visual.cardFrameSprite;

        if (maskImage != null)
            maskImage.sprite = visual.maskSprite;

        if (nameText != null)
            nameText.color = visual.nameTextColor;

        if (descriptionText != null)
            descriptionText.color = visual.descriptionTextColor;

        if (costText != null)
            costText.color = visual.costTextColor;
    }
}