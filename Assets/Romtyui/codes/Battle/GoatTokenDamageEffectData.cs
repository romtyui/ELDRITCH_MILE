using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Token/Goat Token Damage")]
public class GoatTokenDamageEffectData : CardEffectData, CardDescriptionValueProvider
{
    [Header("Damage")]
    [Tooltip("羊羔的基礎傷害，設定為原本傷害效果的數值")]
    public int amount = 6;

    public override void Execute(CardResolveContext context)
    {
        if (context == null) return;
        if (context.source == null) return;

        if (context.target == null)
        {
            Debug.LogWarning("[GoatTokenDamageEffectData] context.target 是 null，無法造成傷害");
            return;
        }

        if (context.target.currentHp <= 0) return;

        int baseDamage = GetRawDamage(context);
        bool ignoreBlock = context.card != null && context.card.hasGrantedIgnoreBlock;

        if (ignoreBlock)
        {
            int damage = context.source.ModifyOutgoingDamage(baseDamage);
            damage = context.target.ModifyIncomingDamage(damage);
            damage = Mathf.Max(0, damage);

            if (damage <= 0) return;

            context.target.TakeDamage(damage, context.source, true);

            Debug.Log($"[GoatTokenDamageEffectData] {context.source.unitName} 對 {context.target.unitName} 造成 {damage} 點穿透傷害");
        }
        else
        {
            context.source.DealDamageTo(context.target, baseDamage);
        }
    }

    private int GetRawDamage(CardResolveContext context)
    {
        int damage = Mathf.Max(0, amount);

        if (context == null || context.card == null || context.target == null) return damage;

        if (context.card.hasGrantedBreakBlock && context.target.block > 0)
        {
            float multiplier = Mathf.Max(1f, context.card.grantedBreakBlockMultiplier);
            damage = Mathf.RoundToInt(damage * multiplier);
        }

        return damage;
    }

    public bool TryGetDescriptionValue(string key, CardResolveContext context, out int value)
    {
        value = 0;

        if (key != "damage" && key != "damege" && key != "damage0" && key != "damege0") return false;

        int previewDamage = GetRawDamage(context);

        if (context != null && context.source != null) previewDamage = context.source.ModifyOutgoingDamage(previewDamage);
        if (context != null && context.target != null) previewDamage = context.target.ModifyIncomingDamage(previewDamage);

        value = Mathf.Max(0, previewDamage);
        return true;
    }
}