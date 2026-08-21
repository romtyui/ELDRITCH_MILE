using UnityEngine;

public class ModifierValue
{
    public ModifierType type;
    public ModifierOperation operation;
    public float value;
    public int priority;
    public Object source;

    public ModifierValue(ModifierType type, ModifierOperation operation, float value, int priority = 0, Object source = null)
    {
        this.type = type;
        this.operation = operation;
        this.value = value;
        this.priority = priority;
        this.source = source;
    }
}