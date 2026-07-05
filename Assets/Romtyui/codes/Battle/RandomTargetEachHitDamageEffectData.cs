using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Random Target Each Hit Damage")]
public class RandomTargetEachHitDamageEffectData : CardEffectData, CardDescriptionValueProvider
{
    [Header("Damage")]
    [Tooltip("每一下造成的基礎傷害")]
    public int damagePerHit = 3;

    [Header("Hit Count")]
    [Tooltip("總共攻擊幾下。每一下都會重新隨機選一個存活敵人")]
    public int hitCount = 3;

    public override void Execute(CardResolveContext context)
    {
        if (context == null)
            return;

        if (context.source == null)
            return;

        if (context.battleManager == null)
        {
            Debug.LogWarning("[RandomTargetEachHitDamageEffectData] context.battleManager 是 null，無法取得隨機敵人");
            return;
        }

        int finalHitCount = Mathf.Max(0, hitCount);

        for (int i = 0; i < finalHitCount; i++)
        {
            BattleUnit randomTarget = context.battleManager.GetRandomAliveEnemyPublic();

            if (randomTarget == null)
            {
                Debug.Log("[RandomTargetEachHitDamageEffectData] 沒有可攻擊的敵人，停止後續攻擊");
                return;
            }

            if (randomTarget.currentHp <= 0)
                continue;

            context.source.DealDamageTo(randomTarget, damagePerHit);

            Debug.Log(
                $"[RandomTargetEachHitDamageEffectData] 第 {i + 1} 下隨機命中 {randomTarget.unitName}，基礎傷害 {damagePerHit}"
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
            key == "repeatCount")
        {
            value = Mathf.Max(0, hitCount);
            return true;
        }

        return false;
    }
}