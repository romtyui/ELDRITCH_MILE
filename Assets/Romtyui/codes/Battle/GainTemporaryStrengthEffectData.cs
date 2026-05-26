using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Gain Temporary Strength")]
public class GainTemporaryStrengthEffectData : CardEffectData
{
    public int amount = 1;

    public override void Execute(CardResolveContext context)
    {
        if (context == null) return;
        if (context.source == null) return;

        context.source.ApplyStatus(StatusType.TemporaryStrength, amount);

        Debug.Log($"{context.source.unitName} 獲得臨時力量 {amount}");
    }
}