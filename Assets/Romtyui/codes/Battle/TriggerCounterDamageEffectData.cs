using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Counter/Trigger Counter Damage")]
public class TriggerCounterDamageEffectData : CardEffectData, CardDescriptionValueProvider
{
    public override void Execute(CardResolveContext context)
    {
        if (context == null)
            return;

        if (context.source == null)
        {
            Debug.LogWarning("[TriggerCounterDamageEffectData] context.source 是 null");
            return;
        }

        if (context.target == null)
        {
            Debug.LogWarning("[TriggerCounterDamageEffectData] context.target 是 null");
            return;
        }

        if (context.target.currentHp <= 0)
            return;

        int counterAmount = context.source.GetStatus(StatusType.Counter);

        if (counterAmount <= 0)
        {
            Debug.Log("[TriggerCounterDamageEffectData] 出牌者目前沒有反擊層數");
            return;
        }

        int finalDamage = counterAmount;

        if (ModifierSystem.Instance != null)
        {
            ModifierQuery query = new ModifierQuery(
                ModifierType.CounterDamage,
                context.source,
                context.target,
                context.card,
                context.battleManager
            );

            query.roundingMode = ModifierRoundingMode.Nearest;
            query.clampResultToZero = true;

            finalDamage = ModifierSystem.Instance.ModifyInt(query, finalDamage);
        }

        if (finalDamage <= 0)
            return;

        context.target.TakeDamage(finalDamage);

        Debug.Log(
            $"[TriggerCounterDamageEffectData] {context.source.unitName} 立即觸發反擊，" +
            $"反擊層數 {counterAmount}，對 {context.target.unitName} 造成 {finalDamage} 傷害"
        );
    }

    public bool TryGetDescriptionValue(string key, CardResolveContext context, out int value)
    {
        value = 0;

        if (key != "damage" &&
            key != "damege" &&
            key != "counterDamage")
            return false;

        if (context == null || context.source == null)
            return true;

        int damage = context.source.GetStatus(StatusType.Counter);

        if (ModifierSystem.Instance != null)
        {
            ModifierQuery query = new ModifierQuery(
                ModifierType.CounterDamage,
                context.source,
                context.target,
                context.card,
                context.battleManager
            );

            query.roundingMode = ModifierRoundingMode.Nearest;
            query.clampResultToZero = true;

            damage = ModifierSystem.Instance.ModifyInt(query, damage);
        }

        value = Mathf.Max(0, damage);
        return true;
    }
}