using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy Actions/Gain Block")]
public class EnemyGainBlockActionData : EnemyActionData
{
    public int amount = 5;

    public override void Execute(EnemyActionContext context)
    {
        if (context == null) return;
        if (context.enemy == null) return;

        context.enemy.GainBlock(amount);

        Debug.Log($"{context.enemy.unitName} Àò±o {amount} ®æ¾×");
    }
}