using UnityEngine;

public abstract class ItemEffectData : ScriptableObject
{
    public abstract void Execute(ItemUseContext context);
}