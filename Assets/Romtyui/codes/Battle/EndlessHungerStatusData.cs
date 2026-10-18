using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Status/Statuses/Endless Hunger")]
public class EndlessHungerStatusData : BattleStatusBehaviourData
{
    public override StatusType SupportedStatus => StatusType.EndlessHunger;

    [Range(0f, 1f)]
    [Tooltip("玩家回合結束時，失去自身最大 HP 的比例。0.1 表示 10%，失血量向上取整。")]
    public float maxHpLossRatio = 0.1f;

    [Min(0)]
    [Tooltip("玩家回合結束時，自損後仍存活則獲得的力量層數。")]
    public int strengthGain = 1;

    public override void OnPlayerTurnEnd(EnemyUnit enemy)
    {
        if (enemy == null || enemy.currentHp <= 0) return;
        if (!enemy.HasStatus(SupportedStatus)) return;

        int hpLoss = Mathf.CeilToInt(enemy.maxHp * Mathf.Clamp01(maxHpLossRatio));

        if (hpLoss > 0) enemy.LoseHpDirectly(hpLoss);
        if (enemy.currentHp <= 0) return;
        if (strengthGain > 0) enemy.ApplyStatus(StatusType.Strength, strengthGain);

        enemy.RefreshAllUI();
    }

    public override string GetDescription(EnemyUnit enemy, int amount)
    {
        float percent = Mathf.Clamp01(maxHpLossRatio) * 100f;

        return $"玩家回合結束時，失去自身最大 HP 的 {percent:0.##}%，並獲得 {Mathf.Max(0, strengthGain)} 層力量。";
    }
}