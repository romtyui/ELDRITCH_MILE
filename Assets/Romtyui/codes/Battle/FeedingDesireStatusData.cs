using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Status/Statuses/Feeding Desire")]
public class FeedingDesireStatusData : BattleStatusBehaviourData
{
    public override StatusType SupportedStatus => StatusType.FeedingDesire;

    [Min(0)]
    [Tooltip("此怪物每成功使玩家隨機捨棄一張手牌，獲得的力量層數。")]
    public int strengthPerCard = 1;

    public override void OnSuccessfullyDiscardedHandCards(EnemyUnit enemy, int amount)
    {
        if (enemy == null || enemy.currentHp <= 0) return;
        if (!enemy.HasStatus(SupportedStatus)) return;
        if (amount <= 0) return;

        int strengthGain = amount * Mathf.Max(0, strengthPerCard);

        if (strengthGain <= 0) return;

        enemy.ApplyStatus(StatusType.Strength, strengthGain);
        enemy.RefreshAllUI();

        Debug.Log($"[FeedingDesire] {enemy.unitName} 成功使玩家捨棄 {amount} 張手牌，獲得 {strengthGain} 層力量");
    }

    public override string GetDescription(EnemyUnit enemy, int amount)
    {
        return $"當玩家的手牌被此怪物隨機捨棄時，每成功捨棄 1 張，獲得 {Mathf.Max(0, strengthPerCard)} 層力量。";
    }
}