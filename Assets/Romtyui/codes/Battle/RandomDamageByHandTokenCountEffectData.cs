using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Random Damage By Used Token Count")]
public class RandomDamageByHandTokenCountEffectData : CardEffectData, CardDescriptionValueProvider
{
    [Header("Token")]
    [Tooltip("要計算的 Token ID。必須和卵 Token CardData 的 Token Id 一樣")]
    public string tokenId = "egg";

    [Header("Extra Hit Settings")]
    [Tooltip("每使用過 1 次指定 Token，追加幾次攻擊")]
    public int extraHitPerUsedToken = 1;

    [Tooltip("如果找不到 RandomEnemyMultiHitDamageEffectData，是否使用 fallbackDamagePerHit")]
    public bool useFallbackDamageIfMissingSourceEffect = true;

    [Tooltip("找不到 RandomEnemyMultiHitDamageEffectData 時使用的備用傷害")]
    public int fallbackDamagePerHit = 3;

    [Tooltip("找不到 RandomEnemyMultiHitDamageEffectData 時使用的備用保底次數")]
    public int fallbackBaseHitCount = 3;

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

        int extraHitCount = GetExtraHitCount(context);

        if (extraHitCount <= 0)
        {
            Debug.Log("[RandomDamageByHandTokenCountEffectData] 追加攻擊次數為 0，不執行追加傷害");
            return;
        }

        int damagePerHit = GetDamagePerHitFromSourceEffect(context);

        for (int i = 0; i < extraHitCount; i++)
        {
            BattleUnit randomTarget = context.battleManager.GetRandomAliveEnemyPublic();

            if (randomTarget == null)
            {
                Debug.Log("[RandomDamageByHandTokenCountEffectData] 沒有可攻擊的敵人");
                return;
            }

            context.source.DealDamageTo(randomTarget, damagePerHit);

            Debug.Log(
                $"[RandomDamageByHandTokenCountEffectData] 追加第 {i + 1} 次命中 {randomTarget.unitName}，基礎傷害 {damagePerHit}"
            );
        }
    }

    private RandomEnemyMultiHitDamageEffectData GetSourceMultiHitEffect(CardResolveContext context)
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

            if (effect is RandomEnemyMultiHitDamageEffectData multiHitEffect)
                return multiHitEffect;
        }

        return null;
    }

    private int GetBaseHitCountFromSourceEffect(CardResolveContext context)
    {
        RandomEnemyMultiHitDamageEffectData sourceEffect = GetSourceMultiHitEffect(context);

        if (sourceEffect != null)
            return Mathf.Max(0, sourceEffect.hitCount);

        return Mathf.Max(0, fallbackBaseHitCount);
    }

    private int GetDamagePerHitFromSourceEffect(CardResolveContext context)
    {
        RandomEnemyMultiHitDamageEffectData sourceEffect = GetSourceMultiHitEffect(context);

        if (sourceEffect != null)
            return Mathf.Max(0, sourceEffect.damagePerHit);

        if (useFallbackDamageIfMissingSourceEffect)
            return Mathf.Max(0, fallbackDamagePerHit);

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

    private int GetExtraHitCount(CardResolveContext context)
    {
        int usedTokenCount = GetUsedTokenCount(context);
        int extraPerToken = Mathf.Max(0, extraHitPerUsedToken);

        return usedTokenCount * extraPerToken;
    }

    private int GetTotalHitCount(CardResolveContext context)
    {
        int baseHitCount = GetBaseHitCountFromSourceEffect(context);
        int extraHitCount = GetExtraHitCount(context);

        return baseHitCount + extraHitCount;
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

        if (key == "extraHitCount" ||
            key == "extraCount" ||
            key == "追加次數")
        {
            value = GetExtraHitCount(context);
            return true;
        }

        if (key == "baseHitCount" ||
            key == "baseCount" ||
            key == "保底次數")
        {
            value = GetBaseHitCountFromSourceEffect(context);
            return true;
        }

        if (key == "bowHitCount" ||
            key == "totalHitCount" ||
            key == "repeatCount" ||
            key == "重複次數")
        {
            value = GetTotalHitCount(context);
            return true;
        }

        return false;
    }
}