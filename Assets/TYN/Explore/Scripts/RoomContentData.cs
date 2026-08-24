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

            [Tooltip("整個房間最多出現幾個。0 = 不限。\n" +
                     "⚠️ 這是**單一條目**的上限。四種寶箱各設 2 會變成總共最多 8 個 ——\n" +
                     "要限制「寶箱總數」請改用下面的 group ＋ 群組配額")]
            [Min(0)] public int maxPerRoom = 0;

            [Tooltip("群組名。同一個群組的條目**共用一份配額**（見下方「群組配額」）。\n\n" +
                     "留空 = 不屬於任何群組，行為跟以前完全一樣 ——\n" +
                     "場景互動（爐子／衣櫃／桌子…）就留空，這樣它們與寶箱互不佔位。")]
            public string group = "";
        }

        /// <summary>
        /// 「這間房要有幾個寶箱」——**配額，不是上限**。
        ///
        /// 【為什麼上限不夠】`maxPerRoom` 只能防止「太多」。實際會出現幾個，
        /// 是其他條目權重的副作用：家具權重高就常常 0 個，寶箱權重高就常常滿額。
        /// 但企劃要的「通常 1 個、最多 2 個而且很稀有」是一個**分布**，
        /// 那必須直接指定，沒辦法靠上限反推。
        ///
        /// 所以先擲出數量，再去填 —— 跟 <see cref="EldritchMile.Core.EncounterPlanner"/>
        /// 的「保證出現」是同一個形狀：**配額先佔位，剩下的才隨機**。
        /// </summary>
        [Serializable]
        public class GroupQuota
        {
            [Tooltip("要控制哪一個群組（對應 Entry.group）")]
            public string group = "chest";

            [Tooltip("各種數量的權重。**第 N 格 = 出現 N 個的權重**。\n\n" +
                     "例：[0, 90, 10] = 「不會沒有、九成一個、一成兩個」\n" +
                     "　　[35, 65]    = 「三成五沒有、六成五一個」\n\n" +
                     "這張表直接就是企劃那句話，改機率不用動程式")]
            public List<float> countWeights = new List<float> { 0f, 90f, 10f };
        }

        [Header("可生成的內容")]
        public List<Entry> entries = new List<Entry>();

        [Header("群組配額（先擲數量，再佔位）")]
        [Tooltip("留空 = 沒有任何群組限制。\n" +
                 "有配額的群組會**先**被安排進隨機的位子，剩下的位子才走一般的權重抽選")]
        public List<GroupQuota> groupQuotas = new List<GroupQuota>();

        [Header("數量控制")]
        [Tooltip("本房間至少要填滿幾個位子")]
        [Min(0)] public int minFilled = 2;

        [Tooltip("本房間最多填滿幾個位子。0 = 不限（受 slot 數量與各自的 fillChance 限制）")]
        [Min(0)] public int maxFilled = 5;

        /// <summary>
        /// 擲出某個群組這次要出現幾個。沒有設定配額的群組回 -1（＝不限制）。
        /// </summary>
        public int RollQuota(string group, System.Random rng)
        {
            if (string.IsNullOrEmpty(group) || groupQuotas == null) return -1;

            GroupQuota q = groupQuotas.Find(x => x != null && x.group == group);
            if (q == null || q.countWeights == null || q.countWeights.Count == 0) return -1;

            float total = 0f;
            for (int i = 0; i < q.countWeights.Count; i++) total += Mathf.Max(0f, q.countWeights[i]);

            if (total <= 0f)
            {
                Debug.LogWarning(
                    $"[房間內容] {name} 的群組「{group}」配額權重全是 0，當成「不出現」。\n" +
                    "⚠️ Unity 新增 List 元素會零填充 —— 記得填數字。", this);
                return 0;
            }

            double roll = rng.NextDouble() * total;
            for (int i = 0; i < q.countWeights.Count; i++)
            {
                roll -= Mathf.Max(0f, q.countWeights[i]);
                if (roll <= 0) return i;
            }

            return q.countWeights.Count - 1;
        }

        /// <summary>這張表上有配額的群組有哪些。</summary>
        public IEnumerable<string> QuotaGroups
        {
            get
            {
                if (groupQuotas == null) yield break;
                for (int i = 0; i < groupQuotas.Count; i++)
                    if (groupQuotas[i] != null && !string.IsNullOrEmpty(groupQuotas[i].group))
                        yield return groupQuotas[i].group;
            }
        }

        /// <summary>
        /// 依權重抽一個能放進指定位子的內容。抽不到回傳 null。
        /// </summary>
        /// <param name="onlyGroup">只考慮這個群組的條目。留空 = 全部（但會排除已滿額的群組）。</param>
        /// <param name="groupFilled">各群組已經放了幾個。</param>
        /// <param name="groupQuota">各群組這次的配額（-1 = 不限）。</param>
        public Entry PickFor(
            SpawnSlot slot, System.Random rng, Dictionary<Entry, int> usedCount,
            string onlyGroup = null,
            Dictionary<string, int> groupFilled = null,
            Dictionary<string, int> groupQuota = null)
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

                // 配額階段：只考慮這個群組
                if (!string.IsNullOrEmpty(onlyGroup))
                {
                    if (e.group != onlyGroup) continue;
                }
                // 一般階段：已經滿額的群組不再參加
                else if (!string.IsNullOrEmpty(e.group)
                         && groupQuota != null && groupQuota.TryGetValue(e.group, out int quota) && quota >= 0)
                {
                    int already = groupFilled != null && groupFilled.TryGetValue(e.group, out int gf) ? gf : 0;
                    if (already >= quota) continue;
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
