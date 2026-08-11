using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Explore
{
    /// <summary>
    /// 一個房間可以生成哪些內容。C6 的「隨機」由這裡定義。
    ///
    /// 做成 ScriptableObject，因為同一份內容表可以給多個房間共用
    /// （例如「漁村室內通用內容」），調整時也不必開場景。
    /// </summary>
    [CreateAssetMenu(fileName = "RoomContent", menuName = "Eldritch/Room Content")]
    public class RoomContentData : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("要生成的互動物件 prefab（寶箱、路人、可調查物…）")]
            public GameObject prefab;

            [Tooltip("權重。數字越大越容易被抽到")]
            [Min(0f)] public float weight = 1f;

            [Tooltip("這個內容適合放在哪種位子")]
            public SpawnSlot.Placement placement = SpawnSlot.Placement.Any;

            [Tooltip("對應 SpawnSlot.contentTag。留空 = 不限")]
            public string tag = "";

            [Tooltip("整個房間最多出現幾個。0 = 不限")]
            [Min(0)] public int maxPerRoom = 0;
        }

        [Header("可生成的內容")]
        public List<Entry> entries = new List<Entry>();

        [Header("數量控制")]
        [Tooltip("本房間至少要填滿幾個位子")]
        [Min(0)] public int minFilled = 2;

        [Tooltip("本房間最多填滿幾個位子。0 = 不限（受 slot 數量與各自的 fillChance 限制）")]
        [Min(0)] public int maxFilled = 5;

        /// <summary>
        /// 依權重抽一個能放進指定位子的內容。抽不到回傳 null。
        /// </summary>
        public Entry PickFor(SpawnSlot slot, System.Random rng, Dictionary<Entry, int> usedCount)
        {
            var candidates = new List<Entry>();
            float total = 0f;
            int zeroWeight = 0;

            for (int i = 0; i < entries.Count; i++)
            {
                Entry e = entries[i];
                if (e == null || e.prefab == null) continue;

                // ⚠️ Unity 用 Inspector 的 + 新增 List 元素時會零填充，
                //    不會套用程式裡的 weight = 1f，所以這裡特別點出來。
                if (e.weight <= 0f) { zeroWeight++; continue; }

                if (!slot.Accepts(e.placement, e.tag)) continue;

                if (e.maxPerRoom > 0 &&
                    usedCount.TryGetValue(e, out int used) &&
                    used >= e.maxPerRoom)
                {
                    continue;
                }

                candidates.Add(e);
                total += e.weight;
            }

            if (candidates.Count == 0 || total <= 0f)
            {
                if (zeroWeight > 0)
                {
                    Debug.LogWarning(
                        $"[房間內容] {name} 有 {zeroWeight} 筆條目的 Weight 是 0 而被跳過。\n" +
                        "⚠️ Unity 的 Inspector 用 + 新增 List 元素時會零填充 —— 請手動把 Weight 改成 1。"
                    );
                }
                return null;
            }

            double roll = rng.NextDouble() * total;
            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= candidates[i].weight;
                if (roll <= 0) return candidates[i];
            }

            return candidates[candidates.Count - 1];
        }
    }
}
