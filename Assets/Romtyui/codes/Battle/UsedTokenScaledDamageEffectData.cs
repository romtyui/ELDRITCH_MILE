using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Token/Used Token Scaled Damage")]
public class UsedTokenScaledDamageEffectData : CardEffectData, CardDescriptionValueProvider
{
    [Header("Token")]
    [Tooltip("要計算的 Token ID。必須和卵 Token CardData 的 Token Id 一樣")]
    public string tokenId = "egg";

    [Header("Damage Bonus")]
    [Tooltip("每使用過 1 次指定 Token，增加多少額外傷害")]
    public int damagePerUsedToken = 1;

    [Header("Fallback")]
    [Tooltip("如果同一張卡找不到 DamageEffectData，是否使用 fallbackBaseDamage")]
    public bool useFallbackBaseDamageIfMissingSourceEffect = true;

    [Tooltip("找不到 DamageEffectData 時使用的備用基礎傷害")]
    public int fallbackBaseDamage = 0;

    [Header("Description Preview")]
    [Tooltip("描述預覽用。當不在戰鬥中或讀不到 BattleDeck 時，先用這個數值預覽")]
    public int previewUsedTokenCount = 0;

    public override void Execute(CardResolveContext context)
    {
        if (context == null)
            return;

        if (context.source == null)
            return;

        if (context.target == null)
            return;

        if (context.battleManager == null)
            return;

        int bonusDamage = GetBonusDamage(context);

        if (bonusDamage <= 0)
        {
            Debug.Log("[UsedTokenScaledDamageEffectData] 額外傷害為 0，不執行追加傷害");
            return;
        }

        context.source.DealDamageTo(context.target, bonusDamage);

        Debug.Log(
            $"[UsedTokenScaledDamageEffectData] tokenId={tokenId}, " +
            $"used={GetUsedTokenCount(context)}, " +
            $"bonusDamage={bonusDamage}"
        );
    }

    private DamageEffectData GetSourceDamageEffect(CardResolveContext context)
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

            if (effect is DamageEffectData damageEffect)
                return damageEffect;
        }

        return null;
    }

    private int GetBaseDamageFromSourceEffect(CardResolveContext context)
    {
        DamageEffectData sourceDamageEffect = GetSourceDamageEffect(context);

        if (sourceDamageEffect != null)
            return Mathf.Max(0, sourceDamageEffect.amount);

        if (useFallbackBaseDamageIfMissingSourceEffect)
            return Mathf.Max(0, fallbackBaseDamage);

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

    private int GetBonusDamage(CardResolveContext context)
    {
        int usedTokenCount = GetUsedTokenCount(context);
        int bonusPerToken = Mathf.Max(0, damagePerUsedToken);

        return usedTokenCount * bonusPerToken;
    }

    private int GetTotalRawDamage(CardResolveContext context)
    {
        int baseDamage = GetBaseDamageFromSourceEffect(context);
        int bonusDamage = GetBonusDamage(context);

        return baseDamage + bonusDamage;
    }

    private int GetPreviewTotalDamage(CardResolveContext context)
    {
        int damage = GetTotalRawDamage(context);

        if (context != null && context.source != null)
            damage = context.source.ModifyOutgoingDamage(damage);

        if (context != null && context.target != null)
            damage = context.target.ModifyIncomingDamage(damage);

        return Mathf.Max(0, damage);
    }

    private int GetPreviewBonusDamage(CardResolveContext context)
    {
        int damage = GetBonusDamage(context);

        if (context != null && context.source != null)
            damage = context.source.ModifyOutgoingDamage(damage);

        if (context != null && context.target != null)
            damage = context.target.ModifyIncomingDamage(damage);

        return Mathf.Max(0, damage);
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

        if (key == "baseDamage" ||
            key == "baseSwordDamage" ||
            key == "基礎傷害")
        {
            value = GetBaseDamageFromSourceEffect(context);
            return true;
        }

        if (key == "usedTokenBonusDamage" ||
            key == "bonusDamage" ||
            key == "tokenBonusDamage" ||
            key == "額外傷害")
        {
            value = GetPreviewBonusDamage(context);
            return true;
        }

        if (key == "swordDamage" ||
            key == "totalDamage" ||
            key == "finalDamage" ||
            key == "總傷害")
        {
            value = GetPreviewTotalDamage(context);
            return true;
        }

        return false;
    }
}