using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Damage")]
public class DamageEffectData : CardEffectData
{
    public int amount;

    public override void Execute(CardResolveContext context)
    {
        if (context == null) return;
        if (context.source == null) return;
        if (context.target == null) return;

        context.source.DealDamageTo(context.target, amount);
    }
}