using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Status/Statuses/Weak Point Break")]
public class WeakPointBreakStatusData : BattleStatusBehaviourData
{
    public override StatusType SupportedStatus => StatusType.WeakPointBreak;

    [Min(0)]
    [Tooltip("此怪物每成功使玩家隨機捨棄一張手牌，賦予玩家的虛弱層數。")]
    public int weakPerCard = 1;

    public override void OnSuccessfullyDiscardedHandCards(EnemyUnit enemy, int amount)
    {
        if (enemy == null || enemy.currentHp <= 0) return;
        if (!enemy.gameObject.activeInHierarchy) return;
        if (!enemy.HasStatus(SupportedStatus)) return;
        if (amount <= 0) return;

        BattleManager manager = enemy.battleManager;

        if (manager == null || manager.playerUnit == null) return;
        if (manager.playerUnit.currentHp <= 0) return;

        int weakAmount = amount * Mathf.Max(0, weakPerCard);

        if (weakAmount <= 0) return;

        manager.playerUnit.ApplyStatus(StatusType.Weak, weakAmount);

        Debug.Log($"[WeakPointBreak] {enemy.unitName} 成功使玩家捨棄 {amount} 張手牌，賦予玩家 {weakAmount} 層虛弱。");
    }

    public override string GetDescription(EnemyUnit enemy, int amount)
    {
        return $"當玩家的手牌被此怪物隨機捨棄時，每成功捨棄1張，賦予玩家{Mathf.Max(0, weakPerCard)}層虛弱。";
    }
}