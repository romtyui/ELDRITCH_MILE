using UnityEngine;

public abstract class BattleStatusBehaviourData : ScriptableObject
{
    public abstract StatusType SupportedStatus { get; }
    public virtual bool SelectsIntentAtPlayerTurnEnd => false;
    public virtual void OnSpawn(EnemyUnit enemy)
    {
    }

    public virtual bool ControlsNextIntent(EnemyUnit enemy)
    {
        return false;
    }

    public virtual bool TrySelectNextIntentAtTurnEnd(EnemyUnit enemy)
    {
        return false;
    }

    public virtual string GetDescription(EnemyUnit enemy, int amount)
    {
        return $"目前層數：{amount}";
    }
    public virtual void OnPlayerTurnEnd(EnemyUnit enemy)
    {
    }
    public virtual bool TrySelectNextIntentAtPlayerTurnEnd(EnemyUnit enemy)
    {
        return false;
    }
    public virtual void OnSuccessfullyDiscardedHandCards(EnemyUnit enemy, int amount)
    {
    }
    public virtual void OnAttacked(EnemyUnit enemy, BattleUnit attacker)
    {
    }

    public virtual void OnEnemyRoundStart(EnemyUnit enemy)
    {
    }

    public virtual void OnIntentExecuted(EnemyUnit enemy, EnemyIntentData intent, bool isAdditionalAction)
    {
    }
    public virtual void OnOwnerDeath(EnemyUnit enemy)
    {
    }
    public virtual void OnEnemyRoundEnd(EnemyUnit enemy)
    {
    }
}