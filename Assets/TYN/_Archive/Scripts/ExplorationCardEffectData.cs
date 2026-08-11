using UnityEngine;

public abstract class ExplorationCardEffectData : ScriptableObject
{
    // 當卡牌被打出時，執行這個方法
    public abstract void Execute(ExplorationCardResolveContext context);
}