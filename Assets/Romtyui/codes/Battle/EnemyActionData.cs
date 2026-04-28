using UnityEngine;

public abstract class EnemyActionData : ScriptableObject
{
    public abstract void Execute(EnemyActionContext context);
}