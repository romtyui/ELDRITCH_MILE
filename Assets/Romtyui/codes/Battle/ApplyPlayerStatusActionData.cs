using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy Actions/Apply Player Status")]
public class ApplyPlayerStatusActionData : EnemyActionData
{
    public StatusType statusType;
    [Min(1)] public int amount = 1;

    public override void Execute(EnemyActionContext context)
    {
        if (context == null || context.enemy == null || context.player == null) return;
        if (context.enemy.currentHp <= 0 || context.player.currentHp <= 0) return;

        context.player.ApplyStatus(statusType, amount);
    }
}