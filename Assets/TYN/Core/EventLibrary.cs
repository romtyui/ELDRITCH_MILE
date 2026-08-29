using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 所有事件的登記處，兼「這一站要不要觸發事件」的裁判。
    ///
    /// 【形狀跟 LootTable 一樣】候選 → 濾掉條件不成立的 → 依權重隨機挑一個。
    /// 差別只在候選是事件、而且多一道「未觸發過」與「機率」。
    ///
    /// 【機率為什麼分兩層】
    ///   · `EventLibrary.globalChance` —— 「這一站到底要不要有事件」
    ///   · `EventData.chance` —— 「就算選到我，我也只有 30% 會真的發生」
    ///
    /// 大綱兩種都有：整體是「**有概率**觸發事件」，而《螺湮的祝福》另外寫了
    /// 「打倒半魚人祭司后 **30% 概率**」。壓成一層的話就表達不出後者。
    ///
    /// 【挑的順序】條件 → **優先序** → 權重 → 事件自己的機率。
    /// 優先序是「哪一批先輪到」，權重是「同一批之內誰被抽到」——
    /// 只用權重做不出「一定先出」（權重再高也只是機率高），
    /// 而《暴食之深淵》那種開場介紹被壓到第三站才出現就失去意義了。
    /// </summary>
    [CreateAssetMenu(fileName = "EventLibrary", menuName = "Eldritch/Event Library")]
    public class EventLibrary : ScriptableObject
    {
        [Tooltip("所有事件。順序不影響行為")]
        public List<EventData> events = new List<EventData>();

        [Tooltip("每進一個節點，有多少機率「會有事件」。\n" +
                 "1 = 只要有合格的事件就一定觸發（做教學或測試時用）")]
        [Range(0f, 1f)] public float globalChance = 0.35f;

        [Tooltip("把每次的判定過程印到 Console。查「為什麼那個事件沒出來」時打開")]
        public bool verbose = false;

        /// <summary>
        /// 挑一個這一站要觸發的事件。沒有就回 null。
        ///
        /// ⚠️ **這支不會改動任何狀態** —— 不立旗標、不扣東西。
        /// 「標記成觸發過」是事件真的播完之後的事（見 <see cref="MarkTriggered"/>），
        /// 否則玩家中途離開，那個事件就再也不會出現了。
        /// </summary>
        /// <param name="ignoreGlobalChance">
        /// true ＝ 跳過「這一站到底要不要有事件」那一關，直接進候選。
        /// **開場那一站用它** —— 理由見 `GameFlowManager.guaranteeEventOnFirstNode`。
        /// 條件、優先序、事件自己的機率**都還是照跑**，只跳過這一關。
        /// </param>
        public EventData Pick(RunContext run, System.Random rng, ItemDatabase db = null,
                              bool ignoreGlobalChance = false)
        {
            if (run == null || rng == null) return null;

            if (!ignoreGlobalChance && globalChance < 1f && rng.NextDouble() > globalChance)
            {
                if (verbose) Debug.Log("[事件] 這一站沒抽中「要有事件」");
                return null;
            }

            var candidates = new List<EventData>();
            float total = 0f;

            for (int i = 0; i < events.Count; i++)
            {
                EventData e = events[i];
                if (e == null) continue;

                if (string.IsNullOrEmpty(e.id))
                {
                    Debug.LogWarning($"[事件] 「{e.name}」沒有填 Id，無法記錄觸發狀態，跳過。", e);
                    continue;
                }

                // 「未觸發過」不是特例，它就是一條旗標條件
                if (e.once && run.HasFlag(e.TriggeredFlag))
                {
                    if (verbose) Debug.Log($"[事件] {e.title} 已經觸發過");
                    continue;
                }

                if (!GameCondition.AllMet(e.conditions, run, db))
                {
                    if (verbose)
                        Debug.Log($"[事件] {e.title} 條件不合：{GameCondition.FirstUnmet(e.conditions, run, db)}");
                    continue;
                }

                if (e.weight <= 0f) continue;

                candidates.Add(e);
            }

            if (candidates.Count == 0)
            {
                if (verbose) Debug.Log("[事件] 沒有任何合格的事件");
                return null;
            }

            // ── 只留優先序最高的那一批 ──
            //
            // 【為什麼要分批，而不是把 priority 折進權重】
            // 折進權重的話「一定先出」就變成「很可能先出」——
            // 開場介紹被壓到第三站才出現，那就沒有意義了。
            //
            // 【為什麼是「最高的那一批」而不是「最高的那一個」】
            // 同一個 priority 可以有好幾個事件，那時還是要隨機 ——
            // 不然就變成一張固定的播放清單，那不是這個系統要的東西。
            int top = int.MinValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].priority > top) top = candidates[i].priority;
            }

            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                if (candidates[i].priority < top) candidates.RemoveAt(i);
            }

            for (int i = 0; i < candidates.Count; i++) total += candidates[i].weight;

            // 依權重挑一個
            double roll = rng.NextDouble() * total;
            EventData picked = candidates[candidates.Count - 1];

            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= candidates[i].weight;
                if (roll <= 0) { picked = candidates[i]; break; }
            }

            // 事件自己的機率是**最後一關** —— 沒過就這一站不觸發，
            // 而不是換一個事件。否則「30% 機率」會被其他事件補位而形同虛設
            if (picked.chance < 1f && rng.NextDouble() > picked.chance)
            {
                if (verbose) Debug.Log($"[事件] {picked.title} 選到了但沒抽中它自己的 {picked.chance:P0}");
                return null;
            }

            if (verbose)
                Debug.Log($"[事件] 觸發：{picked.title}" +
                          $"（優先序 {picked.priority} 這一批有 {candidates.Count} 個）");
            return picked;
        }

        /// <summary>
        /// 標記成「觸發過」。**事件播完才呼叫**，不是選到的時候。
        /// </summary>
        public static void MarkTriggered(EventData e, RunContext run)
        {
            if (e == null || run == null || !e.once) return;
            run.SetFlag(e.TriggeredFlag);
        }
    }
}
