using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Explore
{
    using EldritchMile.Core;

    /// <summary>
    /// 手牌區。取代封存的 `ExplorationHandUIController`（它引用了同樣被封存的
    /// `CardExplorationManager` 與 `ExplorationCardDragUI`，已無法運作）。
    ///
    /// 【職責】只管「把 DialogueEncounterController 的手牌畫出來」與「把玩家的出牌動作轉交回去」。
    /// 回合規則、衰減、結束條件全部在 `DialogueEncounterController`（Core），這裡不做任何判斷。
    /// </summary>
    public class ExploreHandUI : MonoBehaviour
    {
        /// <summary>
        /// 常駐在 EventScene 的對話框旁邊 —— 手牌與對話框是同一組構圖。
        /// Stage prefab 無法在 Inspector 引用場景物件，所以靠單例讓 Stage 找得到。
        /// </summary>
        public static ExploreHandUI Instance { get; private set; }

        [Header("繫結")]
        [Tooltip("留空會自動抓 DialogueEncounterController.Instance")]
        public DialogueEncounterController encounter;

        [Header("生成")]
        public CardViewUIExplore cardPrefab;
        public RectTransform handRoot;

        [Tooltip("整個手牌區的開關對象（含結束按鈕等）。留空則使用自身")]
        public GameObject root;

        [Header("排版")]
        [Tooltip("卡片間距")]
        public float cardSpacing = 140f;

        [Tooltip("整排手牌的最大寬度。超過會自動壓縮間距")]
        public float maxHandWidth = 900f;

        [Tooltip("hover 時卡片上浮的距離")]
        public float hoverLift = 40f;

        [Tooltip("被選取（兩段式出牌的第一段）時上浮的距離。\n" +
                 "要明顯大於 Hover Lift，否則玩家分不出「滑過」與「已選取待命」")]
        public float selectedLift = 70f;

        [Header("屬性無效的視覺 (C17)")]
        [Tooltip("對當前目標屬性完全無效（顯示 ✕）的手牌要壓多暗。\n\n" +
                 "這不是裝飾 —— 有了真正的 0% 之後，玩家必須不 hover 每一張就看得出哪些是死牌，" +
                 "否則相剋表根本沒法玩。\n\n" +
                 "⚠️ 變暗的牌**仍然打得出去**（蓄意失敗是合法策略，C18⑦）。" +
                 "這是資訊，不是鎖定")]
        [Range(0.1f, 1f)] public float ineffectiveAlpha = 0.35f;

        /// <summary>
        /// 目前選取的卡（兩段式出牌的第一段）。
        ///
        /// 【為什麼要有兩段式】誤放一張牌的代價很重 —— 消耗一張手牌，而且目標會**永久衰減**。
        /// 拖曳快但容易失手，尤其 Phase 6 的多選項並排時。
        /// 所以拖曳與點選兩軌並存：熟練玩家拖，求穩的玩家點兩下。
        /// </summary>
        public ExploreCardDrag SelectedCard { get; private set; }

        public IProbabilityTarget PrimaryTarget => encounter != null ? encounter.PrimaryTarget : null;

        /// C18①：尚未選定主要目標時才廣播全選項預覽
        public bool ShouldBroadcastPreview => encounter != null && encounter.ShouldBroadcastPreview;

        private readonly List<ExploreCardDrag> spawned = new List<ExploreCardDrag>();
        private ExploreCardDrag hovered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            if (root == null) root = gameObject;
        }

        private void Start()
        {
            // 訂閱放在 Start 而非 OnEnable：手牌區平時是隱藏的，
            // 而 Unity 對「一開始就 inactive」的物件不會執行 OnEnable，會漏訂閱。
            // Awake/Start 在 SetActive(true) 那一刻才跑，時機才對。
            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (Instance == this) Instance = null;
        }

        private void Subscribe()
        {
            if (encounter == null) encounter = DialogueEncounterController.Instance;
            if (encounter == null) return;

            encounter.OnHandChanged -= Rebuild;
            encounter.OnEncounterEnded -= HandleEncounterEnded;
            encounter.OnPrimaryTargetChanged -= HandlePrimaryTargetChanged;

            encounter.OnHandChanged += Rebuild;
            encounter.OnEncounterEnded += HandleEncounterEnded;

            // 換了主要目標，「哪些牌是死的」就換了一套答案（C17）
            encounter.OnPrimaryTargetChanged += HandlePrimaryTargetChanged;
        }

        private void Unsubscribe()
        {
            if (encounter == null) return;
            encounter.OnHandChanged -= Rebuild;
            encounter.OnEncounterEnded -= HandleEncounterEnded;
            encounter.OnPrimaryTargetChanged -= HandlePrimaryTargetChanged;
        }

        private void HandlePrimaryTargetChanged(IProbabilityTarget target)
        {
            RefreshDimming();
        }

        // ==========================================
        // 顯示控制（自己擁有，不交給別人 SetActive）
        // ==========================================
        public void Show()
        {
            Subscribe();
            if (root != null) root.SetActive(true);
        }

        public void Hide()
        {
            Clear();
            if (root != null) root.SetActive(false);
        }

        /// <summary>
        /// C18⑥：「結束」按鈕接這裡。
        ///
        /// 為什麼不直接接 `ExploreStageController.EndEncounter()`：按鈕住在場景、
        /// Stage 控制器住在 prefab，**場景物件無法在 Inspector 引用 prefab 內的東西**。
        /// 手牌區同樣常駐場景，由它轉一手最單純。
        ///
        /// ⚠️ 只有玩家主動按、或手牌用盡才會結束 —— 判定成功不會自動結束（C18⑦）。
        /// </summary>
        public void RequestEnd()
        {
            if (encounter == null) encounter = DialogueEncounterController.Instance;
            if (encounter != null && encounter.IsActive) encounter.EndEncounter();
        }

        // ==========================================
        // 重建手牌
        // ==========================================
        public void Rebuild()
        {
            Clear();

            if (encounter == null || cardPrefab == null || handRoot == null) return;

            IReadOnlyList<CardInstanceExplore> hand = encounter.Hand;

            for (int i = 0; i < hand.Count; i++)
            {
                CardViewUIExplore view = Instantiate(cardPrefab, handRoot);
                view.Bind(hand[i]);

                var drag = view.GetComponent<ExploreCardDrag>();
                if (drag == null) drag = view.gameObject.AddComponent<ExploreCardDrag>();
                drag.Bind(hand[i], this);

                spawned.Add(drag);
            }

            Layout();
            RefreshDimming();
        }

        /// <summary>
        /// C17：把對當前目標完全無效的手牌壓暗。
        ///
        /// 【對哪個目標？】相剋只看屬性，跟衰減無關，所以只有「目標換了」才需要重算 ——
        /// 但重建手牌時順手做掉最省事。
        ///
        /// 已選定主要目標就對它算；沒選定但**場上只有一個目標**時也對那一個算
        /// （目前探索的寶箱就是這個情況）。有多個目標又沒選定時**不變暗** ——
        /// 那時候「無效」沒有唯一答案，硬壓暗會騙人。
        /// 這正是 C18① 主要目標選定在 Phase 6 的實際用途。
        /// </summary>
        public void RefreshDimming()
        {
            if (encounter == null || ProbabilityCheck.Instance == null) return;

            IProbabilityTarget target = encounter.PrimaryTarget;

            if (target == null)
            {
                IReadOnlyList<IProbabilityTarget> targets = encounter.Targets;
                if (targets != null && targets.Count == 1) target = targets[0];
            }

            for (int i = 0; i < spawned.Count; i++)
            {
                ExploreCardDrag card = spawned[i];
                if (card == null || card.Card == null) continue;

                bool ineffective = false;

                if (target != null)
                {
                    ProbabilityCheck.Instance.CalculateRate(card.Card.data, target, out Effectiveness eff);
                    ineffective = eff == Effectiveness.None;
                }

                card.SetDimmed(ineffective, ineffectiveAlpha);
            }
        }

        private void Clear()
        {
            foreach (ExploreCardDrag c in spawned)
            {
                if (c != null) Destroy(c.gameObject);
            }
            spawned.Clear();
            hovered = null;
        }

        /// <summary>水平置中排列。手牌多時自動壓縮間距，避免超出畫面。</summary>
        private void Layout()
        {
            int n = spawned.Count;
            if (n == 0) return;

            float spacing = cardSpacing;
            if (spacing * (n - 1) > maxHandWidth && n > 1)
            {
                spacing = maxHandWidth / (n - 1);
            }

            float startX = -spacing * (n - 1) * 0.5f;

            for (int i = 0; i < n; i++)
            {
                var rt = spawned[i].GetComponent<RectTransform>();
                if (rt == null) continue;

                // 選取優先於 hover —— 選取是持續狀態，hover 只是滑過
                float lift = 0f;
                if (spawned[i] == SelectedCard) lift = selectedLift;
                else if (spawned[i] == hovered) lift = hoverLift;

                rt.anchoredPosition = new Vector2(startX + spacing * i, lift);
                rt.SetSiblingIndex(i);
            }

            // 提到最上層才不會被旁邊的卡蓋住。選取的優先權高於 hover。
            if (hovered != null) hovered.transform.SetAsLastSibling();
            if (SelectedCard != null) SelectedCard.transform.SetAsLastSibling();
        }

        // ==========================================
        // 給 ExploreCardDrag 回呼
        // ==========================================
        public void NotifyCardHovered(ExploreCardDrag card)
        {
            hovered = card;
            Layout();
        }

        public void NotifyCardUnhovered(ExploreCardDrag card)
        {
            if (hovered == card) hovered = null;
            Layout();
        }

        // ==========================================
        // 兩段式出牌：選卡 → 點目標
        // ==========================================

        /// <summary>點卡片：沒選就選起來，已選就取消。</summary>
        public void ToggleSelect(ExploreCardDrag card)
        {
            SelectedCard = (SelectedCard == card) ? null : card;
            RefreshPreviewForSelection();
            Layout();
        }

        /// <summary>
        /// 依「目前有沒有選取的卡」決定預覽的顯示狀態。
        ///
        /// 選取中就持續顯示該卡的機率 —— 兩段式出牌時玩家要把滑鼠從卡片移到目標上，
        /// 中間必然離開卡片，這時候預覽不能消失，否則正要下決定的那一刻反而沒有資訊。
        /// </summary>
        public void RefreshPreviewForSelection()
        {
            if (SelectedCard != null && SelectedCard.Card != null && ShouldBroadcastPreview)
            {
                HoverPreviewBroadcaster.Instance?.Begin(SelectedCard.Card.data);
            }
            else
            {
                HoverPreviewBroadcaster.Instance?.End();
            }
        }

        public void ClearSelection()
        {
            if (SelectedCard == null) return;

            SelectedCard = null;
            HoverPreviewBroadcaster.Instance?.End();
            Layout();
        }

        /// <summary>
        /// 兩段式的第二段：玩家點了目標。
        /// 回傳 true 代表這次點擊被當成出牌消化掉了，目標不該再執行它自己的 Interact()。
        /// </summary>
        public bool TryPlaySelectedOn(IProbabilityTarget target)
        {
            if (SelectedCard == null || target == null) return false;

            ExploreCardDrag card = SelectedCard;
            SelectedCard = null;
            HoverPreviewBroadcaster.Instance?.End();

            return TryPlay(card, target);
        }

        /// <summary>玩家把卡放到目標上。真正的規則判斷交給 Core。</summary>
        public bool TryPlay(ExploreCardDrag card, IProbabilityTarget target)
        {
            if (encounter == null || card == null || card.Card == null) return false;

            // PlayCard 成功與否指的是「判定成功」，不是「出牌合法」。
            // 出牌合法時 OnHandChanged 會觸發 Rebuild，卡片自然被重建掉，
            // 所以這裡只要回報「手牌數量有沒有變」即可。
            int before = encounter.HandCount;
            encounter.PlayCard(card.Card, target);
            return encounter.HandCount != before;
        }

        private void HandleEncounterEnded()
        {
            Clear();
        }
    }
}
