using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Random Enemy Multi Hit Damage")]
public class RandomEnemyMultiHitDamageEffectData : CardEffectData, CardDescriptionValueProvider
{
    [Header("Damage")]
    public int damagePerHit = 3;

    [Header("Hit Count")]
    public int hitCount = 3;

    public override void Execute(CardResolveContext context)
    {
        if (context == null) return;
        if (context.source == null) return;
        if (context.battleManager == null) return;

        for (int i = 0; i < hitCount; i++)
        {
            BattleUnit randomTarget = context.battleManager.GetRandomAliveEnemyPublic();

            if (randomTarget == null)
            {
                Debug.Log("[RandomEnemyMultiHitDamageEffectData] 沒有可攻擊的敵人");
                return;
            }

            context.source.DealDamageTo(randomTarget, damagePerHit);

            Debug.Log($"[Random Multi Hit] 第 {i + 1} 次命中 {randomTarget.unitName}，基礎傷害 {damagePerHit}");
        }
    }

    public bool TryGetDescriptionValue(string key, CardResolveContext context, out int value)
    {
        value = 0;

        if (key == "damage" || key == "damege" || key == "damage0" || key == "damege0")
        {
            int previewDamage = damagePerHit;

            if (context != null && context.source != null)
                previewDamage = context.source.ModifyOutgoingDamage(previewDamage);

            value = Mathf.Max(0, previewDamage);
            return true;
        }

        if (key == "hit" || key == "hits" || key == "count" || key == "hitCount")
        {
            value = hitCount;
            return true;
        }

        return false;
    }
}