using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core.ProbabilityDialogue
{
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
