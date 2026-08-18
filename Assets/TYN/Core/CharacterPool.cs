using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 「這一站由誰來說話」的候選名單。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【它跟 LootTable／EventLibrary 是同一句話】
    ///
    ///   一份候選清單 → 濾掉條件不成立的 → 依權重隨機挑一個
    ///
    /// 差別只在候選是**角色**。所以條件用的是同一個 <see cref="GameCondition"/>，
    /// 企劃在事件那邊學會的寫法，在這裡原封不動能用。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【為什麼不是 LootTable 那種三層（表→池→條目）】
    ///
    /// 戰利品要「抽 6 次、不重複、再從另一個池抽 2 次」，所以需要池。
    /// 角色池永遠只挑**一個**人 —— 一段對話只有一個說話者。
    /// 硬套三層只會讓每張角色池資產都多一個永遠只有一個元素的 Pools 陣列。
    ///
    /// 需要「一次挑兩個人」的那天（雙人對話），做法是呼叫兩次並排除第一個，
    /// 不是加一層池。
    ///
    /// ────────────────────────────────────────────────────────
    /// 漁村的池子長這樣：
    ///
    ///   [標籤查詢 require=漁村]  權重 100  無條件
    ///   [指名 tokura]            權重  40  【深淵】侵蝕度 ≥ 50
    ///
    /// 標籤查詢那一條是「這個區域的常駐居民」—— 之後新增漁村的路人角色，
    /// 只要在角色資料上打 [漁村] 標籤就會自動進池，**不用回頭改這張表**。
    /// </summary>
    [CreateAssetMenu(fileName = "Chars_", menuName = "Eldritch/Character Pool")]
    public class CharacterPool : ScriptableObject
    {
        public enum EntryKind
        {
            /// 指名一個角色 id
            Character = 0,

            /// 任何符合標籤條件的角色。**加新角色不用回頭改池子**
            TagQuery = 1,

            /// 轉去另一個池子。共用的候選名單只要寫一次
            Pool = 2,
        }

        [Serializable]
        public class Entry
        {
            [Tooltip("給人看的註記，不影響行為。例如「漁村居民」「侵蝕度高才出現」")]
            public string note = "";

            [Tooltip("這一條是「指名角色」「標籤查詢」還是「轉到另一個池子」")]
            public EntryKind kind = EntryKind.Character;

            [Tooltip("Character：角色 id。要在 CharacterDatabase 登記過")]
            public string characterId = "";

            [Tooltip("TagQuery：必須**全部**具備的標籤。留空 = 不限（＝所有角色）")]
            public List<string> requireTags = new List<string>();

            [Tooltip("TagQuery：具備**任一個**就排除的標籤")]
            public List<string> excludeTags = new List<string>();

            [Tooltip("Pool：要轉去抽的池子")]
            public CharacterPool pool;

            [Tooltip("權重。**同一個池子裡的相對值**，不是百分比。\n" +
                     "⚠️ Unity 用 + 新增 List 元素時會零填充 —— 記得改成 1 以上，否則這條永遠抽不到")]
            [Min(0f)] public float weight = 1f;

            [Tooltip("條件全部成立這一條才進池。留空 = 無條件。\n" +
                     "與事件的條件是**同一個型別**，寫法完全一樣")]
            public List<GameCondition> conditions = new List<GameCondition>();
        }

        [Header("候選。順序不影響行為")]
        public List<Entry> entries = new List<Entry>();

        [Tooltip("把每次的挑選過程印到 Console。查「為什麼是他出現」時打開")]
        public bool verbose = false;

        /// <summary>遞迴深度上限。池子指到池子、又指回來的話會無限迴圈。</summary>
        private const int MaxPoolDepth = 8;

        // ==========================================
        /// <summary>
        /// 挑一個人。挑不到就回 null（呼叫端要有備援 —— 通常是節點上手填的那個 id）。
        /// </summary>
        /// <param name="rng">
        /// 亂數源一定要外面傳。**同一個節點重進要是同一個人** ——
        /// 用全域亂數的話，玩家離開再進來就換人了。
        /// </param>
        /// <param name="charDb">角色庫。傳 null 就向 <see cref="GameFlowManager"/> 借。
        /// **編輯器工具與測試一定要自己傳** —— Instance 只有執行時才有值。</param>
        /// <param name="itemDb">道具庫。條件裡的 TagCount 類會用到，同上。</param>
        public CharacterData Pick(
            RunContext run, System.Random rng,
            CharacterDatabase charDb = null, ItemDatabase itemDb = null)
        {
            return Pick(run, rng, ResolveDb(charDb), itemDb, 0);
        }

        private CharacterData Pick(
            RunContext run, System.Random rng,
            CharacterDatabase charDb, ItemDatabase itemDb, int depth)
        {
            if (rng == null) return null;

            if (depth >= MaxPoolDepth)
            {
                Debug.LogWarning($"[角色池] 「{name}」的層數超過 {MaxPoolDepth} 層，可能互相指來指去。中止。", this);
                return null;
            }

            Entry picked = PickEntry(run, rng, itemDb);
            if (picked == null) return null;

            switch (picked.kind)
            {
                case EntryKind.Character:
                {
                    CharacterData c = charDb != null ? charDb.GetById(picked.characterId) : null;

                    if (c == null)
                    {
                        Debug.LogWarning(
                            $"[角色池] 「{name}」指名的「{picked.characterId}」在 CharacterDatabase 裡找不到。", this);
                    }
                    else if (verbose)
                    {
                        Debug.Log($"[角色池] 「{name}」指名 → {c.Label}");
                    }

                    return c;
                }

                case EntryKind.TagQuery:
                {
                    List<CharacterData> matches = Query(picked.requireTags, picked.excludeTags, charDb);

                    if (matches.Count == 0)
                    {
                        Debug.LogWarning(
                            $"[角色池] 「{name}」的標籤查詢沒有任何符合的角色：" +
                            $"需要 [{string.Join(", ", picked.requireTags.ToArray())}]。\n" +
                            "CharacterDatabase 裡沒有這種標籤的人，或標籤打錯字。", this);
                        return null;
                    }

                    // 查到的候選之間是**等機率**的 —— 權重掛在條目上（「居民」對「時藏」），
                    // 不掛在個別角色上。要讓某個居民更常出現，就把他另外拉一條指名條目
                    CharacterData c = matches[rng.Next(matches.Count)];

                    if (verbose)
                        Debug.Log($"[角色池] 「{name}」標籤查詢 {matches.Count} 人 → {c.Label}");

                    return c;
                }

                case EntryKind.Pool:
                    if (picked.pool == null)
                    {
                        Debug.LogWarning($"[角色池] 「{name}」有一條 Pool 條目沒有指定池子。", this);
                        return null;
                    }
                    return picked.pool.Pick(run, rng, charDb, itemDb, depth + 1);
            }

            return null;
        }

        /// <summary>
        /// 依權重挑一條條目。條件不成立的先濾掉。
        ///
        /// ⚠️ 條件是在**挑之前**濾的，不是挑到之後再檢查 ——
        /// 後者會讓「條件不成立」變成「這次沒有人說話」，而不是「換一個人」。
        /// </summary>
        private Entry PickEntry(RunContext run, System.Random rng, ItemDatabase itemDb)
        {
            var candidates = new List<Entry>();
            float total = 0f;
            int zeroWeight = 0;

            for (int i = 0; i < entries.Count; i++)
            {
                Entry e = entries[i];
                if (e == null) continue;

                if (e.weight <= 0f) { zeroWeight++; continue; }

                if (!GameCondition.AllMet(e.conditions, run, itemDb))
                {
                    if (verbose)
                        Debug.Log($"[角色池] 「{name}」跳過 {Describe(e)}：{GameCondition.FirstUnmet(e.conditions, run, itemDb)}");
                    continue;
                }

                candidates.Add(e);
                total += e.weight;
            }

            if (candidates.Count == 0)
            {
                if (zeroWeight > 0)
                {
                    Debug.LogWarning(
                        $"[角色池] 「{name}」有 {zeroWeight} 筆條目的 Weight 是 0 而被跳過。\n" +
                        "⚠️ Unity 用 Inspector 的 + 新增 List 元素時會零填充 —— 請手動改成 1。", this);
                }
                else if (verbose)
                {
                    Debug.Log($"[角色池] 「{name}」沒有任何合格的候選");
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

        private static string Describe(Entry e)
        {
            if (!string.IsNullOrEmpty(e.note)) return e.note;

            switch (e.kind)
            {
                case EntryKind.Character: return e.characterId;
                case EntryKind.TagQuery: return $"[{string.Join(", ", e.requireTags.ToArray())}]";
                case EntryKind.Pool: return e.pool != null ? e.pool.name : "(空的池子)";
            }
            return e.kind.ToString();
        }

        // ==========================================
        /// <summary>
        /// 標籤查詢。回傳的是**新的 List**，呼叫端可以放心修改。
        /// 與 <see cref="LootService.Query"/> 是同一個形狀，只是查的是角色。
        /// </summary>
        public static List<CharacterData> Query(
            List<string> require, List<string> exclude, CharacterDatabase db = null)
        {
            var result = new List<CharacterData>();

            db = ResolveDb(db);
            if (db == null)
            {
                Debug.LogWarning("[角色池] GameFlowManager 上沒有掛 CharacterDatabase，標籤查詢一定查不到人。");
                return result;
            }

            for (int i = 0; i < db.characters.Count; i++)
            {
                CharacterData c = db.characters[i];
                if (c == null || string.IsNullOrEmpty(c.id)) continue;

                bool ok = true;

                if (require != null)
                {
                    for (int t = 0; t < require.Count && ok; t++)
                    {
                        if (!string.IsNullOrEmpty(require[t]) && !c.HasTag(require[t])) ok = false;
                    }
                }

                if (ok && exclude != null)
                {
                    for (int t = 0; t < exclude.Count && ok; t++)
                    {
                        if (!string.IsNullOrEmpty(exclude[t]) && c.HasTag(exclude[t])) ok = false;
                    }
                }

                if (ok) result.Add(c);
            }

            return result;
        }

        /// <summary>沒傳角色庫就向 GameFlowManager 借。</summary>
        private static CharacterDatabase ResolveDb(CharacterDatabase db)
        {
            if (db != null) return db;
            return GameFlowManager.Instance != null ? GameFlowManager.Instance.characterDatabase : null;
        }
    }
}
