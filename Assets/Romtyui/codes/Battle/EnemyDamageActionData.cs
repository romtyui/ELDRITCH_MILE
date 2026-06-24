using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy Actions/Damage Player")]
public class EnemyDamageActionData : EnemyActionData
{
    public int amount = 6;

    public override void Execute(EnemyActionContext context)
    {
        if (context == null) return;
        if (context.enemy == null) return;
        if (context.player == null) return;

        context.enemy.DealDamageTo(context.player, amount);

        Debug.Log($"{context.enemy.unitName} §ðÀ»ª±®a¡A°òÂ¦¶Ë®` {amount}");
    }
    //public override TooltipEntry GetTooltipEntry()
    //{
    //    return new TooltipEntry(
    //        "§ðÀ»",
    //        $"§ðÀ»³y¦¨ {amount} ÂI¶Ë®`"
    //    );
    //}
}