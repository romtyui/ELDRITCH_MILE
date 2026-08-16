using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// C18：打牌環節的回合管理。整份 Core 裡最容易做錯的一塊。
    ///
    /// 【流程】
    ///   1. 玩家可選定一個選項為「主要目標」；未選定時 hover 手牌會廣播全選項預覽
    ///   2. 每回合出一張手牌（不是一次投多張）
    ///   3. 結果即時反應在選項內文與角色對話框
    ///   4. 不滿意可直接再出牌，但該選項成功倍率逐次衰減
    ///   5. 衰減次數 = 手牌總數，出過的牌會消耗
    ///   6. 玩家按結束鈕才結束
    ///   7. 蓄意失敗是合法策略
    ///
    /// 【最重要的一條】第 7 點意味著：**即使判定成功也不可自動結束環節**。
    /// 若寫成「成功即跳出」，蓄意失敗這個玩法就整個沒了。
    /// </summary>
    public class DialogueEncounterController : MonoBehaviour
    {
        /// <summary>
        /// 常駐在 EventScene。因為手牌區與對話框是同一組構圖，兩者都住在場景裡，
        /// 而 Stage prefab 無法在 Inspector 引用場景物件 —— 所以用單例讓 Stage 找得到。
        /// </summary>
        public static DialogueEncounterController Instance { get; private set; }

        [Header("衰減設定 (Q11 待定，先用線性)")]
        [Tooltip("勾選：每次衰減 1/手牌數，讓最後一張剛好接近 0。取消：使用下方固定值")]
        public bool decayScaledToHandSize = true;

        [Tooltip("decayScaledToHandSize 取消勾選時，每次出牌扣掉的倍率")]
        [Range(0f, 1f)] public float fixedDecayStep = 0.2f;

        [Header("嘗試次數提示 (C12)")]
        [Tooltip("第二次嘗試起，附加在判定結果後面的一行。{0} = 這是第幾次。\n" +
                 "留空則不附加。\n\n" +
                 "這是「在試一次？」確認視窗的替代做法：回合感寫進結果文字，不打斷連續嘗試。\n" +
                 "確認視窗每次失敗都跳的話，它傳達的資訊只有第一次是新的，之後就是純摩擦 ——\n" +
                 "而且「再試要付什麼代價」hover 的預覽數字早就答了（會看到機率下降）。")]
        [TextArea(2, 3)]
        public string attemptSuffixFormat = "\n這是你嘗試的第 {0} 次。";

        [Header("狀態 (唯讀)")]
        [SerializeField] private int cardsPlayed;
        [SerializeField] private bool isActive;

        /// C18①：玩家選定的主要目標。null = 未選定，此時才廣播全選項預覽。
        public IProbabilityTarget PrimaryTarget { get; private set; }

        public bool IsActive => isActive;
        public int CardsPlayed => cardsPlayed;
        public int HandCount => hand.Count;
        public bool HasCardsLeft => hand.Count > 0;

        /// 每次衰減扣掉多少。Q11 暫行做法：1 / 手牌總數。
        public float DecayStep { get; private set; } = 0.2f;

        // ⚠️ 存 CardInstanceExplore 而非 CardDataExplore。
        // 用 Data 的話，手上有兩張同名卡時 List.Remove 會移除「第一張符合的」，
        // 玩家出了 B 卻消耗掉 A —— 在牌組會重複的卡牌遊戲裡這是必然會踩到的 bug。
        private readonly List<CardInstanceExplore> hand = new List<CardInstanceExplore>();
        private readonly List<IProbabilityTarget> targets = new List<IProbabilityTarget>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// 手牌用盡、即將自動結束**之前**詢問一次。
        /// 回傳 true 代表有人接手（例如要提供付出代價重來的機會），環節就先不結束。
        ///
        /// 【為什麼要攔在這裡】重試若等到環節結束後才處理，就得把已經拆掉的
        /// 手牌區、對象大圖、對話框狀態整套重建一次，畫面會閃。攔在結束前，
        /// 補一手牌就能無縫接下去。
        ///
        /// 【接手方的責任】回報 true 之後，環節會停在「還在進行中但手牌是空的」狀態，
        /// **必須**由接手方負責收尾 —— 玩家答應就 RefillHand()，拒絕或付不起就 EndEncounter()。
        /// 忘記收尾的話環節會永遠卡著。
        /// </summary>
        public Func<bool> HandExhaustedInterceptor;

        // ── 事件，供 UI 掛載 ──
        public event Action<CardInstanceExplore, IProbabilityTarget, bool, float> OnCardResolved;
        public event Action<IProbabilityTarget> OnPrimaryTargetChanged;
        public event Action OnHandChanged;
        public event Action OnEncounterEnded;

        // ==========================================
        // 開始 / 結束
        // ==========================================
        public void Begin(IReadOnlyList<CardInstanceExplore> startingHand, IReadOnlyList<IProbabilityTarget> encounterTargets)
        {
            hand.Clear();
            targets.Clear();
            cardsPlayed = 0;
            PrimaryTarget = null;

            if (startingHand != null) hand.AddRange(startingHand);

            if (encounterTargets != null)
            {
                for (int i = 0; i < encounterTargets.Count; i++)
                {
                    IProbabilityTarget t = encounterTargets[i];
                    if (t == null) continue;

                    targets.Add(t);
                    HoverPreviewBroadcaster.Instance?.Register(t);
                }
            }

            // C18⑤：衰減次數與手牌總數綁定，手牌本身就是嘗試次數的資源
            DecayStep = decayScaledToHandSize && hand.Count > 0
                ? 1f / hand.Count
                : fixedDecayStep;

            isActive = true;
            OnHandChanged?.Invoke();

            Debug.Log($"[打牌] 開始：手牌 {hand.Count} 張、選項 {targets.Count} 個、每次衰減 {DecayStep:F2}");
        }

        /// <summary>
        /// C18⑥：結束打牌環節。
        /// 只有兩種情況會走到這裡 —— 玩家按結束鈕，或手牌耗盡。
        /// **不可**因為判定成功就自動呼叫（C18⑦）。
        /// </summary>
        /// <summary>
        /// 上一次結束是**玩家主動**的（按結束鈕、拒絕重試），而不是手牌用盡自動結束。
        ///
        /// 【為什麼要分】收尾時會「代替玩家在對話框上點一下」，讓按完結束不必再手動點。
        /// 但那個推進只有在玩家真的按了什麼的時候才成立 —— 手牌用盡是自動結束，
        /// 玩家什麼都沒按，這時候推進會把**剛剛才顯示的判定結果**直接跳掉。
        /// </summary>
        public bool EndedByPlayer { get; private set; } = true;

        public void EndEncounter(bool playerInitiated = true)
        {
            if (!isActive) return;

            EndedByPlayer = playerInitiated;
            isActive = false;
            HoverPreviewBroadcaster.Instance?.End();

            for (int i = 0; i < targets.Count; i++)
            {
                HoverPreviewBroadcaster.Instance?.Unregister(targets[i]);
            }

            Debug.Log($"[打牌] 結束：共出了 {cardsPlayed} 張");
            OnEncounterEnded?.Invoke();
        }

        // ==========================================
        // 主要目標
        // ==========================================
        public void SelectPrimaryTarget(IProbabilityTarget target)
        {
            if (PrimaryTarget == target) return;

            PrimaryTarget = target;

            // C18①：選定後畫面聚焦在該目標，不再廣播全選項預覽
            if (target != null) HoverPreviewBroadcaster.Instance?.End();

            OnPrimaryTargetChanged?.Invoke(target);
            Debug.Log($"[打牌] 主要目標：{(target != null ? target.DisplayName : "（未選定）")}");
        }

        public void ClearPrimaryTarget()
        {
            SelectPrimaryTarget(null);
        }

        /// <summary>
        /// C17/C18①：目前是否應該廣播 hover 預覽。
        /// 呼叫 HoverPreviewBroadcaster.Begin() 前必須先問這個。
        /// </summary>
        public bool ShouldBroadcastPreview => isActive && PrimaryTarget == null;

        // ==========================================
        // 瞄準回饋
        // ==========================================

        /// <summary>手上有一張待命的卡（拖曳中，或兩段式出牌選取中）。由手牌區設定。</summary>
        public bool HasArmedCard { get; set; }

        /// <summary>目前被瞄準的目標。用來讓它變暗，告訴玩家「放開會打在這」。</summary>
        public IProbabilityTarget AimedTarget { get; private set; }

        /// <summary>
        /// 設定瞄準目標。舊的會自動解除，所以呼叫端不必自己記上一個是誰。
        ///
        /// 【為什麼放在 Core】拖曳（手牌區驅動）與 hover（目標自己驅動）兩條路
        /// 都要改同一份狀態。分開記的話會出現「兩個目標同時是暗的」。
        /// </summary>
        public void SetAimed(IProbabilityTarget target)
        {
            if (ReferenceEquals(AimedTarget, target)) return;

            AimedTarget?.SetTargeted(false);
            AimedTarget = target;
            AimedTarget?.SetTargeted(true);
        }

        public void ClearAimed() => SetAimed(null);

        // ==========================================
        // 嘗試次數提示（C12）
        // ==========================================
        /// <summary>
        /// 把「這是第幾次嘗試」附加到判定結果文字後面。**第一次不附加** ——
        /// 第一次沒有「又」的語氣，寫「第 1 次」反而像系統在報數。
        ///
        /// 由各個 IProbabilityTarget 在 OnCheckResult 裡呼叫。
        /// 呼叫時 cardsPlayed 已經加過了（PlayCard 的順序：消耗手牌 → 衰減 → OnCheckResult），
        /// 所以這裡拿到的就是「這一次是第幾次」。
        /// </summary>
        public string WithAttemptLine(string body)
        {
            if (string.IsNullOrEmpty(attemptSuffixFormat)) return body;
            if (cardsPlayed <= 1) return body;

            // 格式字串是企劃在 Inspector 裡填的。少打了 {0} 就直接接在後面，
            // 而不是讓 string.Format 靜靜地把整句結果文字換成例外。
            return attemptSuffixFormat.Contains("{0}")
                ? body + string.Format(attemptSuffixFormat, cardsPlayed)
                : body + attemptSuffixFormat;
        }

        // ==========================================
        // 出牌
        // ==========================================
        /// <summary>
        /// C18②：出一張牌。
        /// 順序固定為 擲骰 → 消耗手牌 → 目標衰減 → 通知結果，不可顛倒
        /// （衰減若早於擲骰，這一張就會吃到自己造成的懲罰）。
        /// </summary>
        public bool PlayCard(CardInstanceExplore card, IProbabilityTarget target)
        {
            if (!isActive)
            {
                Debug.LogWarning("[打牌] 環節尚未開始或已結束");
                return false;
            }

            if (card == null || card.data == null) return false;

            // 已選定主要目標時，出牌一律作用在它身上
            IProbabilityTarget actual = PrimaryTarget ?? target;

            if (actual == null)
            {
                Debug.Log("[打牌] 沒有指定目標");
                return false;
            }

            if (!hand.Contains(card))
            {
                Debug.LogWarning($"[打牌] {card.data.cardName} 不在手牌中");
                return false;
            }

            if (ProbabilityCheck.Instance == null)
            {
                Debug.LogWarning("[打牌] 場上沒有 ProbabilityCheck");
                return false;
            }

            // 1. 擲骰
            float usedRate;
            bool success = ProbabilityCheck.Instance.Roll(card.data, actual, out usedRate);

            // 2. 消耗手牌（C18⑤：出過的牌會消耗）
            hand.Remove(card);
            cardsPlayed++;
            OnHandChanged?.Invoke();

            // 3. 目標衰減（C18④：掛在選項上，不是掛在卡片上）
            actual.ApplyDecay(DecayStep);

            // 4. 即時反饋到選項內文與角色對話框（C18③）
            actual.OnCheckResult(success, usedRate);
            OnCardResolved?.Invoke(card, actual, success, usedRate);

            // ⚠️ 這裡刻意「不」判斷成功就結束 —— 蓄意失敗是合法策略（C18⑦）。
            //    只有手牌耗盡才自動收尾，其餘一律等玩家按結束鈕。
            if (hand.Count == 0)
            {
                // 先問有沒有人要接手（付出代價重來）。有的話環節先不結束，
                // 由接手方負責 RefillHand() 或 EndEncounter()。
                if (HandExhaustedInterceptor != null && HandExhaustedInterceptor())
                {
                    Debug.Log("[打牌] 手牌用盡，等玩家決定要不要付代價重來");
                    return success;
                }

                Debug.Log("[打牌] 手牌用盡，自動結束");

                // ⚠️ playerInitiated: false —— 玩家沒有按任何東西。
                //    傳 true 的話收尾會「代替玩家點一下」，把上面剛顯示的判定結果直接跳掉。
                EndEncounter(false);
            }

            return success;
        }

        /// <summary>
        /// 付出代價後補一手新牌，**繼續同一次遭遇**（不是開新的一次）。
        ///
        /// ⚠️ cardsPlayed 刻意**不重置**。衰減已經被目標重置回初始了，
        /// 「總共在這個目標上耗掉幾張」就成了唯一還看得見的代價紀錄 ——
        /// 而且它正是遞增代價的依據。重置的話玩家會看到「第 1 次」但被收第三次的錢。
        /// </summary>
        public void RefillHand(IReadOnlyList<CardInstanceExplore> newHand)
        {
            if (!isActive)
            {
                Debug.LogWarning("[打牌] 環節已結束，無法補牌");
                return;
            }

            hand.Clear();
            if (newHand != null) hand.AddRange(newHand);

            // 衰減級距要跟著新的手牌數重算，否則補 3 張卻還照 5 張的級距扣
            DecayStep = decayScaledToHandSize && hand.Count > 0
                ? 1f / hand.Count
                : fixedDecayStep;

            OnHandChanged?.Invoke();

            Debug.Log($"[打牌] 補牌 {hand.Count} 張，每次衰減 {DecayStep:F2}（累計已出 {cardsPlayed} 張）");
        }

        public IReadOnlyList<CardInstanceExplore> Hand => hand;
        public IReadOnlyList<IProbabilityTarget> Targets => targets;
    }
}
