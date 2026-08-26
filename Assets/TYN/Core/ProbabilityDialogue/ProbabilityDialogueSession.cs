using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core.ProbabilityDialogue
{
    /// <summary>
    /// 「用卡牌改機率的對話」的**規則引擎**。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【為什麼是純 C# 不是 MonoBehaviour】
    ///
    /// 規格書 §8 把 Module 分得很細（EventController / ProbabilityService /
    /// RandomResolver / EventView…），核心是一句話：**UI 只接結果，不可以自己改 Data**。
    ///
    /// 拆成七個 class 對這個規模是過度設計，但那條界線要守住 ——
    /// 所以規則全部在這一支、完全不碰 UI，UI 透過事件接通知。
    /// 好處是 **T01~T16 可以完全離線跑**，不用開場景、不用擺 UI。
    ///
    /// ⚠️ 這是新機制，跟已驗收的探索打牌（`DialogueEncounterController`）**是兩套**，
    /// 刻意不共用。詳見 <see cref="ProbabilityCardData"/> 的對照表。
    /// </summary>
    public class ProbabilityDialogueSession
    {
        public enum State
        {
            /// 還沒開始
            Loading,

            /// 可以出牌、可以選回答
            CardPhase,

            /// 判定中。**所有輸入都要被擋掉**（規格 §5.1、§10）
            Resolving,

            /// 結束（成功或全部失敗）
            End,
        }

        /// <summary>一個回答在這一場的即時狀態。</summary>
        public class RuntimeOption
        {
            public ProbabilityAnswerOption source;

            /// 目前機率。**卡牌只改這個，不動 baseProbability**（規格 §5）
            public int currentProbability;

            /// 失敗過的回答不可再選，也不再被卡牌影響（規格 R6）
            public bool available = true;

            public string OptionId => source != null ? source.optionId : "";
        }

        // ==========================================
        // 狀態
        // ==========================================
        public State CurrentState { get; private set; } = State.Loading;
        public ProbabilityDialogueData Data { get; private set; }

        /// 失敗過幾次。用來挑 failurePrompt
        public int FailureCount { get; private set; }

        /// 目前的 NPC 問話
        public string CurrentPrompt { get; private set; } = "";

        private readonly List<RuntimeOption> options = new List<RuntimeOption>();
        private readonly List<ProbabilityCardData> hand = new List<ProbabilityCardData>();

        public IReadOnlyList<RuntimeOption> Options => options;
        public IReadOnlyList<ProbabilityCardData> Hand => hand;

        public bool HasAvailableOption
        {
            get
            {
                for (int i = 0; i < options.Count; i++) if (options[i].available) return true;
                return false;
            }
        }

        private System.Random rng;

        /// <summary>
        /// 最後一次判定的骰值。**測試與除錯用** —— 規格 §5.1 要求記錄。
        /// </summary>
        public int LastRoll { get; private set; } = -1;
        public string LastSelectedOptionId { get; private set; } = "";

        // ==========================================
        // 事件（UI 只接這些，不主動讀寫狀態）
        // ==========================================
        public event Action OnStarted;
        /// (卡, 受影響的回答, 每個回答的 before, after)
        public event Action<ProbabilityCardData, List<RuntimeOption>, List<int>, List<int>> OnCardPlayed;
        public event Action<RuntimeOption, int, bool> OnOptionResolved;   // (回答, roll, 成功)
        public event Action<RuntimeOption> OnOptionDisabled;
        public event Action<int, string> OnPromptChanged;                 // (failureCount, 新的問話)
        public event Action<bool> OnEnded;                                // true = 成功結束
        public event Action OnHandChanged;

        // ==========================================
        /// <summary>
        /// 開始一場。rng 一定要外面傳 —— 測試要能指定種子重現（規格 §14-6）。
        /// </summary>
        public bool Begin(ProbabilityDialogueData data, System.Random random)
        {
            // 規格 §11：Data 不完整時要在開始前顯示錯誤，**不可以讓玩家卡在畫面上**
            if (data == null)
            {
                Debug.LogError("[機率對話] 沒有指定對話資料，不開始");
                return false;
            }

            if (data.options == null || data.options.Count == 0)
            {
                Debug.LogError($"[機率對話]「{data.name}」一個回答都沒有，不開始 —— 開了玩家會卡死", data);
                return false;
            }

            Data = data;
            rng = random ?? new System.Random();

            options.Clear();
            for (int i = 0; i < data.options.Count; i++)
            {
                if (data.options[i] == null) continue;
                options.Add(new RuntimeOption
                {
                    source = data.options[i],
                    currentProbability = Mathf.Clamp(data.options[i].baseProbability, 0, data.probabilityCap),
                    available = true,
                });
            }

            hand.Clear();
            if (data.cardPool != null && data.handSize > 0)
                hand.AddRange(data.cardPool.Deal(data.handSize, rng));

            FailureCount = 0;
            CurrentPrompt = data.initialPrompt;
            LastRoll = -1;
            LastSelectedOptionId = "";

            CurrentState = State.CardPhase;

            OnStarted?.Invoke();
            OnHandChanged?.Invoke();
            OnPromptChanged?.Invoke(0, CurrentPrompt);

            Debug.Log($"[機率對話] 開始「{data.name}」：{options.Count} 個回答、手牌 {hand.Count} 張");
            return true;
        }

        // ==========================================
        // 出牌
        // ==========================================
        /// <summary>
        /// 打出一張卡。**規格 §9.1 的順序：先改 Data，再通知 View。**
        /// </summary>
        public bool PlayCard(ProbabilityCardData card)
        {
            if (CurrentState != State.CardPhase) return false;   // 規格 §10：Resolving 禁止輸入
            if (card == null) return false;

            int idx = hand.IndexOf(card);
            if (idx < 0) return false;                            // 不在手牌裡

            // 找出「同色 ＋ 還可用」的回答（規格 R4 / R6）
            var targets = new List<RuntimeOption>();
            var before = new List<int>();
            var after = new List<int>();

            for (int i = 0; i < options.Count; i++)
            {
                RuntimeOption o = options[i];
                if (!o.available) continue;
                if (o.source.acceptedColorIds == null) continue;
                if (!o.source.acceptedColorIds.Contains(card.colorId)) continue;

                targets.Add(o);
                before.Add(o.currentProbability);

                // 規格 §4 Recommended：Clamp 0~cap
                o.currentProbability = Mathf.Clamp(o.currentProbability + card.value, 0, Data.probabilityCap);
                after.Add(o.currentProbability);
            }

            // 規格 R7：拖出 hand 就移除，不能再用。
            // R8：沒有同色回答時**卡照樣用掉**，只是沒有效果 —— 要顯示 No Effect
            hand.RemoveAt(idx);

            OnCardPlayed?.Invoke(card, targets, before, after);
            OnHandChanged?.Invoke();

            if (targets.Count == 0)
                Debug.Log($"[機率對話] {card.cardId}（{card.colorId}）沒有可影響的同色回答 —— No Effect");

            return true;
        }

        // ==========================================
        // 選回答
        // ==========================================
        /// <summary>
        /// 選一個回答做判定。**規格 §9.2。**
        ///
        /// ⚠️ 進來第一件事就是把 State 改成 Resolving —— 規格 §10「快速多次點」
        /// 要求只處理第一次。狀態機本身就是那個鎖，不用另外做 requestId。
        /// </summary>
        public bool SelectOption(RuntimeOption option)
        {
            if (CurrentState != State.CardPhase) return false;
            if (option == null || !option.available) return false;
            if (!options.Contains(option)) return false;

            CurrentState = State.Resolving;

            int p = option.currentProbability;
            bool success;

            // 規格 §5 的三種情形。0 與 >=cap **不擲骰** ——
            // 擲了的話 0% 有機率成功（roll 可能是 0）、100% 有機率失敗
            if (p <= 0)
            {
                LastRoll = -1;
                success = false;
            }
            else if (p >= Data.probabilityCap)
            {
                LastRoll = -1;
                success = true;
            }
            else
            {
                LastRoll = rng.Next(0, 100);
                success = LastRoll < p;
            }

            LastSelectedOptionId = option.OptionId;

            Debug.Log($"[機率對話] 選「{option.OptionId}」機率 {p}%" +
                      (LastRoll >= 0 ? $"、骰 {LastRoll}" : "、不擲骰") +
                      $" → {(success ? "成功" : "失敗")}");

            OnOptionResolved?.Invoke(option, LastRoll, success);

            if (success)
            {
                RunOutcome(option.source.successOutcome);
                CurrentState = State.End;
                OnEnded?.Invoke(true);
                return true;
            }

            // ── 失敗（規格 §6）──
            option.available = false;
            FailureCount++;
            OnOptionDisabled?.Invoke(option);

            if (HasAvailableOption)
            {
                CurrentPrompt = Data.GetFailurePrompt(FailureCount);
                OnPromptChanged?.Invoke(FailureCount, CurrentPrompt);
                CurrentState = State.CardPhase;      // 回到可以出牌
                return true;
            }

            // 最後一個回答也失敗
            CurrentPrompt = Data.finalFailureText;
            OnPromptChanged?.Invoke(FailureCount, CurrentPrompt);
            RunOutcome(Data.terminalFailureOutcome);
            CurrentState = State.End;
            OnEnded?.Invoke(false);
            return true;
        }

        /// <summary>用 optionId 選 —— UI 綁按鈕時比傳物件方便。</summary>
        public bool SelectOption(string optionId)
        {
            for (int i = 0; i < options.Count; i++)
                if (options[i].OptionId == optionId) return SelectOption(options[i]);
            return false;
        }

        // ==========================================
        private void RunOutcome(List<EventEffect> effects)
        {
            if (effects == null || effects.Count == 0) return;

            RunContext run = GameFlowManager.Instance != null ? GameFlowManager.Instance.Run : null;
            if (run == null)
            {
                // 離線測試時沒有 run —— 那是正常的，不該吵
                return;
            }

            for (int i = 0; i < effects.Count; i++) effects[i]?.Apply(run);
        }
    }
}
