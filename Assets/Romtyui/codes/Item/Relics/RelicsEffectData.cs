using UnityEngine;

public enum RelicsTriggerType
{
    BattleStart,

    PlayerTurnStart,

    PlayerTurnEnd,

    CardPlayed
}

public abstract class RelicsEffectData : ScriptableObject
{
    [Header("Trigger")]
    [Tooltip("這個遺物效果會在哪個戰鬥時間點觸發。")]
    public RelicsTriggerType triggerType =
        RelicsTriggerType.BattleStart;


    /// <summary>
    /// 判斷這個效果是否應該在目前時間點觸發。
    /// </summary>
    public bool CanTrigger(
        RelicsTriggerType currentTrigger
    )
    {
        return triggerType ==
               currentTrigger;
    }


    /// <summary>
    /// 真正執行遺物效果。
    /// </summary>
    public abstract void Execute(
        RelicsUseContext context
    );
}