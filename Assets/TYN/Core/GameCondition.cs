using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>各尊神的侵蝕度 id。目前只有「深淵」是確定的，其餘等企劃定。</summary>
    public static class CorruptionTracks
    {
        /// <summary>【深淵】。大綱的深淵線事件用的就是這一條。</summary>
        public const string Abyss = "abyss";
    }

    /// <summary>
    /// 一條「現在成不成立」的判斷。**事件池與角色池共用同一個型別。**
    ///
    /// ────────────────────────────────────────────────────────
    /// 【為什麼要抽出來】三件事拆到底是同一句話：
    /// 「一份候選清單 → 濾掉條件不成立的 → 依權重隨機挑一個」
    ///
    ///   · 戰利品：候選是道具（`LootTable`，已完成）
    ///   · 事件　：候選是事件，條件包含「未觸發過」
    ///   · 角色池：候選是角色，條件包含「侵蝕度夠高」
    ///
    /// 不抽出來的話，事件會做一套條件、角色池再做一套，
    /// 兩套的語法還會不一樣 —— 企劃要記兩種寫法。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【條件種類是照大綱實際出現的需求開的】不是憑空想像：
    ///
    /// | 大綱的條件 | 對應 |
    /// |---|---|
    /// | 「游戲内首次踏入漁村」 | `FlagNotSet`（踏入時立旗標） |
    /// | 「【深淵】的侵蝕度為 50% 以上」 | `CorruptionAtLeast` |
    /// | 「持有的神牌少於 3 張」 | `TagCountBelow` |
    /// | 「持有 20 張以上的武器牌」 | `TagCountAtLeast` |
    /// | 「身上有糧食」 | `TagCountAtLeast`（1） |
    /// | 「遊戲進行一段時間後（約 800 秒）」 | `ElapsedAtLeast` |
    ///
    /// 「打倒半魚人祭司后 **30% 概率**」裡的機率**不是條件** ——
    /// 機率屬於事件本身（`EventData.chance`），條件只回答「能不能」。
    /// 混在一起的話，同一個事件就沒辦法「條件成立但這次沒抽中，下次還能再抽」。
    /// </summary>
    [Serializable]
    public class GameCondition
    {
        public enum Kind
        {
            /// 旗標已立起
            FlagSet = 0,

            /// 旗標**還沒**立起。「未觸發過」「第一次進來」都是這個
            FlagNotSet = 1,

            /// 某尊神的侵蝕度 ≥ value
            CorruptionAtLeast = 2,

            /// 某尊神的侵蝕度 < value
            CorruptionBelow = 3,

            /// 身上帶這個標籤的東西 ≥ value
            TagCountAtLeast = 4,

            /// 身上帶這個標籤的東西 < value
            TagCountBelow = 5,

            /// 這場 run 已經玩了 ≥ value 秒
            ElapsedAtLeast = 6,

            /// <summary>
            /// **戰鬥牌組**裡這一類的牌 ≥ value。key：Weapon（武器牌）／Faith（信仰牌 = 神牌）。2026-10-29 追加。
            /// 跟 TagCount 的差別：那個數的是背包裡的道具，牌在牌組裡，不在背包。
            /// </summary>
            DeckCountAtLeast = 7,

            /// <summary>戰鬥牌組裡這一類的牌 &lt; value。key 同上</summary>
            DeckCountBelow = 8,

            /// <summary>這個角色在隊伍裡。key = 角色 id。對話事件用。2026-11-02</summary>
            InParty = 9,

            /// <summary>這個角色不在隊伍裡。key = 角色 id</summary>
            NotInParty = 10,

            /// <summary>這個角色還沒加入大廳。key = 角色 id。協助者事件用</summary>
            NotInLobby = 11,

            /// <summary>同行成員 ≤ value 人（不含主角）。協助者事件用（大綱：同行成員不多於 3 人）</summary>
            PartySizeAtMost = 12,
        }

        [Tooltip("要判斷什麼")]
        public Kind kind = Kind.FlagSet;

        [Tooltip("Flag 類：旗標名稱\n" +
                 "Corruption 類：神的 id（深淵 = abyss）\n" +
                 "TagCount 類：道具標籤（Food / Weapon / Curio…）\n" +
                 "Elapsed：不用填")]
        public string key = "";

        [Tooltip("門檻。Flag 類不用填")]
        public int value;

        public bool IsMet(RunContext run) => IsMet(run, null);

        /// <param name="db">道具庫。測試與編輯器工具要自己傳，見 RunContext.CountByTag</param>
        public bool IsMet(RunContext run, ItemDatabase db)
        {
            if (run == null) return false;

            switch (kind)
            {
                case Kind.FlagSet: return run.HasFlag(key);
                case Kind.FlagNotSet: return !run.HasFlag(key);

                case Kind.CorruptionAtLeast: return run.GetCorruption(key) >= value;
                case Kind.CorruptionBelow: return run.GetCorruption(key) < value;

                case Kind.TagCountAtLeast: return run.CountByTag(key, db) >= value;
                case Kind.TagCountBelow: return run.CountByTag(key, db) < value;

                case Kind.DeckCountAtLeast: return PlayerVitals.CountDeck(key) >= value;
                case Kind.DeckCountBelow: return PlayerVitals.CountDeck(key) < value;

                case Kind.InParty: return run.InParty(key);
                case Kind.NotInParty: return !run.InParty(key);
                case Kind.NotInLobby:
                {
                    MetaProgressData meta = GameFlowManager.Instance != null ? GameFlowManager.Instance.Meta : null;
                    return meta == null || !meta.InLobby(key);
                }
                case Kind.PartySizeAtMost: return run.party.Count <= value;

                case Kind.ElapsedAtLeast: return run.ElapsedSeconds >= value;
            }

            return false;
        }

        /// <summary>給 Console 與編輯器看的可讀說明。查「為什麼這個事件沒觸發」時很有用。</summary>
        public string Describe()
        {
            switch (kind)
            {
                case Kind.FlagSet: return $"旗標「{key}」已立";
                case Kind.FlagNotSet: return $"旗標「{key}」尚未立";
                case Kind.CorruptionAtLeast: return $"{key} 侵蝕度 ≥ {value}";
                case Kind.CorruptionBelow: return $"{key} 侵蝕度 < {value}";
                case Kind.TagCountAtLeast: return $"持有 [{key}] ≥ {value}";
                case Kind.TagCountBelow: return $"持有 [{key}] < {value}";
                case Kind.ElapsedAtLeast: return $"已進行 ≥ {value} 秒";
                case Kind.DeckCountAtLeast: return $"牌組 [{key}] ≥ {value}";
                case Kind.DeckCountBelow: return $"牌組 [{key}] < {value}";
                case Kind.InParty: return $"「{key}」在隊伍內";
                case Kind.NotInParty: return $"「{key}」不在隊伍內";
                case Kind.NotInLobby: return $"「{key}」未加入大廳";
                case Kind.PartySizeAtMost: return $"同行成員 ≤ {value}";
            }
            return kind.ToString();
        }

        // ==========================================
        /// <summary>
        /// 一整組條件是不是**全部**成立。空清單 = 無條件成立。
        ///
        /// 【為什麼只有 AND 沒有 OR】大綱裡的條件全部都是單一條件，
        /// 沒有一個需要「A 或 B」。真的需要時再開，不要為了假想的需求先做一棵運算樹 ——
        /// 那會讓企劃得學一套小語言。
        /// </summary>
        public static bool AllMet(List<GameCondition> conditions, RunContext run, ItemDatabase db = null)
        {
            if (conditions == null || conditions.Count == 0) return true;

            for (int i = 0; i < conditions.Count; i++)
            {
                GameCondition c = conditions[i];
                if (c != null && !c.IsMet(run, db)) return false;
            }
            return true;
        }

        /// <summary>回傳第一條不成立的條件的說明。全部成立就回 null。</summary>
        public static string FirstUnmet(List<GameCondition> conditions, RunContext run, ItemDatabase db = null)
        {
            if (conditions == null) return null;

            for (int i = 0; i < conditions.Count; i++)
            {
                GameCondition c = conditions[i];
                if (c != null && !c.IsMet(run, db)) return c.Describe();
            }
            return null;
        }
    }
}
