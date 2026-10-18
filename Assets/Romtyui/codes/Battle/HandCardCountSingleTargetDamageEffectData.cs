using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Hand/Hand Card Count Single Target Damage")]
public class HandCardCountSingleTargetDamageEffectData : CardEffectData, CardDescriptionValueProvider
{
    [Header("Target Token")]
    [UnityEngine.Serialization.FormerlySerializedAs("targetCardId")]
    [Tooltip("只計算 Is Token 已勾選，且 CardData.tokenId 符合此值的手牌")]
    public string targetTokenId = "goat_token";

    [Header("Description Preview")]
    [Min(0)]
    [Tooltip("無法取得手牌資料時，描述預覽使用的張數，不影響實際戰鬥計算")]
    public int previewHandCardCount = 0;

    public override void Execute(CardResolveContext context)
    {
        if (context == null || context.source == null) return;

        if (context.battleManager == null || context.battleManager.playerDeck == null)
        {
            Debug.LogWarning("[HandCardCountSingleTargetDamageEffectData] 無法取得手牌資料");
            return;
        }

        RandomEnemyMultiHitDamageEffectData singleTargetEffect = GetSourceDamageEffect(context);
        RandomTargetEachHitDamageEffectData randomTargetEffect = GetRandomSourceDamageEffect(context);

        if (singleTargetEffect == null && randomTargetEffect == null)
        {
            Debug.LogWarning("[HandCardCountSingleTargetDamageEffectData] 同張卡的 effects 找不到可配合的連擊傷害效果");
            return;
        }

        bool randomizeEachHit = singleTargetEffect == null;

        if (!randomizeEachHit && context.target == null)
        {
            Debug.LogWarning("[HandCardCountSingleTargetDamageEffectData] context.target 是 null，無法對同一目標攻擊");
            return;
        }

        int finalHitCount = GetMatchingHandCardCount(context, true);
        int damagePerHit = singleTargetEffect != null ? singleTargetEffect.damagePerHit : randomTargetEffect.damagePerHit;
        damagePerHit = Mathf.Max(0, damagePerHit);

        Debug.Log($"[HandCardCountSingleTargetDamageEffectData] Token ID：{targetTokenId}，攻擊次數：{finalHitCount}，每下基礎傷害：{damagePerHit}，每下隨機：{randomizeEachHit}");

        for (int i = 0; i < finalHitCount; i++)
        {
            BattleUnit target = context.target;

            if (randomizeEachHit) target = context.battleManager.GetRandomAliveEnemyPublic();

            if (target == null)
            {
                Debug.Log("[HandCardCountSingleTargetDamageEffectData] 沒有可攻擊的目標，停止後續攻擊");
                return;
            }

            if (target.currentHp <= 0)
            {
                if (randomizeEachHit) continue;

                Debug.Log("[HandCardCountSingleTargetDamageEffectData] 目標已死亡，停止後續攻擊");
                return;
            }

            context.source.DealDamageTo(target, damagePerHit);

            Debug.Log($"[HandCardCountSingleTargetDamageEffectData] 第 {i + 1}/{finalHitCount} 下命中 {target.unitName}，基礎傷害 {damagePerHit}");
        }
    }

    private RandomEnemyMultiHitDamageEffectData GetSourceDamageEffect(CardResolveContext context)
    {
        if (context == null || context.card == null || context.card.data == null) return null;
        if (context.card.data.effects == null) return null;

        for (int i = 0; i < context.card.data.effects.Count; i++)
        {
            if (context.card.data.effects[i] is RandomEnemyMultiHitDamageEffectData sourceEffect) return sourceEffect;
        }

        return null;
    }

    private RandomTargetEachHitDamageEffectData GetRandomSourceDamageEffect(CardResolveContext context)
    {
        if (context == null || context.card == null || context.card.data == null) return null;
        if (context.card.data.effects == null) return null;

        for (int i = 0; i < context.card.data.effects.Count; i++)
        {
            if (context.card.data.effects[i] is RandomTargetEachHitDamageEffectData sourceEffect) return sourceEffect;
        }

        return null;
    }

    private int GetMatchingHandCardCount(CardResolveContext context, bool includePlayedCard = false)
    {
        if (string.IsNullOrWhiteSpace(targetTokenId)) return 0;
        if (context == null || context.battleManager == null || context.battleManager.playerDeck == null) return Mathf.Max(0, previewHandCardCount);

        var hand = context.battleManager.playerDeck.Hand;

        if (hand == null) return Mathf.Max(0, previewHandCardCount);

        string requiredTokenId = targetTokenId.Trim();
        int count = 0;
        bool playedCardIsInHand = false;

        for (int i = 0; i < hand.Count; i++)
        {
            CardInstance card = hand[i];

            if (card == context.card) playedCardIsInHand = true;
            if (card == null || card.data == null) continue;
            if (!card.data.isToken) continue;
            if (!string.Equals(card.data.tokenId, requiredTokenId, System.StringComparison.Ordinal)) continue;

            count++;
        }

        if (includePlayedCard && !playedCardIsInHand)
        {
            CardInstance playedCard = context.card;

            if (playedCard != null && playedCard.data != null && playedCard.data.isToken && string.Equals(playedCard.data.tokenId, requiredTokenId, System.StringComparison.Ordinal)) count++;
        }

        return count;
    }

    private int GetPreviewDamage(CardResolveContext context)
    {
        RandomEnemyMultiHitDamageEffectData singleTargetEffect = GetSourceDamageEffect(context);
        RandomTargetEachHitDamageEffectData randomTargetEffect = GetRandomSourceDamageEffect(context);

        if (singleTargetEffect == null && randomTargetEffect == null) return 0;

        int damage = singleTargetEffect != null ? singleTargetEffect.damagePerHit : randomTargetEffect.damagePerHit;
        damage = Mathf.Max(0, damage);

        if (context != null && context.source != null) damage = context.source.ModifyOutgoingDamage(damage);
        if (singleTargetEffect != null && context != null && context.target != null) damage = context.target.ModifyIncomingDamage(damage);

        return Mathf.Max(0, damage);
    }

    public bool TryGetDescriptionValue(string key, CardResolveContext context, out int value)
    {
        value = 0;

        if (key == "damage" || key == "damege" || key == "damage0" || key == "damege0")
        {
            value = GetPreviewDamage(context);
            return true;
        }

        if (key == "extraHitCount" || key == "extraCount" || key == "追加次數")
        {
            value = GetMatchingHandCardCount(context);
            return true;
        }

        return false;
    }
}