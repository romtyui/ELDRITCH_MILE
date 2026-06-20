using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Token/Random Damage By Hand Token Count")]
public class RandomDamageByHandTokenCountEffectData : CardEffectData
{
    [Header("Token")]
    public string tokenId = "DefaultToken";

    [Header("Damage")]
    public int damagePerHit = 1;

    public override void Execute(CardResolveContext context)
    {
        if (context == null) return;
        if (context.source == null) return;
        if (context.battleManager == null) return;

        int tokenCountInHand = context.battleManager.CountTokenInHand(tokenId);

        Debug.Log($"[Token Random Damage] 手牌 tokenId={tokenId} 數量：{tokenCountInHand}");

        for (int i = 0; i < tokenCountInHand; i++)
        {
            BattleUnit randomEnemy = context.battleManager.GetRandomAliveEnemyPublic();

            if (randomEnemy == null)
            {
                Debug.Log("[Token Random Damage] 沒有可攻擊的敵人");
                return;
            }

            context.source.DealDamageTo(randomEnemy, damagePerHit);

            Debug.Log($"[Token Random Damage] 第 {i + 1} 次命中 {randomEnemy.unitName}，傷害 {damagePerHit}");
        }
    }
}