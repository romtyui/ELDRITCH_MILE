using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core.ProbabilityDialogue
{
    /// <summary>
    /// 出牌怎麼把機率往上推。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【為什麼有兩種】規格書原本寫的是加法，但實測與模擬都指出加法會壞掉：
    /// 牌面那些 20~100 的數字是為**探索**設計的（60 的牌 ＝ 這張牌有 60% 開得開箱子），
    /// 直接當百分點加上去太大 —— 一張 100 的牌自己就填滿一個回答。
    ///
    /// 20000 手模擬（`Tools/spec_rebuild/sim_dialogue.py`，回答各收一種屬性）：
    ///
    /// | | 平均 | 中位 | 能推到 100% |
    /// |---|---|---|---|
    /// | 加法 | 85.6% | 100% | **66.1%** |
    /// | 乘法 | 56.7% | 50% | 9.2% |
    ///
    /// 乘法之後才出現取捨：全押想要的獎勵約 57%，
    /// 分散押注「至少拿到一個」約 83% 但拿到哪個由不得你。
    /// </summary>
    public enum ProbabilityGrowth
    {
        /// <summary>
        /// `P += 牌面值`。規格書原本的寫法。
        /// **留著是為了規格書的 T01~T16 還跑得動**，新內容不建議用。
        /// </summary>
        Additive = 0,

        /// <summary>
        /// `P ×= (1 + 牌面值/100)`。2026-08-29 定案。
        ///
        /// 　25% 用一張 100 → 25 × 2.0 ＝ 50%
        /// 　50% 用一張 80  → 50 × 1.8 ＝ 90%
        ///
        /// ⚠️ **P = 0 時任何牌都推不動**（0 乘任何數還是 0）。
        /// 所以 `baseProbability` 不要填 0 —— Begin() 會擋下來並警告。
        /// </summary>
        Multiplicative = 1,
    }

    /// <summary>
    /// 一個回答選項。**規格書 §7.2 AnswerOption。**
    /// </summary>
    [Serializable]
    public class ProbabilityAnswerOption
    {
        [Tooltip("回答 unique ID。用來 Debug 與記錄，玩家看不到")]
        public string optionId = "";

        [TextArea]
        [Tooltip("回答文字")]
        public string text = "";

        [Tooltip("最初的成功機率（0~100）。卡牌只改 Runtime 的值，**不會改這裡**")]
        [Range(0, 100)] public int baseProbability = 30;

        [Tooltip("這個回答吃哪些屬性的牌（本我／超我／自我）。\n" +
                 "**可以有多個** —— 規格 §3.1：多屬性回答就顯示多個色點。\n\n" +
                 "顏色不用在這裡填 —— 屬性的顏色由 AttributeChartData 決定，\n" +
                 "卡框顏色也是同一份來源，兩邊永遠一致。\n\n" +
                 "⚠️ 屬性「無」的黑白牌對任何回答都有效，不必列在這裡")]
        public List<ExploreAttribute> acceptedAttributes = new List<ExploreAttribute>();

        [TextArea]
        [Tooltip("成功後 NPC 說的話")]
        public string successText = "";

        [Tooltip("成功後要跑的效果。留空 = 只顯示文字就結束")]
        public List<EventEffect> successOutcome = new List<EventEffect>();
    }

    /// <summary>
    /// 一次「用卡牌改機率的 NPC 對話」。**規格書 §7.1 EventDefinition。**
    ///
    /// ────────────────────────────────────────────────────────
    /// 【流程】（規格 §2）
    ///   出牌改機率（可出 0 張到全部）→ 選一個回答 → 判定
    ///   成功就結束；失敗就**移掉那個回答**、NPC 語氣更強，剩下的繼續
    ///   全部失敗 → 跑 terminalFailureOutcome
    ///
    /// ⚠️ **牌是同一種牌**，只有玩法不同 —— 詳見 <see cref="ProbabilityCardRules"/>。
    /// 手牌從玩家自己的探索牌組發，所以牌組建構會影響對話結果。
    /// </summary>
    [CreateAssetMenu(fileName = "PDialogue_", menuName = "Eldritch/機率對話/對話事件")]
    public class ProbabilityDialogueData : ScriptableObject
    {
        [Header("識別")]
        public string eventId = "";

        [Tooltip("說話的角色 id（對應 CharacterDatabase）")]
        public string npcId = "";

        [Header("畫面")]
        [Tooltip("背景圖。留空則沿用目前畫面")]
        public Sprite background;

        [Header("文字")]
        [TextArea]
        [Tooltip("第一次的 NPC 問話")]
        public string initialPrompt = "";

        [TextArea]
        [Tooltip("每次失敗後的問話，語氣可以更強烈。\n\n" +
                 "**用第幾次失敗去取**（failureCount - 1）。不夠用時會**沿用最後一句**，" +
                 "不會變成空白 —— 規格 §11 的 Edge Case「failurePrompts 不夠」。")]
        public List<string> failurePrompts = new List<string>();

        [TextArea]
        [Tooltip("全部回答都失敗後的最後一段文字")]
        public string finalFailureText = "";

        [Header("卡牌")]
        [Tooltip("開場發幾張手牌（規格 §7.1 handSize）。**不要 hardcode**")]
        [Min(0)] public int handSize = 5;

        [Tooltip("⚠️ 正常情況**留空**。\n\n" +
                 "手牌是從玩家自己的探索牌組發的 —— 對話用的牌和開寶箱用的牌\n" +
                 "是同一種牌、同一副牌組，所以牌組建構才會影響對話。\n\n" +
                 "這一欄只在「牌組拿不到」時當備援：離線測試、\n" +
                 "以及 F1 直接跳進這個 Stage（那時 run 可能還沒發過牌）。")]
        public List<CardDataExplore> fallbackCards = new List<CardDataExplore>();

        [Header("回答")]
        [Tooltip("目前目標是三個回答，但程式不限制數量")]
        public List<ProbabilityAnswerOption> options = new List<ProbabilityAnswerOption>();

        [Header("結果")]
        [Tooltip("全部回答失敗後的懲罰或劇情")]
        public List<EventEffect> terminalFailureOutcome = new List<EventEffect>();

        [Header("規則")]
        [Tooltip("出牌怎麼把機率往上推。\n\n" +
                 "· Multiplicative（預設）P ×= (1 + 牌面值/100)　25% 用 100 的牌 → 50%\n" +
                 "· Additive　　　　　　　P += 牌面值　　　　　　規格書原本的寫法\n\n" +
                 "⚠️ 用乘法時 Base Probability **不可以是 0** —— 0 乘不動。\n" +
                 "詳細的模擬數據見 ProbabilityGrowth 的說明")]
        public ProbabilityGrowth growth = ProbabilityGrowth.Multiplicative;

        [Tooltip("機率上限。規格 §4 的 Recommended 是 Clamp 0~100。\n" +
                 "留這個欄位是因為規格說**不要 hardcode**，未來可能允許超過")]
        [Range(1, 999)] public int probabilityCap = 100;

        /// <summary>
        /// 這次失敗要用哪一句問話。**不夠用時沿用最後一句**，永遠不會是空的。
        /// </summary>
        public string GetFailurePrompt(int failureCount)
        {
            if (failurePrompts == null || failurePrompts.Count == 0) return "";

            int idx = Mathf.Clamp(failureCount - 1, 0, failurePrompts.Count - 1);
            return failurePrompts[idx];
        }
    }
}
