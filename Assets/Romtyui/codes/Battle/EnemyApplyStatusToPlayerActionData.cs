using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy Actions/Apply Status To Player")]
public class EnemyApplyStatusToPlayerActionData : EnemyActionData
{
    public StatusType statusType;
    public int amount = 1;

    public override void Execute(EnemyActionContext context)
    {
        if (context == null) return;
        if (context.player == null) return;

        context.player.ApplyStatus(statusType, amount);

        Debug.Log($"{context.enemy.unitName} ¹ïª±®a¬I¥[ {statusType} x{amount}");
    }
}