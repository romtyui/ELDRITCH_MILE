using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy Actions/Summon Abyss Enemy")]
public class SummonAbyssEnemyActionData : EnemyActionData
{
    public override void Execute(EnemyActionContext context)
    {
        if (context == null || context.enemy == null || context.battleManager == null) return;
        if (context.enemy.currentHp <= 0) return;

        EnemyFormationSpawner spawner = context.battleManager.enemyFormationSpawner;

        if (spawner == null)
        {
            Debug.LogWarning("[SummonAbyssEnemyActionData] BattleManager 沒有指定 EnemyFormationSpawner。");
            return;
        }

        spawner.SummonRandomAbyssEnemy();
    }
}