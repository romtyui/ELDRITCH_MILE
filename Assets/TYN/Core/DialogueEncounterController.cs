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
        public void EndEncounter()
        {
            if (!isActive) return;

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
                Debug.Log("[打牌] 手牌用盡，自動結束");
                EndEncounter();
            }

            return success;
        }

        public IReadOnlyList<CardInstanceExplore> Hand => hand;
        public IReadOnlyList<IProbabilityTarget> Targets => targets;
    }
}
