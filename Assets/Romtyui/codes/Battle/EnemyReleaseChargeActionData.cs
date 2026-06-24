using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy Actions/Release Charge")]
public class EnemyReleaseChargeActionData : EnemyActionData
{
    [Header("Release")]
    public bool useRemainingChargeAsDamage = true;
    public int fallbackDamage = 10;

    public override void Execute(EnemyActionContext context)
    {
        if (context == null)
            return;

        if (context.enemy == null)
            return;

        if (context.player == null)
            return;

        int turnsLeft = context.enemy.TickChargeCountdown();

        if (turnsLeft > 0)
        {
            context.enemy.RequestStayOnCurrentIntent();

            Debug.Log($"[EnemyAction] {context.enemy.unitName} 繼續蓄力，剩餘回合 {turnsLeft}，蓄力值 {context.enemy.chargeValue}");
            return;
        }

        int damage = useRemainingChargeAsDamage
            ? context.enemy.GetChargeDamage()
            : fallbackDamage;

        context.enemy.ClearCharge();

        if (damage <= 0)
        {
            Debug.Log($"[EnemyAction] {context.enemy.unitName} 蓄力值為 0，不造成傷害");
            return;
        }

        context.enemy.DealDamageTo(context.player, damage);

        Debug.Log($"[EnemyAction] {context.enemy.unitName} 釋放蓄力，造成 {damage} 傷害");
    }
}