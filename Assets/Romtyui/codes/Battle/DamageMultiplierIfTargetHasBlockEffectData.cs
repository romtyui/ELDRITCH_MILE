using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Damage/Damage Multiplier If Target Has Block")]
public class DamageMultiplierIfTargetHasBlockEffectData : CardEffectData, CardDescriptionValueProvider
{
    [Header("Damage")]
    [Tooltip("基礎傷害")]
    public int amount = 5;

    [Header("Multiplier")]
    [Tooltip("目標有護盾時的傷害倍率。例如 2 = 2 倍")]
    public float multiplier = 2f;

    public override void Execute(CardResolveContext context)
    {
        if (context == null)
            return;

        if (context.source == null)
            return;

        if (context.target == null)
        {
            Debug.LogWarning("[DamageMultiplierIfTargetHasBlockEffectData] context.target 是 null，無法造成傷害");
            return;
        }

        if (context.target.currentHp <= 0)
            return;

        bool targetHasBlock = context.target.block > 0;

        int baseDamage = Mathf.Max(0, amount);

        if (targetHasBlock)
        {
            baseDamage = Mathf.RoundToInt(
                baseDamage * Mathf.Max(0f, multiplier)
            );
        }

        context.source.DealDamageTo(context.target, baseDamage);

        Debug.Log(
            $"[DamageMultiplierIfTargetHasBlockEffectData] " +
            $"目標 {context.target.unitName} 是否有護盾：{targetHasBlock}，" +
            $"本次基礎傷害：{baseDamage}"
        );
    }

    public bool TryGetDescriptionValue(string key, CardResolveContext context, out int value)
    {
        value = 0;

        if (key != "damage" &&
            key != "damege" &&
            key != "damage0" &&
            key != "damege0")
        {
            return false;
        }

        int previewDamage = Mathf.Max(0, amount);

        if (context != null &&
            context.target != null &&
            context.target.block > 0)
        {
            previewDamage = Mathf.RoundToInt(
                previewDamage * Mathf.Max(0f, multiplier)
            );
        }

        if (context != null && context.source != null)
            previewDamage = context.source.ModifyOutgoingDamage(previewDamage);

        if (context != null && context.target != null)
            previewDamage = context.target.ModifyIncomingDamage(previewDamage);

        value = Mathf.Max(0, previewDamage);
        return true;
    }
}