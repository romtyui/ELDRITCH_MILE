using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Status Behaviours/Abyss Strategy")]
public class AbyssStrategyStatusData : BattleStatusBehaviourData
{
    public EnemyIntentData summonIntent;
    public EnemyIntentData immortalFleshIntent;
    public EnemyIntentData lamentIntent;

    [Min(1)] public int requiredAllyCount = 3;
    [Min(1)] public int guaranteedHealingInterval = 8;

    public override StatusType SupportedStatus => StatusType.AbyssStrategy;
    public override bool SelectsIntentAtPlayerTurnEnd => true;

    public override void OnSpawn(EnemyUnit enemy)
    {
        enemy.abyssStrategyRuntime = new EnemyUnit.AbyssStrategyRuntimeState();
        enemy.AdditionalIntent = null;
        enemy.TrySetStatusIntent(summonIntent);
    }

    public override bool ControlsNextIntent(EnemyUnit enemy)
    {
        return true;
    }

    public override void OnPlayerTurnEnd(EnemyUnit enemy)
    {
        SelectIntent(enemy);
    }

    public override bool TrySelectNextIntentAtPlayerTurnEnd(EnemyUnit enemy)
    {
        return SelectIntent(enemy);
    }

    public override void OnEnemyRoundStart(EnemyUnit enemy)
    {
        enemy.abyssStrategyRuntime.healedThisRound = false;
        SelectIntent(enemy);
    }

    private bool SelectIntent(EnemyUnit enemy)
    {
        if (enemy == null || !enemy.CanSwitchStatusPhase) return false;

        enemy.AdditionalIntent = null;

        EnemyUnit.AbyssStrategyRuntimeState runtime = enemy.abyssStrategyRuntime;

        if (!runtime.hasExecutedNormalAction) return enemy.TrySetStatusIntent(summonIntent);

        if (runtime.roundsWithoutHealing >= guaranteedHealingInterval)
        {
            enemy.AdditionalIntent = immortalFleshIntent;
        }

        int allyCount = CountAliveAllies(enemy);

        if (allyCount < requiredAllyCount) return enemy.TrySetStatusIntent(summonIntent);
        if ((long)enemy.currentHp * 2 < enemy.maxHp) return enemy.TrySetStatusIntent(immortalFleshIntent);

        return enemy.TrySetStatusIntent(lamentIntent);
    }

    private int CountAliveAllies(EnemyUnit enemy)
    {
        BattleManager manager = enemy.battleManager;

        if (manager == null || manager.enemies == null) return 1;

        int count = 0;
        bool includesSelf = false;

        for (int i = 0; i < manager.enemies.Count; i++)
        {
            EnemyUnit ally = manager.enemies[i];

            if (ally == null || ally.currentHp <= 0 || !ally.gameObject.activeInHierarchy) continue;

            count++;

            if (ally == enemy) includesSelf = true;
        }

        if (!includesSelf) count++;

        return count;
    }

    public override void OnIntentExecuted(EnemyUnit enemy, EnemyIntentData intent, bool isAdditionalAction)
    {
        EnemyUnit.AbyssStrategyRuntimeState runtime = enemy.abyssStrategyRuntime;

        if (!isAdditionalAction) runtime.hasExecutedNormalAction = true;

        if (intent == immortalFleshIntent)
        {
            runtime.roundsWithoutHealing = 0;
            runtime.healedThisRound = true;
        }
    }

    public override void OnEnemyRoundEnd(EnemyUnit enemy)
    {
        EnemyUnit.AbyssStrategyRuntimeState runtime = enemy.abyssStrategyRuntime;

        if (runtime.healedThisRound)
        {
            runtime.roundsWithoutHealing = 0;
        }
        else
        {
            runtime.roundsWithoutHealing++;
        }

        SelectIntent(enemy);
    }

    public override string GetDescription(EnemyUnit enemy, int amount)
    {
        return "首次行動必定召喚。友方不足3名時優先召喚；否則自身HP低於50%時回血，其餘使用悲歌。連續8個回合未回血後，下回合額外使用一次不朽的血肉。";
    }
}