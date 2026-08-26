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

        [Tooltip("可以被哪些顏色的卡影響。\n" +
                 "**可以有多個** —— 規格 §3.1：多色回答先用多個色點。\n" +
                 "⚠️ 字串要跟卡牌的 Color Id 一模一樣，打錯不會報錯，只會沒效果")]
        public List<string> acceptedColorIds = new List<string>();

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
    /// ⚠️ 這是新機制，跟現有的探索打牌是兩套。詳見 <see cref="ProbabilityCardData"/>。
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
        public ProbabilityCardPool cardPool;

        [Tooltip("開場發幾張手牌（規格 §7.1 handSize）。**不要 hardcode**")]
        [Min(0)] public int handSize = 5;

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
