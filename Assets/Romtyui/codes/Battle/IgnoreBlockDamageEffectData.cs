using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Damage/Ignore Block Damage")]
public class IgnoreBlockDamageEffectData : CardEffectData, CardDescriptionValueProvider
{
    [Header("Damage")]
    public int amount = 5;

    public override void Execute(CardResolveContext context)
    {
        if (context == null)
            return;

        if (context.source == null)
            return;

        if (context.target == null)
        {
            Debug.LogWarning("[IgnoreBlockDamageEffectData] context.target 是 null，無法造成傷害");
            return;
        }

        int damage = amount;

        damage = context.source.ModifyOutgoingDamage(damage);
        damage = context.target.ModifyIncomingDamage(damage);

        damage = Mathf.Max(0, damage);

        if (damage <= 0)
            return;

        context.target.TakeDamage(damage, context.source, true);

        Debug.Log(
            $"[IgnoreBlockDamageEffectData] {context.source.unitName} 對 {context.target.unitName} 造成 {damage} 點穿透護盾傷害"
        );
    }

    public bool TryGetDescriptionValue(string key, CardResolveContext context, out int value)
    {
        value = 0;

        if (key != "damage" &&
            key != "damege" &&
            key != "damage0" &&
            key != "damege0")
            return false;

        int previewDamage = amount;

        if (context != null && context.source != null)
            previewDamage = context.source.ModifyOutgoingDamage(previewDamage);

        if (context != null && context.target != null)
            previewDamage = context.target.ModifyIncomingDamage(previewDamage);

        value = Mathf.Max(0, previewDamage);
        return true;
    }
}