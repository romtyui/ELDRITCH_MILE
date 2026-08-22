using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 「這一區的戰鬥節點會遇到誰」的候選名單。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【它跟 LootTable／EventLibrary／CharacterPool 是同一句話】
    ///
    ///   一份候選清單 → 濾掉條件不成立的 → 依權重隨機挑一個
    ///
    /// 差別只在候選是**敵人 id**。條件用的是同一個 <see cref="GameCondition"/>，
    /// 企劃在事件那邊學會的寫法，在這裡原封不動能用。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【為什麼比 CharacterPool 簡單】
    ///
    /// 角色池有「標籤查詢」與「池中池」，因為角色會一直增加，
    /// 而且要讓新角色不用回頭改表就能進池。
    /// 敵人是**設計好的固定幾隻**，加一隻怪本來就該同時決定它出現在哪 ——
    /// 所以直接列 id 就好，多一層抽象只會多一個對不上的地方。
    ///
    /// 需要「一場放兩隻」的那天，做法是呼叫兩次，不是加一層池。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【難度曲線放哪裡】
    ///
    /// Slay the Spire 分 easy pool / hard pool 兩個池，用「第幾場」切換。
    /// 這裡**不開第二個池** —— 條件層已經能表達「第幾層之後才出現」，
    /// 用同一個池加條件比兩個池少一個要同步的地方。
    ///
    /// 漁村這一區的三隻中階怪（珊瑚寄居蟹／半魚人祭司／魚頭胖魚人）
    /// **機制不同但同一階**，所以權重都給一樣，不必排序。
    /// </summary>
    [CreateAssetMenu(fileName = "Encounters_", menuName = "Eldritch/Encounter Pool")]
    public class EncounterPool : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("給人看的註記，不影響行為。例如「漁村常見」「深淵高才出現」")]
            public string note = "";

            [Tooltip("敵人 id。要跟 EnemyData.enemyId 對得上（boss / fish_priest / …）")]
            public string enemyId = "";

            [Tooltip("權重。**同一個池子裡的相對值**，不是百分比。\n" +
                     "⚠️ Unity 用 + 新增 List 元素時會零填充 —— 記得改成 1 以上，否則這條永遠抽不到")]
            [Min(0f)] public float weight = 1f;

            [Tooltip("條件全部成立這一條才進池。留空 = 無條件。\n" +
                     "與事件的條件是**同一個型別**，寫法完全一樣。\n" +
                     "難度曲線就用這裡做：例如「已通過節點數 ≥ 3」")]
            public List<GameCondition> conditions = new List<GameCondition>();
        }

        /// <summary>
        /// 「這一區保證會遇到誰」。
        ///
        /// 【為什麼需要這個】《螺湮的祝福》要打倒半魚人祭司才會觸發。
        /// 如果祭司只是池子裡的一條，玩家可能整場都抽不到他 ——
        /// 那個事件就等於**機率再乘一次**，設計上是看不見的損失。
        ///
        /// 保證項會在「從池子抽」之前先被安排進合格的戰鬥節點。
        /// 這也是 Slay the Spire 對精英的做法：**固定安排，不抽**。
        /// </summary>
        [Serializable]
        public class Guaranteed
        {
            [Tooltip("給人看的註記。例如「《螺湮的祝福》需要打倒他」")]
            public string note = "";

            [Tooltip("敵人 id")]
            public string enemyId = "";

            [Tooltip("這一區至少要出現幾次")]
            [Min(1)] public int count = 1;

            [Tooltip("最早可以排在第幾層。第 0 層是起點，通常不希望一開場就遇到硬的")]
            [Min(0)] public int minLayer = 1;

            [Tooltip("最晚可以排在第幾層。**-1 = 不限**。\n" +
                     "設上限是為了留出後續空間 —— 打完祭司才觸發的事件，\n" +
                     "如果祭司排在最後一層，那個事件就沒有機會出現了")]
            public int maxLayer = -1;
        }

        [Header("一般戰鬥節點的候選。順序不影響行為")]
        public List<Entry> entries = new List<Entry>();

        [Header("保證出現（會先佔位，再抽剩下的）")]
        public List<Guaranteed> guaranteed = new List<Guaranteed>();

        [Header("Boss 節點")]
        [Tooltip("Boss 節點固定打誰。留空則跟一般節點一樣從池子抽（不建議）。\n\n" +
                 "【為什麼 Boss 不抽】業界慣例：精英與 Boss 是**預先安排**的，" +
                 "不從池子隨機。玩家對 Boss 的預期是「這一區的終點」，抽到雜魚會很怪")]
        public string bossEnemyId = "";

        [Tooltip("把每次的挑選過程印到 Console。查「為什麼遇到他」時打開")]
        public bool verbose = false;

        // ==========================================
        /// <summary>
        /// 依權重抽一個敵人 id。抽不到回 null ——
        /// 呼叫端要有備援（通常是「交給戰鬥組自己抽怪」）。
        /// </summary>
        /// <param name="rng">
        /// 亂數源一定要外面傳。**同一張地圖重算要得到同一個結果** ——
        /// 用全域亂數的話，同一場 run 每次讀取都會變。
        /// </param>
        public string Pick(RunContext run, System.Random rng, ItemDatabase itemDb = null)
        {
            if (rng == null) return null;

            var candidates = new List<Entry>();
            float total = 0f;

            for (int i = 0; i < entries.Count; i++)
            {
                Entry e = entries[i];
                if (e == null || string.IsNullOrEmpty(e.enemyId)) continue;
                if (e.weight <= 0f) continue;

                // ⚠️ 條件是在**挑之前**濾的，不是挑到之後再檢查 ——
                //    後者會讓「條件不成立」變成「這一站沒有敵人」，而不是「換一隻」
                if (!GameCondition.AllMet(e.conditions, run, itemDb))
                {
                    if (verbose)
                        Debug.Log($"[敵人池]「{name}」跳過 {e.enemyId}：" +
                                  $"{GameCondition.FirstUnmet(e.conditions, run, itemDb)}", this);
                    continue;
                }

                candidates.Add(e);
                total += e.weight;
            }

            if (candidates.Count == 0 || total <= 0f)
            {
                Debug.LogWarning(
                    $"[敵人池]「{name}」這次沒有任何合格的候選，交給戰鬥組自己抽怪。\n" +
                    "常見原因：條目權重是 0（Unity 新增元素會零填充），或條件都不成立。", this);
                return null;
            }

            double roll = rng.NextDouble() * total;
            float acc = 0f;

            for (int i = 0; i < candidates.Count; i++)
            {
                acc += candidates[i].weight;
                if (roll <= acc)
                {
                    if (verbose)
                        Debug.Log($"[敵人池]「{name}」{candidates.Count} 個候選 → {candidates[i].enemyId}", this);
                    return candidates[i].enemyId;
                }
            }

            return candidates[candidates.Count - 1].enemyId;
        }
    }
}
