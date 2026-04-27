using UnityEngine;

public abstract class CardEffectData : ScriptableObject
{
    public abstract void Execute(CardResolveContext context);
}