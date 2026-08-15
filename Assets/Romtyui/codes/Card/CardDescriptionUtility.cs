using System.Collections.Generic;
using UnityEngine;

public static class CardDescriptionUtility
{
    public static string BuildDescription(
        CardInstance card,
        BattleUnit user,
        BattleUnit target
    )
    {
        if (card == null || card.data == null)
            return "";

        string text = card.data.description;

        if (string.IsNullOrWhiteSpace(text))
            return "";

        int damageIndex = 0;
        int blockIndex = 0;

        List<CardEffectData> effects = card.data.effects;

        if (effects == null)
            return text;

        for (int i = 0; i < effects.Count; i++)
        {
            CardEffectData effect = effects[i];

            if (effect == null)
                continue;

            if (effect is DamageEffectData damageEffect)
            {
                int value = CalculateDamagePreview(card, damageEffect, user, target);

                if (damageIndex == 0)
                {
                    text = text.Replace("{damage}", value.ToString());
                    text = text.Replace("{damege}", value.ToString());
                }

                text = text.Replace("{damage" + damageIndex + "}", value.ToString());
                text = text.Replace("{damege" + damageIndex + "}", value.ToString());

                damageIndex++;
            }
            else if (effect is GainBlockEffectData blockEffect)
            {
                int value = CalculateBlockPreview(blockEffect, user);

                if (blockIndex == 0)
                    text = text.Replace("{block}", value.ToString());

                text = text.Replace("{block" + blockIndex + "}", value.ToString());

                blockIndex++;
            }
        }

        return text;
    }

    private static int CalculateDamagePreview(
        CardInstance card,
        DamageEffectData damageEffect,
        BattleUnit user,
        BattleUnit target
    )
    {
        if (damageEffect == null)
            return 0;

        int damage = damageEffect.amount;

        if (user != null)
            damage = user.ModifyOutgoingDamage(damage);

        bool shouldCalculateTargetStatus =
            card != null &&
            card.data != null &&
            card.data.targetType == TargetType.SingleEnemy &&
            target != null;

        if (shouldCalculateTargetStatus)
            damage = target.ModifyIncomingDamage(damage);

        return Mathf.Max(0, damage);
    }

    private static int CalculateBlockPreview(
        GainBlockEffectData blockEffect,
        BattleUnit user
    )
    {
        if (blockEffect == null)
            return 0;

        int block = blockEffect.amount;

        if (user != null)
            block = user.ModifyBlockGain(block);

        return Mathf.Max(0, block);
    }
}