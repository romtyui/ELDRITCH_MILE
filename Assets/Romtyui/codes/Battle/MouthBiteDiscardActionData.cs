using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy Actions/Mouth Bite Discard")]
public class MouthBiteDiscardActionData : EnemyActionData
{
    [Min(1)]
    public int discardAmount = 1;

    public override void Execute(EnemyActionContext context)
    {
        if (context == null || context.enemy == null || context.player == null) return;
        if (context.enemy.currentHp <= 0 || context.player.currentHp <= 0) return;
        if (!context.enemy.gameObject.activeInHierarchy) return;

        int amount = Mathf.Max(0, discardAmount);

        if (amount <= 0) return;

        context.player.QueueHandDiscardAtNextTurnStart(context.enemy, amount);

        Debug.Log($"[MouthBite] {context.enemy.unitName} 安排玩家下回合開始時隨機捨棄 {amount} 張手牌。");
    }
}