using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardViewUIExplore : MonoBehaviour
{
    [Header("Image Refs (探索卡牌圖層)")]
    public Image artworkImage;
    public Image cardFrameImage;

    [Header("Text Refs")]
    public TMP_Text nameText;
    public TMP_Text costText;
    public TMP_Text descriptionText;

    [Header("Fallback Visual")]
    public CardVisualDataExplore defaultVisualData;

    public CardInstanceExplore CardInstance { get; private set; }

    [Header("Tooltip (共用戰鬥的系統)")]
    public TooltipTriggerUI tooltipTrigger;
    public TooltipKeywordDatabase tooltipKeywordDatabase;

    [Header("Keyword Highlight")]
    public string keywordColor = "#FFD45A";

    [Header("Tooltip Position")]
    public TooltipAnchorSide cardTooltipSide = TooltipAnchorSide.Top;

    public void Bind(CardInstanceExplore instance)
    {
        CardInstance = instance;

        if (instance == null || instance.data == null)
            return;

        CardDataExplore data = instance.data;

        if (nameText != null)
            nameText.text = data.cardName;

        if (costText != null)
            costText.text = instance.currentCost.ToString();

        CardVisualDataExplore visual = data.visualData != null
            ? data.visualData
            : defaultVisualData;

        ApplyVisual(visual);
        RefreshRuntimeDescription();
    }

    public void RefreshRuntimeDescription()
    {
        if (CardInstance == null || CardInstance.data == null)
            return;

        string description = CardInstance.data.description;
        
        // 如果你需要加入機率提示，可以在這裡動態修改文本
        // 例如：description += $"\n成功率: {CardInstance.data.successProbability * 100}%";

        if (descriptionText != null)
            descriptionText.text = BuildHighlightedDescription(description);

        SetupTooltip(description);
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
            if (entry == null || string.IsNullOrWhiteSpace(entry.keyword))
                continue;

            string coloredKeyword = $"<color={keywordColor}>{entry.keyword}</color>";
            result = result.Replace(entry.keyword, coloredKeyword);
        }

        return result;
    }

    private void SetupTooltip(string runtimeDescription)
    {
        if (tooltipTrigger == null)
            tooltipTrigger = GetComponent<TooltipTriggerUI>();

        if (tooltipTrigger == null || tooltipKeywordDatabase == null)
            return;

        List<TooltipEntry> entries = new List<TooltipEntry>();
        List<TooltipKeywordEntry> foundKeywords = tooltipKeywordDatabase.FindKeywordsInText(runtimeDescription);

        foreach (var keywordEntry in foundKeywords)
        {
            if (keywordEntry == null) continue;

            string title = string.IsNullOrWhiteSpace(keywordEntry.title) ? keywordEntry.keyword : keywordEntry.title;
            string body = keywordEntry.description;

            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(body)) continue;
            
            // 避免重複加入
            if (!entries.Exists(e => e.title == title))
            {
                entries.Add(new TooltipEntry(title, body));
            }
        }

        tooltipTrigger.SetEntries(entries, cardTooltipSide);
    }

    private void ApplyVisual(CardVisualDataExplore visual)
    {
        if (visual == null) return;

        if (artworkImage != null) artworkImage.sprite = visual.artworkSprite;
        if (cardFrameImage != null) cardFrameImage.sprite = visual.cardFrameSprite;
        if (nameText != null) nameText.color = visual.nameTextColor;
        if (descriptionText != null) descriptionText.color = visual.descriptionTextColor;
        if (costText != null) costText.color = visual.costTextColor;
    }
}