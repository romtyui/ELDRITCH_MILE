using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Status/Statuses/Inflation Cycle")]
public class InflationCycleStatusData : BattleStatusBehaviourData
{
    public override StatusType SupportedStatus => StatusType.InflationCycle;

    public EnemyIntentData inflationIntent;
    public EnemyIntentData explosionIntent;

    [Min(1)]
    public int requiredInflationStacks = 3;

    public override void OnSpawn(EnemyUnit enemy)
    {
        if (enemy == null) return;

        SelectIntent(enemy);
    }

    public override bool ControlsNextIntent(EnemyUnit enemy)
    {
        return true;
    }

    public override bool TrySelectNextIntentAtTurnEnd(EnemyUnit enemy)
    {
        return SelectIntent(enemy);
    }

    private bool SelectIntent(EnemyUnit enemy)
    {
        if (enemy == null || !enemy.CanSwitchStatusPhase) return false;

        int requiredStacks = Mathf.Max(1, requiredInflationStacks);
        EnemyIntentData selectedIntent = enemy.GetStatus(StatusType.Inflation) >= requiredStacks ? explosionIntent : inflationIntent;

        return enemy.TrySetStatusIntent(selectedIntent);
    }

    public override string GetDescription(EnemyUnit enemy, int amount)
    {
        return $"膨脹達到{Mathf.Max(1, requiredInflationStacks)}層後，下次行動改為爆裂。";
    }
}