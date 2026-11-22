using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "CardGame/Status Behaviours/Gluttonous Cycle")]
public class GluttonousCycleStatusData : BattleStatusBehaviourData
{
    [Min(0)] public int maxHpLoss = 50;
    [Min(1)] public int minimumMaxHp = 50;

    public override StatusType SupportedStatus => StatusType.GluttonousCycle;

    public override void OnEnemyRoundEnd(EnemyUnit enemy)
    {
        BattleManager manager = enemy.battleManager;

        if (manager == null || manager.enemies == null) return;

        EnemyUnit target = null;

        for (int i = 0; i < manager.enemies.Count; i++)
        {
            EnemyUnit ally = manager.enemies[i];

            if (ally == null || ally == enemy) continue;
            if (!ally.gameObject.activeInHierarchy || ally.currentHp <= 0) continue;
            if ((long)ally.currentHp * 2 >= ally.maxHp) continue;

            if (target == null || ally.currentHp < target.currentHp) target = ally;
        }

        if (target == null) return;

        target.Heal(target.maxHp, enemy);

        int reducedMaxHp = Mathf.Max(minimumMaxHp, enemy.maxHp - maxHpLoss);

        enemy.maxHp = Mathf.Min(enemy.maxHp, reducedMaxHp);
        enemy.currentHp = Mathf.Min(enemy.currentHp, enemy.maxHp);
        enemy.NotifyUnitChanged();
        enemy.RefreshHpUI();
        target.RefreshHpUI();
    }

    public override void OnOwnerDeath(EnemyUnit enemy)
    {
        if (enemy == null) return;

        BattleManager manager = enemy.battleManager;

        if (manager == null || manager.enemies == null) return;

        List<EnemyUnit> allies = new List<EnemyUnit>(manager.enemies);

        for (int i = 0; i < allies.Count; i++)
        {
            EnemyUnit ally = allies[i];

            if (ally == null || ally == enemy) continue;
            if (!ally.gameObject.activeInHierarchy) continue;
            if (ally.currentHp <= 0) continue;

            ally.LoseHpDirectly(ally.currentHp);
        }

        Debug.Log($"[GluttonousCycle] {enemy.unitName} 死亡，消滅所有存活友方。");
    }

    public override string GetDescription(EnemyUnit enemy, int amount)
    {
        return $"敵方回合結束時，治療一名HP低於50%的其他友方，優先選擇目前HP最低者，恢復其最大HP的100%。自身最大HP減少{maxHpLoss}，最低降至{minimumMaxHp}。\n自身死亡時，消滅所有友方。";
    }
}