using UnityEngine;

public abstract class EnemyActionData : ScriptableObject
{
    [Header("Tooltip")]
    public TooltipEntry tooltipEntry = new TooltipEntry();

    public abstract void Execute(EnemyActionContext context);

    public virtual TooltipEntry GetTooltipEntry()
    {
        return tooltipEntry;
    }
}