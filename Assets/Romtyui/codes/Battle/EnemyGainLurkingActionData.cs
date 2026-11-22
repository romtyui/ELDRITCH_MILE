using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy Actions/Gain Lurking")]
public class EnemyGainLurkingActionData : EnemyActionData
{
    public override void Execute(EnemyActionContext context)
    {
        if (context == null || context.enemy == null) return;
        if (context.enemy.currentHp <= 0) return;
        if (!context.enemy.gameObject.activeInHierarchy) return;

        context.enemy.SetStatus(StatusType.Lurking, 1);
        context.enemy.RefreshAllUI();

        Debug.Log($"[Lurking] {context.enemy.unitName} 進入潛伏，下一個玩家回合結束時解除。");
    }
}