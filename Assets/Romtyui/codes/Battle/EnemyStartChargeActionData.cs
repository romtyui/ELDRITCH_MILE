using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy Actions/Start Charge")]
public class EnemyStartChargeActionData : EnemyActionData
{
    [Header("Charge")]
    public int chargeValue = 30;
    public int countdownTurns = 2;

    public override void Execute(EnemyActionContext context)
    {
        if (context == null)
            return;

        if (context.enemy == null)
            return;

        context.enemy.StartCharge(chargeValue, countdownTurns);

        Debug.Log($"[EnemyAction] {context.enemy.unitName} 開始蓄力：{chargeValue}，倒數 {countdownTurns} 回合");
    }
}