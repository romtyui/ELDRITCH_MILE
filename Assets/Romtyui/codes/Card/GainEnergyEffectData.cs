using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Gain Energy")]
public class GainEnergyEffectData : CardEffectData
{
    public int amount;

    public override void Execute(CardResolveContext context)
    {
        context.battleManager.GainEnergy(amount);
    }
}