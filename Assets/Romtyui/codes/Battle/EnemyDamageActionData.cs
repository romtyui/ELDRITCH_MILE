using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy Actions/Damage Player")]
public class EnemyDamageActionData : EnemyActionData
{
    public int amount = 6;

    public override void Execute(EnemyActionContext context)
    {
        if (context == null) return;
        if (context.player == null) return;

        context.player.TakeDamage(amount);

        Debug.Log($"{context.enemy.unitName} §ðÀ»ª±®a¡A³y¦¨ {amount} ¶Ë®`");
    }
}