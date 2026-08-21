using System.Collections.Generic;
using UnityEngine;

public abstract class RelicsModifierEffectData : RelicsEffectData
{
    public sealed override void Execute(RelicsUseContext context)
    {
        /*
         * Modifier 型遺物不透過 Trigger Execute。
         * 實際效果由 ModifierSystem 在需要計算數值時取得。
         */
    }

    public abstract void CollectModifiers(ModifierQuery query, List<ModifierValue> results, Object modifierSource);
}