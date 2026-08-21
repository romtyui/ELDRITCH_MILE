using System.Collections.Generic;
using UnityEngine;

public static class ModifierCalculator
{
    public static float CalculateFloat(float baseValue, List<ModifierValue> modifiers, bool clampResultToZero = true)
    {
        if (modifiers == null || modifiers.Count == 0)
            return clampResultToZero ? Mathf.Max(0f, baseValue) : baseValue;

        modifiers.Sort((a, b) => a.priority.CompareTo(b.priority));

        float flat = 0f;
        float additivePercent = 0f;
        float multiplier = 1f;

        for (int i = 0; i < modifiers.Count; i++)
        {
            ModifierValue modifier = modifiers[i];

            if (modifier == null)
                continue;

            switch (modifier.operation)
            {
                case ModifierOperation.Flat:
                    flat += modifier.value;
                    break;

                case ModifierOperation.AddPercent:
                    additivePercent += modifier.value;
                    break;

                case ModifierOperation.Multiply:
                    multiplier *= modifier.value;
                    break;
            }
        }

        float finalValue = baseValue;

        finalValue += flat;
        finalValue *= 1f + additivePercent;
        finalValue *= multiplier;

        for (int i = 0; i < modifiers.Count; i++)
        {
            ModifierValue modifier = modifiers[i];

            if (modifier == null)
                continue;

            switch (modifier.operation)
            {
                case ModifierOperation.Override:
                    finalValue = modifier.value;
                    break;

                case ModifierOperation.ClampMin:
                    finalValue = Mathf.Max(finalValue, modifier.value);
                    break;

                case ModifierOperation.ClampMax:
                    finalValue = Mathf.Min(finalValue, modifier.value);
                    break;
            }
        }

        if (clampResultToZero)
            finalValue = Mathf.Max(0f, finalValue);

        return finalValue;
    }

    public static int CalculateInt(int baseValue, List<ModifierValue> modifiers, ModifierRoundingMode roundingMode = ModifierRoundingMode.Nearest, bool clampResultToZero = true)
    {
        float finalValue = CalculateFloat(baseValue, modifiers, clampResultToZero);

        switch (roundingMode)
        {
            case ModifierRoundingMode.Floor:
                return Mathf.FloorToInt(finalValue);

            case ModifierRoundingMode.Ceil:
                return Mathf.CeilToInt(finalValue);

            case ModifierRoundingMode.Nearest:
            default:
                return RoundNearest(finalValue);
        }
    }

    private static int RoundNearest(float value)
    {
        if (value >= 0f)
            return Mathf.FloorToInt(value + 0.5f);

        return Mathf.CeilToInt(value - 0.5f);
    }
}