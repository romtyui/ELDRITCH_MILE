using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy Actions/Immortal Flesh")]
public class ImmortalFleshActionData : EnemyActionData
{
    [Range(0f, 1f)] public float healMaxHpRatio = 0.8f;
    [Min(0)] public int blockAmount = 8;

    public override void Execute(EnemyActionContext context)
    {
        if (context == null || context.enemy == null) return;
        if (context.enemy.currentHp <= 0) return;

        int healAmount = Mathf.RoundToInt(context.enemy.maxHp * healMaxHpRatio);

        context.enemy.Heal(healAmount, context.enemy);
        context.enemy.GainBlock(blockAmount);
    }
}