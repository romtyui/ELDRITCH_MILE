using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy Actions/Shell Bash")]
public class EnemyShellBashActionData : EnemyActionData
{
    public override void Execute(EnemyActionContext context)
    {
        if (context == null) return;
        if (context.enemy == null) return;
        if (context.player == null) return;
        if (context.enemy.currentHp <= 0) return;

        int baseDamage = Mathf.Max(0, context.enemy.block);

        context.enemy.DealDamageTo(context.player, baseDamage);

        Debug.Log($"{context.enemy.unitName} 使用貝殼撞擊，依自身護盾量造成基礎傷害：{baseDamage}");
    }
}