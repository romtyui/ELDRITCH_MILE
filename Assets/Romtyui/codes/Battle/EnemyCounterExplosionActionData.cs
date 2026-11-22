using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy Actions/Counter Explosion")]
public class EnemyCounterExplosionActionData : EnemyActionData
{
    [Min(1)]
    public int hitCount = 3;

    public override void Execute(EnemyActionContext context)
    {
        if (context == null || context.enemy == null || context.player == null) return;

        EnemyUnit enemy = context.enemy;
        BattleUnit player = context.player;

        if (enemy.currentHp <= 0 || !enemy.gameObject.activeInHierarchy) return;

        int baseDamage = Mathf.Max(0, enemy.GetStatus(StatusType.Counter));
        int totalHits = Mathf.Max(1, hitCount);

        for (int i = 0; i < totalHits; i++)
        {
            if (enemy == null || player == null) break;
            if (enemy.currentHp <= 0 || player.currentHp <= 0) break;
            if (!enemy.gameObject.activeInHierarchy) break;

            enemy.DealDamageTo(player, baseDamage);

            Debug.Log($"[CounterExplosion] {enemy.unitName} Ãzµõ¡A²Ä {i + 1}/{totalHits} ¬q¡A°òÂ¦¶Ë®` {baseDamage}¡C");
        }

        if (enemy != null && enemy.currentHp > 0)
        {
            enemy.LoseHpDirectly(enemy.currentHp);
        }
    }
}