using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Status/Statuses/Feeding Temptation")]
public class FeedingTemptationStatusData : BattleStatusBehaviourData
{
    public override StatusType SupportedStatus => StatusType.FeedingTemptation;

    [Min(1)]
    public int hungerPerRound = 1;

    public override void OnEnemyRoundStart(EnemyUnit enemy)
    {
        if (enemy == null || enemy.currentHp <= 0) return;
        if (!enemy.gameObject.activeInHierarchy) return;
        if (!enemy.HasStatus(SupportedStatus)) return;

        BattleManager manager = enemy.battleManager;

        if (manager == null || manager.playerUnit == null) return;
        if (manager.playerUnit.currentHp <= 0) return;

        int amount = Mathf.Max(0, hungerPerRound);

        if (amount <= 0) return;

        manager.playerUnit.ApplyStatus(StatusType.Hunger, amount);

        Debug.Log($"[FeedingTemptation] {enemy.unitName} 在敵方回合開始時賦予玩家 {amount} 層飢餓。");
    }

    public override string GetDescription(EnemyUnit enemy, int amount)
    {
        return $"敵方回合開始時，賦予玩家{Mathf.Max(0, hungerPerRound)}層飢餓。場上存活友方包含自己少於3名時，正常行動優先使用深淵的呼喚。";
    }
}