using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Token/Used Token Scaled Block")]
public class UsedTokenScaledBlockEffectData : CardEffectData, CardDescriptionValueProvider
{
    [Header("Token")]
    [Tooltip("要計算的 Token ID。必須和卵 Token CardData 的 Token Id 一樣")]
    public string tokenId = "egg";

    [Header("Block Bonus")]
    [Tooltip("每使用過 1 次指定 Token，增加多少額外護盾")]
    public int blockPerUsedToken = 1;

    [Header("Fallback")]
    [Tooltip("如果同一張卡找不到 GainBlockEffectData，是否使用 fallbackBaseBlock")]
    public bool useFallbackBaseBlockIfMissingSourceEffect = true;

    [Tooltip("找不到 GainBlockEffectData 時使用的備用基礎護盾")]
    public int fallbackBaseBlock = 0;

    [Header("Description Preview")]
    [Tooltip("描述預覽用。當不在戰鬥中或讀不到 BattleDeck 時，先用這個數值預覽")]
    public int previewUsedTokenCount = 0;

    public override void Execute(CardResolveContext context)
    {
        if (context == null)
            return;

        if (context.source == null)
            return;

        if (context.battleManager == null)
            return;

        int bonusBlock = GetBonusBlock(context);

        if (bonusBlock <= 0)
        {
            Debug.Log("[UsedTokenScaledBlockEffectData] 額外護盾為 0，不執行追加護盾");
            return;
        }

        context.source.GainBlock(bonusBlock);

        Debug.Log(
            $"[UsedTokenScaledBlockEffectData] tokenId={tokenId}, " +
            $"used={GetUsedTokenCount(context)}, " +
            $"bonusBlock={bonusBlock}"
        );
    }

    private GainBlockEffectData GetSourceBlockEffect(CardResolveContext context)
    {
        if (context == null)
            return null;

        if (context.card == null)
            return null;

        if (context.card.data == null)
            return null;

        if (context.card.data.effects == null)
            return null;

        for (int i = 0; i < context.card.data.effects.Count; i++)
        {
            CardEffectData effect = context.card.data.effects[i];

            if (effect == null)
                continue;

            if (effect == this)
                continue;

            if (effect is GainBlockEffectData blockEffect)
                return blockEffect;
        }

        return null;
    }

    private int GetBaseBlockFromSourceEffect(CardResolveContext context)
    {
        GainBlockEffectData sourceBlockEffect = GetSourceBlockEffect(context);

        if (sourceBlockEffect != null)
            return Mathf.Max(0, sourceBlockEffect.amount);

        if (useFallbackBaseBlockIfMissingSourceEffect)
            return Mathf.Max(0, fallbackBaseBlock);

        return 0;
    }

    private int GetUsedTokenCount(CardResolveContext context)
    {
        if (context == null)
            return Mathf.Max(0, previewUsedTokenCount);

        if (context.battleManager == null)
            return Mathf.Max(0, previewUsedTokenCount);

        return context.battleManager.GetUsedTokenCount(tokenId);
    }

    private int GetBonusBlock(CardResolveContext context)
    {
        int usedTokenCount = GetUsedTokenCount(context);
        int bonusPerToken = Mathf.Max(0, blockPerUsedToken);

        return usedTokenCount * bonusPerToken;
    }

    private int GetTotalRawBlock(CardResolveContext context)
    {
        int baseBlock = GetBaseBlockFromSourceEffect(context);
        int bonusBlock = GetBonusBlock(context);

        return baseBlock + bonusBlock;
    }

    private int GetPreviewTotalBlock(CardResolveContext context)
    {
        int block = GetTotalRawBlock(context);

        if (context != null && context.source != null)
            block = context.source.ModifyBlockGain(block);

        return Mathf.Max(0, block);
    }

    private int GetPreviewBonusBlock(CardResolveContext context)
    {
        int block = GetBonusBlock(context);

        if (context != null && context.source != null)
            block = context.source.ModifyBlockGain(block);

        return Mathf.Max(0, block);
    }

    public bool TryGetDescriptionValue(string key, CardResolveContext context, out int value)
    {
        value = 0;

        if (key == "usedEggCount" ||
            key == "usedTokenCount" ||
            key == "playedTokenCount" ||
            key == "eggCount" ||
            key == "已使用過卵的次數")
        {
            value = GetUsedTokenCount(context);
            return true;
        }

        if (key == "baseBlock" ||
            key == "baseShieldBlock" ||
            key == "基礎護盾")
        {
            value = GetBaseBlockFromSourceEffect(context);
            return true;
        }

        if (key == "usedTokenBonusBlock" ||
            key == "bonusBlock" ||
            key == "tokenBonusBlock" ||
            key == "額外護盾")
        {
            value = GetPreviewBonusBlock(context);
            return true;
        }

        if (key == "shieldBlock" ||
            key == "totalBlock" ||
            key == "finalBlock" ||
            key == "總護盾")
        {
            value = GetPreviewTotalBlock(context);
            return true;
        }

        return false;
    }
}