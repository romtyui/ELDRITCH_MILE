using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy Actions/Swallow")]
public class EnemySwallowActionData : EnemyActionData
{
    [Range(0f, 1f)]
    [Tooltip("每成功捨棄一張手牌，恢復自身最大 HP 的比例。0.1 表示 10%。總治療量向上取整，並沿用現有治療 Modifier。")]
    public float healMaxHpRatioPerCard = 0.1f;

    [Min(0)]
    [Tooltip("每成功捨棄一張手牌，失去的力量層數。力量最低扣到 0。")]
    public int strengthLossPerCard = 1;

    public override void Execute(EnemyActionContext context)
    {
        if (context == null || context.enemy == null) return;
        if (context.enemy.currentHp <= 0) return;

        EnemyUnit enemy = context.enemy;
        int cardCount = enemy.ConsumeSuccessfullyDiscardedHandCardCount();

        if (cardCount <= 0) return;

        int healAmount = Mathf.CeilToInt(enemy.maxHp * Mathf.Clamp01(healMaxHpRatioPerCard) * cardCount);
        int strengthLoss = Mathf.Min(enemy.GetStatus(StatusType.Strength), cardCount * Mathf.Max(0, strengthLossPerCard));

        if (healAmount > 0) enemy.Heal(healAmount);
        if (strengthLoss > 0) enemy.RemoveStatus(StatusType.Strength, strengthLoss);

        enemy.RefreshAllUI();

        Debug.Log($"[{enemy.unitName}] 吞嚥：成功捨棄 {cardCount} 張手牌，基礎治療 {healAmount}，失去 {strengthLoss} 層力量");
    }
}