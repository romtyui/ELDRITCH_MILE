using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core.ProbabilityDialogue
{
    /// <summary>
    /// 對話事件的登記處。**附件《對話》開頭那句話的實作**：
    /// 「進入位於地圖上的對話節點觸發，隨後根據當前同行角色中隨機抽取相關事件進行。」
    ///
    /// 【為什麼不用現成的 <see cref="EventLibrary"/>】那一支收的是 <see cref="EventData"/>
    /// （八個特殊事件那一套，有旗標、條件、侵蝕度效果）。機率對話是另一種資產，
    /// 型別對不上；硬要共用就得把 EventLibrary 泛型化，動到已經驗收的東西。
    ///
    /// 【形狀跟 EventLibrary 一樣】候選 → 濾掉不合格的 → 依權重挑一個。
    /// 差別是這裡多一道「同行角色」的過濾，少了條件與 globalChance ——
    /// 對話節點本來就是「一定有對話」，不像事件節點是「有機率有事件」。
    /// </summary>
    [CreateAssetMenu(fileName = "PDialogueLibrary", menuName = "Eldritch/機率對話/對話事件庫")]
    public class ProbabilityDialogueLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public ProbabilityDialogueData dialogue;

            [Tooltip("權重。**Unity 用 + 新增 List 元素時會零填充** —— 記得改成 1 以上，\n" +
                     "否則這一條永遠抽不到，而且不會有任何錯誤訊息")]
            [Min(0f)] public float weight = 1f;

            [Tooltip("整場 run 只出現一次。\n\n" +
                     "附件把「已觸發的事件在同一輪次中會不會再觸發」標為未確定 ——\n" +
                     "先做成一個勾勾，定案時改資料就好，不必改程式")]
            public bool once = true;
        }

        [Tooltip("所有對話事件。順序不影響行為")]
        public List<Entry> entries = new List<Entry>();

        [Tooltip("把每次的挑選過程印到 Console。查「為什麼那段對話沒出來」時打開")]
        public bool verbose = false;

        /// <summary>
        /// 這個事件觸發過了沒有 —— 用 RunContext 的旗標記，跟 EventData 同一個做法。
        /// </summary>
        public static string PlayedFlag(ProbabilityDialogueData d) =>
            d != null && !string.IsNullOrEmpty(d.eventId) ? "pdialogue." + d.eventId : "";

        /// <summary>
        /// 挑一段對話。挑不到回 null。
        ///
        /// ⚠️ **這支不改任何狀態** —— 標記成「演過了」是對話真的演完之後的事
        /// （<see cref="MarkPlayed"/>），否則玩家中途離開，那一段就再也不會出現。
        /// 這跟 <see cref="EventLibrary.Pick"/> 是同一個規矩。
        /// </summary>
        /// <param name="companionIds">
        /// 目前的同行角色 id。**留空 = 不過濾**（現在還沒有隊伍系統，
        /// 所以正常情況就是留空，全部事件一起抽）。
        /// </param>
        public ProbabilityDialogueData Pick(
            RunContext run, System.Random rng, IList<string> companionIds = null)
        {
            if (rng == null) return null;

            var candidates = new List<Entry>();
            float total = 0f;

            for (int i = 0; i < entries.Count; i++)
            {
                Entry e = entries[i];
                if (e == null || e.dialogue == null) continue;

                if (string.IsNullOrEmpty(e.dialogue.eventId))
                {
                    Debug.LogWarning(
                        $"[對話庫]「{e.dialogue.name}」沒有填 Event Id，記不了「演過沒」，跳過。\n" +
                        "⚠️ 而且亂數種子也是用 eventId 算的 —— 空字串會讓每一段對話共用同一手牌。",
                        e.dialogue);
                    continue;
                }

                if (e.once && run != null && run.HasFlag(PlayedFlag(e.dialogue)))
                {
                    if (verbose) Debug.Log($"[對話庫]「{e.dialogue.eventId}」演過了");
                    continue;
                }

                // 同行角色過濾。**沒有指定同行角色時不過濾** ——
                // 反過來做的話（沒隊伍就一段都不給）會在隊伍系統做好之前
                // 讓所有對話節點都變成空的
                if (companionIds != null && companionIds.Count > 0
                    && !string.IsNullOrEmpty(e.dialogue.npcId)
                    && !companionIds.Contains(e.dialogue.npcId))
                {
                    if (verbose) Debug.Log($"[對話庫]「{e.dialogue.eventId}」的角色不在隊伍裡");
                    continue;
                }

                if (e.weight <= 0f) continue;

                candidates.Add(e);
                total += e.weight;
            }

            if (candidates.Count == 0)
            {
                if (verbose) Debug.Log("[對話庫] 沒有任何合格的對話");
                return null;
            }

            double roll = rng.NextDouble() * total;
            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= candidates[i].weight;
                if (roll <= 0) return candidates[i].dialogue;
            }

            return candidates[candidates.Count - 1].dialogue;
        }

        /// <summary>演完了。**成功或失敗都算演過** —— 同一段對話重來一次沒有意義。</summary>
        public static void MarkPlayed(RunContext run, ProbabilityDialogueData d)
        {
            string flag = PlayedFlag(d);
            if (run == null || string.IsNullOrEmpty(flag)) return;
            run.SetFlag(flag);
        }
    }
}
