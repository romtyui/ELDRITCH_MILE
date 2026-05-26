using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Random Enemy Multi Hit Damage")]
public class RandomEnemyMultiHitDamageEffectData : CardEffectData
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
}