using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardViewUI : MonoBehaviour
{
    [Header("Image Refs")]
    public Image artworkImage;
    public Image cardFaceImage;
    public Image cardFrameImage;
    public Image maskImage;

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
    public string damageValueColor = "#FF4A4A";
    public string blockValueColor = "#66CCFF";

    [Header("Tooltip Position")]
    public TooltipAnchorSide cardTooltipSide = TooltipAnchorSide.Top;


    private BattleManager cachedBattleManager;
    private string lastRuntimeDescription;

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

        CardVisualData visual = data.visualData != null
            ? data.visualData
            : defaultVisualData;

        ApplyVisual(visual);

        RefreshRuntimeDescription();
    }

    public void RefreshRuntimeDescription()
    {
        if (CardInstance == null || CardInstance.data == null)
            return;

        lastRuntimeDescription = BuildRuntimeDescription(CardInstance);

        if (descriptionText != null)
            descriptionText.text = BuildHighlightedDescription(lastRuntimeDescription);

        SetupTooltip(CardInstance, lastRuntimeDescription);
    }

    private string BuildRuntimeDescription(CardInstance instance)
    {
        if (instance == null || instance.data == null)
            return "";

        string text = instance.data.description;

        if (string.IsNullOrWhiteSpace(text))
            return "";

        BattleManager battleManager = GetBattleManager();

        BattleUnit source = battleManager != null
            ? battleManager.playerUnit
            : null;

        BattleUnit target = null;

        if (battleManager != null && instance.data.targetType == TargetType.SingleEnemy)
            target = battleManager.currentEnemy;

        CardResolveContext context = new CardResolveContext(
            source,
            target,
            instance,
            battleManager
        );

        return ReplaceDescriptionTokens(text, instance, context);
    }

    private string ReplaceDescriptionTokens(
    string text,
    CardInstance instance,
    CardResolveContext context
)
    {
        if (instance == null || instance.data == null || instance.data.effects == null)
            return text;

        int damageIndex = 0;
        int blockIndex = 0;

        Dictionary<string, string> values = new Dictionary<string, string>();

        for (int i = 0; i < instance.data.effects.Count; i++)
        {
            CardEffectData effect = instance.data.effects[i];

            if (effect == null)
                continue;

            if (effect is DamageEffectData damageEffect)
            {
                int value = CalculateDamagePreview(instance, damageEffect, context);
                string coloredValue = ColorValue(value, damageValueColor);

                if (!values.ContainsKey("damage"))
                    values.Add("damage", coloredValue);

                if (!values.ContainsKey("damege"))
                    values.Add("damege", coloredValue);

                values["damage" + damageIndex] = coloredValue;
                values["damege" + damageIndex] = coloredValue;

                damageIndex++;
            }
            else if (effect is GainBlockEffectData blockEffect)
            {
                int value = CalculateBlockPreview(blockEffect, context);
                string coloredValue = ColorValue(value, blockValueColor);

                if (!values.ContainsKey("block"))
                    values.Add("block", coloredValue);

                values["block" + blockIndex] = coloredValue;

                blockIndex++;
            }
            else if (effect is CardDescriptionValueProvider provider)
            {
                AddProviderValue(values, provider, "damage", context, damageValueColor);
                AddProviderValue(values, provider, "damege", context, damageValueColor);
                AddProviderValue(values, provider, "block", context, blockValueColor);
            }
        }

        return Regex.Replace(text, @"\{([a-zA-Z0-9_]+)\}", match =>
        {
            string key = match.Groups[1].Value;

            if (values.TryGetValue(key, out string value))
                return value;

            return match.Value;
        });
    }

    private void AddProviderValue(
    Dictionary<string, string> values,
    CardDescriptionValueProvider provider,
    string key,
    CardResolveContext context,
    string color
)
    {
        if (provider == null)
            return;

        if (values.ContainsKey(key))
            return;

        if (provider.TryGetDescriptionValue(key, context, out int value))
            values.Add(key, ColorValue(value, color));
    }
    private string ColorValue(int value, string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return value.ToString();

        return $"<color={color}>{value}</color>";
    }

    private int CalculateDamagePreview(
        CardInstance instance,
        DamageEffectData damageEffect,
        CardResolveContext context
    )
    {
        if (damageEffect == null)
            return 0;

        int damage = damageEffect.amount;

        BattleUnit source = context != null ? context.source : null;
        BattleUnit target = context != null ? context.target : null;

        if (source != null)
            damage = source.ModifyOutgoingDamage(damage);

        bool shouldApplyTargetStatus =
            instance != null &&
            instance.data != null &&
            instance.data.targetType == TargetType.SingleEnemy &&
            target != null;

        if (shouldApplyTargetStatus)
            damage = target.ModifyIncomingDamage(damage);

        return Mathf.Max(0, damage);
    }

    private int CalculateBlockPreview(
        GainBlockEffectData blockEffect,
        CardResolveContext context
    )
    {
        if (blockEffect == null)
            return 0;

        int block = blockEffect.amount;

        BattleUnit source = context != null ? context.source : null;

        if (source != null)
            block = source.ModifyBlockGain(block);

        return Mathf.Max(0, block);
    }

    private BattleManager GetBattleManager()
    {
        if (cachedBattleManager == null)
            cachedBattleManager = FindFirstObjectByType<BattleManager>();

        return cachedBattleManager;
    }

    private string BuildHighlightedDescription(string originalDescription)
    {
        if (string.IsNullOrWhiteSpace(originalDescription))
            return "";

        if (tooltipKeywordDatabase == null)
            return originalDescription;

        string result = originalDescription;

        List<TooltipKeywordEntry> foundKeywords =
            tooltipKeywordDatabase.FindKeywordsInText(originalDescription);

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

    private void SetupTooltip(CardInstance card, string runtimeDescription)
    {
        if (tooltipTrigger == null)
            tooltipTrigger = GetComponent<TooltipTriggerUI>();

        if (tooltipTrigger == null)
            return;

        List<TooltipEntry> entries = new List<TooltipEntry>();

        AddKeywordTooltipEntries(entries, runtimeDescription);

        tooltipTrigger.SetEntries(entries, cardTooltipSide);
    }

    private void AddKeywordTooltipEntries(List<TooltipEntry> results, string description)
    {
        if (results == null)
            return;

        if (tooltipKeywordDatabase == null)
            return;

        if (string.IsNullOrWhiteSpace(description))
            return;

        List<TooltipKeywordEntry> foundKeywords =
            tooltipKeywordDatabase.FindKeywordsInText(description);

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

            bool alreadyAdded = false;

            for (int j = 0; j < results.Count; j++)
            {
                if (results[j].title == title)
                {
                    alreadyAdded = true;
                    break;
                }
            }

            if (alreadyAdded)
                continue;

            results.Add(new TooltipEntry(title, body));
        }
    }

    public void SetTooltipSide(TooltipAnchorSide side)
    {
        cardTooltipSide = side;

        if (tooltipTrigger != null)
            tooltipTrigger.preferredSide = side;
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