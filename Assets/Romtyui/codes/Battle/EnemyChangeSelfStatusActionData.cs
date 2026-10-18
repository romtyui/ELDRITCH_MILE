using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy Actions/Change Self Status")]
public class EnemyChangeSelfStatusActionData : EnemyActionData
{
    [Tooltip("要給予或清除的自身狀態。")]
    public StatusType statusType = StatusType.Strength;

    [Tooltip("勾選時，清除此狀態的全部層數；未勾選時，給予指定層數。")]
    public bool clearAll;

    [Min(0)]
    [Tooltip("未勾選 Clear All 時，給予的狀態層數。")]
    public int amount = 3;

    public override void Execute(EnemyActionContext context)
    {
        if (context == null || context.enemy == null) return;
        if (context.enemy.currentHp <= 0) return;

        if (clearAll)
        {
            context.enemy.ClearStatusCompletely(statusType);
        }
        else
        {
            if (amount <= 0) return;

            context.enemy.ApplyStatus(statusType, amount);
        }

        context.enemy.RefreshAllUI();
    }
}