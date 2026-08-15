using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Damage")]
public class DamageEffectData : CardEffectData, CardDescriptionValueProvider
{
    public int amount;

    public override void Execute(CardResolveContext context)
    {
        if (context == null) return;
        if (context.source == null) return;
        if (context.target == null) return;

        context.source.DealDamageTo(context.target, amount);
    }

    public bool TryGetDescriptionValue(
        string key,
        CardResolveContext context,
        out int value
    )
    {
        value = 0;

        if (key != "damage")
            return false;

        value = GetPreviewDamage(context);
        return true;
    }

    public int GetPreviewDamage(CardResolveContext context)
    {
        int damage = amount;

        if (context != null && context.source != null)
            damage = context.source.ModifyOutgoingDamage(damage);

        if (context != null && context.target != null)
            damage = context.target.ModifyIncomingDamage(damage);

        return Mathf.Max(0, damage);
    }
}