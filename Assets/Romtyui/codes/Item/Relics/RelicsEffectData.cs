using UnityEngine;

public enum RelicsTriggerType
{
    BattleStart,
    PlayerTurnStart,
    PlayerTurnEnd
}

public abstract class RelicsEffectData : ScriptableObject
{
    [Header("Trigger")]
    public RelicsTriggerType triggerType = RelicsTriggerType.BattleStart;

    public abstract void Execute(RelicsUseContext context);
}