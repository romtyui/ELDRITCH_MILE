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
    /// ⚠️ **流程**跟已驗收的探索打牌（`DialogueEncounterController`）是兩套，刻意不共用
    /// —— 改那一支會連帶弄壞探索打牌。
    /// 但**牌是同一種牌**（`CardDataExplore`，手牌從玩家的探索牌組發）。
    /// 兩者的差別見 <see cref="ProbabilityCardRules"/> 的對照表。
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
        private readonly List<CardDataExplore> hand = new List<CardDataExplore>();

        public IReadOnlyList<RuntimeOption> Options => options;
        public IReadOnlyList<CardDataExplore> Hand => hand;

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

        /// <summary>
        /// 這一場結束時**真的發生了什麼**，接成一句給玩家看的話
        /// （「獲得 老舊釣竿、【深淵】的侵蝕度 +5%」）。沒有效果就是空字串。
        ///
        /// 【為什麼要留著】`EventEffect.Apply` 的回傳值就是那句提示，
        /// 之前這裡把它丟掉了 —— 於是對話成功拿到東西，畫面上完全沒有結算，
        /// 玩家只能自己去翻背包。事件那一頭（`EventStageController.ApplyOption`）
        /// 早就在用同一個回傳值了，這裡只是補上同一件事。
        ///
        /// ⚠️ **在 `OnEnded` 之前就填好** —— UI 是在那個事件裡讀它的。
        /// </summary>
        public string LastOutcomeNotes { get; private set; } = "";

        /// <summary>多個效果之間用什麼隔開。跟事件那邊同一個寫法。</summary>
        public string outcomeSeparator = "、";

        // ==========================================
        // 事件（UI 只接這些，不主動讀寫狀態）
        // ==========================================
        public event Action OnStarted;
        /// (卡, 受影響的回答, 每個回答的 before, after)
        public event Action<CardDataExplore, List<RuntimeOption>, List<int>, List<int>> OnCardPlayed;
        public event Action<RuntimeOption, int, bool> OnOptionResolved;   // (回答, roll, 成功)
        public event Action<RuntimeOption> OnOptionDisabled;
        public event Action<int, string> OnPromptChanged;                 // (failureCount, 新的問話)
        public event Action<bool> OnEnded;                                // true = 成功結束
        public event Action OnHandChanged;

        // ==========================================
        /// <summary>
        /// 開始一場。rng 一定要外面傳 —— 測試要能指定種子重現（規格 §14-6）。
        /// </summary>
        /// <param name="deckSource">
        /// 從哪副牌組發手牌。**正常情況傳玩家的 `run.exploreDeck`** ——
        /// 對話用的牌和開寶箱用的牌是同一種牌、同一副牌組。
        ///
        /// 傳 null 或空的話會退回 `data.fallbackCards`（離線測試、F1 直接跳關）。
        /// 引擎本身不去讀 `GameFlowManager`，**這樣才能純離線跑規格測試**。
        /// </param>
        public bool Begin(ProbabilityDialogueData data, System.Random random,
                          IList<CardDataExplore> deckSource = null)
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

                int p0 = Mathf.Clamp(data.options[i].baseProbability, 0, data.probabilityCap);

                // ⚠️ 乘法的死角：0 乘任何數還是 0 —— 那個回答會**永遠推不動**，
                //    而且畫面上只會安靜地一直顯示 0%，看不出是設定錯了
                if (p0 <= 0 && data.growth == ProbabilityGrowth.Multiplicative)
                {
                    Debug.LogWarning(
                        $"[機率對話]「{data.name}」的回答「{data.options[i].optionId}」" +
                        "Base Probability 是 0，但成長公式是乘法 —— 這個回答永遠推不動。\n" +
                        "把基礎機率改成大於 0，或把 Growth 改成 Additive。", data);
                }

                options.Add(new RuntimeOption
                {
                    source = data.options[i],
                    currentProbability = p0,
                    available = true,
                });
            }

            hand.Clear();
            if (data.handSize > 0)
            {
                // 玩家的牌組優先；拿不到才用資料上的備援清單。
                // ⚠️ 順序不能反 —— 反過來的話玩家辛苦組的牌組永遠用不到
                IList<CardDataExplore> src =
                    (deckSource != null && deckSource.Count > 0) ? deckSource : data.fallbackCards;

                hand.AddRange(ProbabilityCardRules.Deal(src, data.handSize, rng));

                if (hand.Count == 0)
                {
                    Debug.LogWarning(
                        $"[機率對話]「{data.name}」發不出手牌 —— 玩家牌組是空的，" +
                        "而且 Fallback Cards 也沒填。\n" +
                        "⚠️ 沒有手牌還是可以直接選回答（用基礎機率），不會卡死。", data);
                }
            }

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
        public bool PlayCard(CardDataExplore card)
        {
            if (CurrentState != State.CardPhase) return false;   // 規格 §10：Resolving 禁止輸入
            if (card == null) return false;

            int idx = hand.IndexOf(card);
            if (idx < 0) return false;                            // 不在手牌裡

            // ⚠️ 用 IndexOf 找的是**第一張同名卡**。牌組允許重複，
            //    所以手上可能有兩張一樣的 —— 移掉哪一張都一樣，不影響結果

            // 找出「屬性相符 ＋ 還可用」的回答（規格 R4 / R6）。
            //
            // ⚠️ **一張牌會同時作用在所有相符的回答上**，不是只挑一個。
            //    這是規格書 R4 的原意，也是色點存在的理由 ——
            //    玩家看得到「這張紅牌會推動哪幾個回答」。
            //    已經判定失敗的回答不再被推動（R6）。
            var targets = new List<RuntimeOption>();
            var before = new List<int>();
            var after = new List<int>();

            for (int i = 0; i < options.Count; i++)
            {
                RuntimeOption o = options[i];
                if (!o.available) continue;
                if (!ProbabilityCardRules.Affects(card, o.source.acceptedAttributes)) continue;

                targets.Add(o);
                before.Add(o.currentProbability);

                // 成長公式與 Clamp 都在 ProbabilityCardRules.Apply 裡，這裡不自己算
                o.currentProbability =
                    ProbabilityCardRules.Apply(Data.growth, o.currentProbability, card, Data.probabilityCap);
                after.Add(o.currentProbability);
            }

            // 規格 R7：拖出 hand 就移除，不能再用。
            // R8：沒有同色回答時**卡照樣用掉**，只是沒有效果 —— 要顯示 No Effect
            hand.RemoveAt(idx);

            OnCardPlayed?.Invoke(card, targets, before, after);
            OnHandChanged?.Invoke();

            if (targets.Count == 0)
                Debug.Log($"[機率對話] {card.cardId}（{card.attribute}）沒有可影響的回答 —— No Effect");

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
        /// <summary>
        /// 跑一組效果，並把它們回報的提示存進 <see cref="LastOutcomeNotes"/>。
        ///
        /// ⚠️ **一定要在 `OnEnded` 之前呼叫** —— UI 是在那個事件裡讀結算文字的。
        /// 兩處呼叫端（成功、全部失敗）目前都是這個順序，改的時候不要換過去。
        /// </summary>
        private void RunOutcome(List<EventEffect> effects)
        {
            LastOutcomeNotes = "";

            if (effects == null || effects.Count == 0) return;

            RunContext run = GameFlowManager.Instance != null ? GameFlowManager.Instance.Run : null;
            if (run == null)
            {
                // 離線測試時沒有 run —— 那是正常的，不該吵
                return;
            }

            var notes = new List<string>();

            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i] == null) continue;

                // ⚠️ 回傳值就是給玩家看的那句話。丟掉的話結算永遠不會出現
                string note = effects[i].Apply(run);
                if (!string.IsNullOrEmpty(note)) notes.Add(note);
            }

            LastOutcomeNotes = string.Join(outcomeSeparator, notes.ToArray());
        }
    }
}
