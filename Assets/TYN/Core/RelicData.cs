using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 遺物（收藏品）。**不是點來用的**：戰鬥開始時自動生效，一件只能有一個。
    ///
    /// 【為什麼從 ItemData 拆出來】（2026-11-06，Romtyui 要求）見 <see cref="FoodData"/> 的說明，同一件事。
    ///
    /// ⚠️ `Curio` 標籤先留著：戰利品表與 Romtyui 大廳的倉庫還在用標籤篩選。
    /// </summary>
    [CreateAssetMenu(fileName = "Item_relic_", menuName = "Eldritch/Relic")]
    public class RelicData : ItemData, IRelicsEffectSource, ISerializationCallbackReceiver
    {
        //新增時間26/10/06 by romtyui（2026-11-06 隨拆分從 ItemData 搬來）
        [Header("物品等級")]
        [Min(1)]
        [Tooltip("物品本身的等級。")]
        public int tire = 1;
        //新增時間26/10/06 by romtyui

        [Header("遺物效果（戰鬥中被動觸發）")]
        [Tooltip("這件收藏品在戰鬥裡的效果。**可以放好幾個**，全部都會生效。\n\n" +
                 "兩種效果都放這裡，戰鬥端會自己分：\n" +
                 "  · 觸發型 —— BattleStart／回合開始／回合結束／出牌時自動執行（見 RelicsTriggerType）\n" +
                 "  · 數值型 —— 繼承 RelicsModifierEffectData（例：Stat Modifier，受到治療 +10%），\n" +
                 "    不會被「觸發」，而是 ModifierSystem 算傷害／治療／護甲時來問\n\n" +
                 "⚠️ 戰鬥開始時 BattleStageController 會把身上的遺物\n" +
                 "（**這個 RelicData 本身**，不是效果）送進 RelicsInventory。\n\n" +
                 "留空 = 這件收藏品目前只是收藏品，沒有效果")]
        public List<RelicsEffectData> relicEffects = new List<RelicsEffectData>();

        // 舊版是單一欄位 `relicEffect`。改成清單時用這個接住已經填過的值，
        // 讀進來之後在 OnAfterDeserialize 搬進清單 —— 見那裡的說明
        [SerializeField, HideInInspector, UnityEngine.Serialization.FormerlySerializedAs("relicEffect")]
        private RelicsEffectData legacyRelicEffect;

        /// <summary>
        /// 第一個遺物效果。**只為了相容舊程式而留著**（判斷「這件有沒有效果」）。
        /// 新程式請用 <see cref="relicEffects"/> 或 <see cref="RelicsEffects"/>。
        /// </summary>
        public RelicsEffectData relicEffect
        {
            get
            {
                if (relicEffects == null) return null;
                for (int i = 0; i < relicEffects.Count; i++)
                    if (relicEffects[i] != null) return relicEffects[i];
                return null;
            }
        }

        /// <summary>
        /// Romtyui 的 <see cref="RelicsRuntime"/> 就是靠這個介面讀遺物的。
        ///
        /// 【為什麼要實作】他的 `RelicsInventory` 收的是 ScriptableObject，`RelicsRuntime`
        /// 拿到每一件都會做 `as IRelicsEffectSource` —— 不是這個介面的話**整件跳過**。
        /// </summary>
        public IReadOnlyList<RelicsEffectData> RelicsEffects => relicEffects;

        /// <summary>**遺物是唯一的** —— 一件只能有一個，不能疊。（2026-10-13 定案）</summary>
        public override bool IsUnique => true;

        // ==========================================
        // 舊資料搬家：relicEffect（單一）→ relicEffects（清單）
        // ==========================================
        //
        // ⚠️ 這個 callback 可能在**載入執行緒**上跑。UnityEngine.Object 的 `==` 與
        //    List.Contains（走 Equals）會呼叫引擎內部的存活檢查，非主執行緒會報錯 ——
        //    所以這裡全部用 ReferenceEquals，只做純 C# 的參考比較。

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            if (ReferenceEquals(legacyRelicEffect, null)) return;

            if (relicEffects == null) relicEffects = new List<RelicsEffectData>();

            bool already = false;
            for (int i = 0; i < relicEffects.Count; i++)
                if (ReferenceEquals(relicEffects[i], legacyRelicEffect)) { already = true; break; }

            if (!already) relicEffects.Insert(0, legacyRelicEffect);
            legacyRelicEffect = null;
        }
    }
}
