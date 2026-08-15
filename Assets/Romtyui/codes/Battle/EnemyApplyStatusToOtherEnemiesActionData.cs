using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy Actions/Apply Status To Other Enemies")]
public class EnemyApplyStatusToOtherEnemiesActionData : EnemyActionData
{
    [Header("Status")]
    public StatusType statusType = StatusType.Strength;
    public int amount = 1;

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

            enemy.ApplyStatus(statusType, amount);
            enemy.RefreshAllUI();

            Debug.Log($"[EnemyAction] {context.enemy.unitName} ½á¤© {enemy.unitName} {statusType} {amount} ¼h");
        }
    }
}