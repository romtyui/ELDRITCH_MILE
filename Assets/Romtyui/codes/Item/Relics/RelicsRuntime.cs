using System.Collections.Generic;
using UnityEngine;

public class RelicsRuntime : MonoBehaviour
{
    // =========================================================
    // References
    // =========================================================

    [Header("References")]
    [Tooltip("玩家目前持有的遺物 Inventory。")]
    public RelicsInventory relicsInventory;


    // =========================================================
    // Trigger
    // =========================================================

    /// <summary>
    /// 執行指定時間點的所有遺物效果。
    /// </summary>
    public void Trigger(
        RelicsTriggerType triggerType,
        RelicsUseContext context
    )
    {
        // =====================================================
        // 基本檢查
        // =====================================================

        if (relicsInventory == null)
        {
            Debug.LogWarning(
                "[RelicsRuntime] " +
                "RelicsInventory 沒有指定"
            );

            return;
        }


        if (context == null)
        {
            Debug.LogWarning(
                "[RelicsRuntime] " +
                "RelicsUseContext 是 null"
            );

            return;
        }


        context.triggerType =
            triggerType;


        IReadOnlyList<ScriptableObject>
            currentRelics =
                relicsInventory.CurrentRelics;


        if (currentRelics == null)
            return;


        // =====================================================
        // 遍歷目前持有的所有 Relic
        // =====================================================

        for (
            int relicIndex = 0;
            relicIndex < currentRelics.Count;
            relicIndex++
        )
        {
            ScriptableObject relicObject =
                currentRelics[relicIndex];


            if (relicObject == null)
                continue;


            // =================================================
            // 取得這個 Relic 的 Effects
            // =================================================

            IRelicsEffectSource effectSource =
                relicObject as IRelicsEffectSource;


            if (effectSource == null)
            {
                Debug.LogWarning(
                    $"[RelicsRuntime] " +
                    $"遺物 {relicObject.name} " +
                    $"沒有實作 IRelicsEffectSource，" +
                    $"因此無法取得 Relics Effects。"
                );

                continue;
            }


            IReadOnlyList<RelicsEffectData>
                effects =
                    effectSource.RelicsEffects;


            if (effects == null)
                continue;


            // =================================================
            // 遍歷這個 Relic 的所有 Effect
            // =================================================

            for (
                int effectIndex = 0;
                effectIndex < effects.Count;
                effectIndex++
            )
            {
                RelicsEffectData effect =
                    effects[effectIndex];


                if (effect == null)
                    continue;


                // =============================================
                // Trigger 不符合
                // =============================================

                if (!effect.CanTrigger(
                        triggerType))
                {
                    continue;
                }


                // =============================================
                // Trigger 符合
                // =============================================

                Debug.Log(
                    $"[RelicsRuntime] " +
                    $"Trigger = {triggerType}，" +
                    $"Relic = {relicObject.name}，" +
                    $"Effect = {effect.name}"
                );


                effect.Execute(
                    context
                );
            }
        }
    }
}