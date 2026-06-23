using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy Actions/Heal Other Enemies")]
public class EnemyHealOtherEnemiesActionData : EnemyActionData
{
    [Header("Heal")]
    public int healAmount = 10;

    public override void Execute(EnemyActionContext context)
    {
        if (context == null)
            return;

        if (context.enemy == null)
            return;

        if (context.battleManager == null)
            return;

        if (context.battleManager.enemies == null)
            return;

        for (int i = 0; i < context.battleManager.enemies.Count; i++)
        {
            EnemyUnit enemy = context.battleManager.enemies[i];

            if (enemy == null)
                continue;

            if (enemy == context.enemy)
                continue;

            if (!enemy.gameObject.activeInHierarchy)
                continue;

            if (enemy.currentHp <= 0)
                continue;

            enemy.Heal(healAmount);
            enemy.RefreshAllUI();

            Debug.Log($"[EnemyAction] {context.enemy.unitName} 治療 {enemy.unitName} {healAmount} 點生命");
        }
    }
}