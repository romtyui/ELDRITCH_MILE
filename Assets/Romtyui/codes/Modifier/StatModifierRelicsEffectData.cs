using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Relics Effects/Stat Modifier")]
public class StatModifierRelicsEffectData : RelicsModifierEffectData
{
    [Header("Modifier")]
    public ModifierType modifierType = ModifierType.HealingReceived;

    public ModifierOperation operation = ModifierOperation.AddPercent;

    [Tooltip("AddPercent 使用小數，例如 0.1 = +10%。Multiply 則 2 = ×2。")]
    public float value = 0.1f;

    [Tooltip("只影響 Override / Clamp 等需要順序的效果。數字越大越晚處理。")]
    public int priority;

    [Header("Unit Requirement")]
    public ModifierUnitRequirement unitRequirement = ModifierUnitRequirement.Any;

    public override void CollectModifiers(ModifierQuery query, List<ModifierValue> results, Object modifierSource)
    {
        if (query == null || results == null)
            return;

        if (query.type != modifierType)
            return;

        if (!MatchesUnitRequirement(query))
            return;

        Object finalSource = modifierSource != null ? modifierSource : this;

        results.Add(new ModifierValue(modifierType, operation, value, priority, finalSource));
    }

    private bool MatchesUnitRequirement(ModifierQuery query)
    {
        switch (unitRequirement)
        {
            case ModifierUnitRequirement.SourceIsPlayer:
                return query.source != null && query.source.isPlayerUnit;

            case ModifierUnitRequirement.TargetIsPlayer:
                return query.target != null && query.target.isPlayerUnit;

            case ModifierUnitRequirement.SourceOrTargetIsPlayer:
                return (query.source != null && query.source.isPlayerUnit) ||
                       (query.target != null && query.target.isPlayerUnit);

            case ModifierUnitRequirement.Any:
            default:
                return true;
        }
    }
}