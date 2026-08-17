using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Token/Used Token Scaled Damage")]
public class UsedTokenScaledDamageEffectData : CardEffectData, CardDescriptionValueProvider
{
    private enum PatchType
    {
        DamageEffectAmount,
        RandomEnemyMultiHitDamagePerHit,
        RandomTargetEachHitDamagePerHit
    }

    private class RuntimePatch
    {
        public CardEffectData effect;
        public PatchType patchType;
        public int originalValue;
    }

    [Header("Token")]
    [Tooltip("要計算的 Token ID。必須和卵 Token CardData 的 Token Id 一樣")]
    public string tokenId = "egg";

    [Header("Damage Bonus")]
    [Tooltip("每使用過 1 次指定 Token，讓後續傷害效果的每一下傷害增加多少")]
    public int damagePerUsedToken = 1;

    [Header("Patch Rule")]
    [Tooltip("只影響這個效果後面的傷害效果。建議打開，避免影響已經執行過的效果")]
    public bool onlyPatchEffectsAfterThis = true;

    [Tooltip("沒有使用過 Token 時是否不做任何事")]
    public bool skipWhenNoUsedToken = true;

    [Header("Description Preview")]
    [Tooltip("描述預覽用。當不在戰鬥中或讀不到 BattleDeck 時，先用這個數值預覽")]
    public int previewUsedTokenCount = 0;

    private bool isPatchActive;
    private List<RuntimePatch> activePatches = new List<RuntimePatch>();

    public override void Execute(CardResolveContext context)
    {
        if (context == null)
            return;

        if (context.card == null || context.card.data == null)
            return;

        if (context.card.data.effects == null)
            return;

        if (context.battleManager == null)
            return;

        if (isPatchActive)
        {
            Debug.Log("[UsedTokenScaledDamageEffectData] 傷害加成已經套用中，本次不重複加成");
            return;
        }

        int bonusPerHit = GetBonusDamagePerHit(context);

        if (bonusPerHit <= 0 && skipWhenNoUsedToken)
        {
            Debug.Log("[UsedTokenScaledDamageEffectData] 已使用 Token 次數為 0，不套用傷害加成");
            return;
        }

        List<RuntimePatch> patches = BuildPatches(context, bonusPerHit);

        if (patches.Count == 0)
        {
            Debug.LogWarning("[UsedTokenScaledDamageEffectData] 找不到可加成的傷害效果");
            return;
        }

        activePatches = patches;
        isPatchActive = true;

        ApplyPatches(activePatches, bonusPerHit);

        context.battleManager.StartCoroutine(RestorePatchesNextFrame());

        Debug.Log(
            $"[UsedTokenScaledDamageEffectData] 已套用傷害加成。tokenId={tokenId}, " +
            $"used={GetUsedTokenCount(context)}, bonusPerHit={bonusPerHit}, patches={patches.Count}"
        );
    }

    private List<RuntimePatch> BuildPatches(CardResolveContext context, int bonusPerHit)
    {
        List<RuntimePatch> patches = new List<RuntimePatch>();

        List<CardEffectData> effects = context.card.data.effects;

        int selfIndex = effects.IndexOf(this);

        for (int i = 0; i < effects.Count; i++)
        {
            if (onlyPatchEffectsAfterThis && selfIndex >= 0 && i <= selfIndex)
                continue;

            CardEffectData effect = effects[i];

            if (effect == null)
                continue;

            if (effect == this)
                continue;

            if (effect is DamageEffectData damageEffect)
            {
                patches.Add(new RuntimePatch
                {
                    effect = damageEffect,
                    patchType = PatchType.DamageEffectAmount,
                    originalValue = damageEffect.amount
                });

                continue;
            }

            if (effect is RandomEnemyMultiHitDamageEffectData multiHitEffect)
            {
                patches.Add(new RuntimePatch
                {
                    effect = multiHitEffect,
                    patchType = PatchType.RandomEnemyMultiHitDamagePerHit,
                    originalValue = multiHitEffect.damagePerHit
                });

                continue;
            }

            if (effect is RandomTargetEachHitDamageEffectData randomEachHitEffect)
            {
                patches.Add(new RuntimePatch
                {
                    effect = randomEachHitEffect,
                    patchType = PatchType.RandomTargetEachHitDamagePerHit,
                    originalValue = randomEachHitEffect.damagePerHit
                });

                continue;
            }
        }

        return patches;
    }

    private void ApplyPatches(List<RuntimePatch> patches, int bonusPerHit)
    {
        for (int i = 0; i < patches.Count; i++)
        {
            RuntimePatch patch = patches[i];

            if (patch == null || patch.effect == null)
                continue;

            int patchedValue = Mathf.Max(0, patch.originalValue + bonusPerHit);

            switch (patch.patchType)
            {
                case PatchType.DamageEffectAmount:
                    {
                        DamageEffectData damageEffect = patch.effect as DamageEffectData;

                        if (damageEffect != null)
                            damageEffect.amount = patchedValue;

                        break;
                    }

                case PatchType.RandomEnemyMultiHitDamagePerHit:
                    {
                        RandomEnemyMultiHitDamageEffectData multiHitEffect =
                            patch.effect as RandomEnemyMultiHitDamageEffectData;

                        if (multiHitEffect != null)
                            multiHitEffect.damagePerHit = patchedValue;

                        break;
                    }

                case PatchType.RandomTargetEachHitDamagePerHit:
                    {
                        RandomTargetEachHitDamageEffectData randomEachHitEffect =
                            patch.effect as RandomTargetEachHitDamageEffectData;

                        if (randomEachHitEffect != null)
                            randomEachHitEffect.damagePerHit = patchedValue;

                        break;
                    }
            }
        }
    }

    private IEnumerator RestorePatchesNextFrame()
    {
        yield return null;

        RestorePatches();

        isPatchActive = false;
        activePatches.Clear();

        Debug.Log("[UsedTokenScaledDamageEffectData] 已還原傷害效果原始數值");
    }

    private void RestorePatches()
    {
        for (int i = 0; i < activePatches.Count; i++)
        {
            RuntimePatch patch = activePatches[i];

            if (patch == null || patch.effect == null)
                continue;

            switch (patch.patchType)
            {
                case PatchType.DamageEffectAmount:
                    {
                        DamageEffectData damageEffect = patch.effect as DamageEffectData;

                        if (damageEffect != null)
                            damageEffect.amount = patch.originalValue;

                        break;
                    }

                case PatchType.RandomEnemyMultiHitDamagePerHit:
                    {
                        RandomEnemyMultiHitDamageEffectData multiHitEffect =
                            patch.effect as RandomEnemyMultiHitDamageEffectData;

                        if (multiHitEffect != null)
                            multiHitEffect.damagePerHit = patch.originalValue;

                        break;
                    }

                case PatchType.RandomTargetEachHitDamagePerHit:
                    {
                        RandomTargetEachHitDamageEffectData randomEachHitEffect =
                            patch.effect as RandomTargetEachHitDamageEffectData;

                        if (randomEachHitEffect != null)
                            randomEachHitEffect.damagePerHit = patch.originalValue;

                        break;
                    }
            }
        }
    }

    private int GetUsedTokenCount(CardResolveContext context)
    {
        if (context == null)
            return Mathf.Max(0, previewUsedTokenCount);

        if (context.battleManager == null)
            return Mathf.Max(0, previewUsedTokenCount);

        return context.battleManager.GetUsedTokenCount(tokenId);
    }

    private int GetBonusDamagePerHit(CardResolveContext context)
    {
        int usedTokenCount = GetUsedTokenCount(context);
        int bonusPerToken = Mathf.Max(0, damagePerUsedToken);

        return usedTokenCount * bonusPerToken;
    }

    private int GetFirstBaseDamage(CardResolveContext context)
    {
        if (context == null || context.card == null || context.card.data == null)
            return 0;

        if (context.card.data.effects == null)
            return 0;

        for (int i = 0; i < context.card.data.effects.Count; i++)
        {
            CardEffectData effect = context.card.data.effects[i];

            if (effect == null)
                continue;

            if (effect == this)
                continue;

            if (effect is DamageEffectData damageEffect)
                return GetOriginalOrCurrentDamageAmount(damageEffect);

            if (effect is RandomEnemyMultiHitDamageEffectData multiHitEffect)
                return GetOriginalOrCurrentMultiHitDamagePerHit(multiHitEffect);

            if (effect is RandomTargetEachHitDamageEffectData randomEachHitEffect)
                return GetOriginalOrCurrentRandomEachHitDamagePerHit(randomEachHitEffect);
        }

        return 0;
    }

    private int GetFirstHitCount(CardResolveContext context)
    {
        if (context == null || context.card == null || context.card.data == null)
            return 1;

        if (context.card.data.effects == null)
            return 1;

        for (int i = 0; i < context.card.data.effects.Count; i++)
        {
            CardEffectData effect = context.card.data.effects[i];

            if (effect == null)
                continue;

            if (effect is RandomEnemyMultiHitDamageEffectData multiHitEffect)
                return Mathf.Max(1, multiHitEffect.hitCount);

            if (effect is RandomTargetEachHitDamageEffectData randomEachHitEffect)
                return Mathf.Max(1, randomEachHitEffect.hitCount);
        }

        return 1;
    }

    private int GetScaledDamagePerHit(CardResolveContext context)
    {
        int damage = GetFirstBaseDamage(context) + GetBonusDamagePerHit(context);

        if (context != null && context.source != null)
            damage = context.source.ModifyOutgoingDamage(damage);

        if (context != null && context.target != null)
            damage = context.target.ModifyIncomingDamage(damage);

        return Mathf.Max(0, damage);
    }

    private int GetScaledTotalDamage(CardResolveContext context)
    {
        int damagePerHit = GetScaledDamagePerHit(context);
        int hitCount = GetFirstHitCount(context);

        return damagePerHit * hitCount;
    }

    private int GetOriginalOrCurrentDamageAmount(DamageEffectData effect)
    {
        if (effect == null)
            return 0;

        RuntimePatch patch = FindActivePatch(effect, PatchType.DamageEffectAmount);

        if (patch != null)
            return Mathf.Max(0, patch.originalValue);

        return Mathf.Max(0, effect.amount);
    }

    private int GetOriginalOrCurrentMultiHitDamagePerHit(RandomEnemyMultiHitDamageEffectData effect)
    {
        if (effect == null)
            return 0;

        RuntimePatch patch = FindActivePatch(effect, PatchType.RandomEnemyMultiHitDamagePerHit);

        if (patch != null)
            return Mathf.Max(0, patch.originalValue);

        return Mathf.Max(0, effect.damagePerHit);
    }

    private int GetOriginalOrCurrentRandomEachHitDamagePerHit(RandomTargetEachHitDamageEffectData effect)
    {
        if (effect == null)
            return 0;

        RuntimePatch patch = FindActivePatch(effect, PatchType.RandomTargetEachHitDamagePerHit);

        if (patch != null)
            return Mathf.Max(0, patch.originalValue);

        return Mathf.Max(0, effect.damagePerHit);
    }

    private RuntimePatch FindActivePatch(CardEffectData effect, PatchType patchType)
    {
        if (!isPatchActive)
            return null;

        for (int i = 0; i < activePatches.Count; i++)
        {
            RuntimePatch patch = activePatches[i];

            if (patch == null)
                continue;

            if (patch.effect == effect && patch.patchType == patchType)
                return patch;
        }

        return null;
    }

    public bool TryGetDescriptionValue(string key, CardResolveContext context, out int value)
    {
        value = 0;

        if (key == "usedEggCount" ||
            key == "usedTokenCount" ||
            key == "playedTokenCount" ||
            key == "eggCount" ||
            key == "已使用過卵的次數")
        {
            value = GetUsedTokenCount(context);
            return true;
        }

        if (key == "bonusDamage" ||
            key == "usedTokenBonusDamage" ||
            key == "tokenBonusDamage" ||
            key == "額外傷害")
        {
            value = GetBonusDamagePerHit(context);
            return true;
        }

        if (key == "damage" ||
            key == "damege" ||
            key == "scaledDamage" ||
            key == "damagePerHit" ||
            key == "finalDamage" ||
            key == "總傷害")
        {
            value = GetScaledDamagePerHit(context);
            return true;
        }

        if (key == "hitCount" ||
            key == "repeatCount" ||
            key == "bowHitCount" ||
            key == "重複次數")
        {
            value = GetFirstHitCount(context);
            return true;
        }

        if (key == "totalDamageAllHits" ||
            key == "allHitDamage" ||
            key == "總段數傷害")
        {
            value = GetScaledTotalDamage(context);
            return true;
        }

        return false;
    }
}