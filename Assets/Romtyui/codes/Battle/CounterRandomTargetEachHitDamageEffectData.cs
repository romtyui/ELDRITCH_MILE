using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Counter/Counter Random Target Each Hit Damage")]
public class CounterRandomTargetEachHitDamageEffectData : CardEffectData, CardDescriptionValueProvider
{
    [Header("Damage")]
    [Tooltip("每一下造成的基礎傷害")]
    public int damagePerHit = 3;

    public override void Execute(CardResolveContext context)
    {
        if (context == null)
            return;

        if (context.source == null)
            return;

        if (context.battleManager == null)
        {
            Debug.LogWarning("[CounterRandomTargetEachHitDamageEffectData] context.battleManager 是 null，無法取得隨機敵人");
            return;
        }

        int counterAmount = context.source.GetStatus(StatusType.Counter);
        int finalHitCount = Mathf.Max(0, counterAmount);

        if (finalHitCount <= 0)
        {
            Debug.Log("[CounterRandomTargetEachHitDamageEffectData] 出牌者沒有反擊層數，不進行攻擊");
            return;
        }

        for (int i = 0; i < finalHitCount; i++)
        {
            BattleUnit randomTarget = context.battleManager.GetRandomAliveEnemyPublic();

            if (randomTarget == null)
            {
                Debug.Log("[CounterRandomTargetEachHitDamageEffectData] 沒有可攻擊的敵人，停止後續攻擊");
                return;
            }

            if (randomTarget.currentHp <= 0)
                continue;

            context.source.DealDamageTo(randomTarget, damagePerHit);

            Debug.Log(
                $"[CounterRandomTargetEachHitDamageEffectData] 第 {i + 1}/{finalHitCount} 下隨機命中 {randomTarget.unitName}，基礎傷害 {damagePerHit}"
            );
        }
    }

    public bool TryGetDescriptionValue(string key, CardResolveContext context, out int value)
    {
        value = 0;

        if (key == "damage" ||
            key == "damege" ||
            key == "damage0" ||
            key == "damege0")
        {
            int previewDamage = damagePerHit;

            if (context != null && context.source != null)
                previewDamage = context.source.ModifyOutgoingDamage(previewDamage);

            value = Mathf.Max(0, previewDamage);
            return true;
        }

        if (key == "hit" ||
            key == "hits" ||
            key == "count" ||
            key == "hitCount" ||
            key == "repeatCount" ||
            key == "counter")
        {
            if (context != null && context.source != null)
                value = Mathf.Max(0, context.source.GetStatus(StatusType.Counter));

            return true;
        }

        return false;
    }
}