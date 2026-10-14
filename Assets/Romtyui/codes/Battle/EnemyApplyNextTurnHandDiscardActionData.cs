using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy Actions/Apply Next Turn Hand Discard")]
public class EnemyApplyNextTurnHandDiscardActionData : EnemyActionData
{
    [Min(1)]
    [Tooltip("玩家下次回合抽牌後，隨機捨棄的手牌張數。成功捨棄的張數會記錄到施加此效果的怪物身上。")]
    public int amount = 2;

    public override void Execute(EnemyActionContext context)
    {
        if (context == null) return;
        if (context.enemy == null || context.player == null) return;
        if (context.enemy.currentHp <= 0 || context.player.currentHp <= 0) return;

        context.player.QueueHandDiscardAtNextTurnStart(context.enemy, Mathf.Max(1, amount));
    }
}