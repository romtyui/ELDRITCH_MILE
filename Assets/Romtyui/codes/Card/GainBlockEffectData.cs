using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Gain Block")]
public class GainBlockEffectData : CardEffectData
{
    public int amount;

    public override void Execute(CardResolveContext context)
    {
        if (context.source == null) return;
        context.source.GainBlock(amount);
    }
}