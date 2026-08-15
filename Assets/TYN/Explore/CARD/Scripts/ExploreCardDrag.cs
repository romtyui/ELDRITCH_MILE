using UnityEngine;
using UnityEngine.EventSystems;

namespace EldritchMile.Explore
{
    using EldritchMile.Core;

    /// <summary>
    /// 探索卡的拖曳與 hover。取代封存的兩套平行實作
    /// （`ExplorationCardDragUI` 與 `CardDragUIExplore`）。
    ///
    /// 【C17 hover 預覽】滑上卡片時廣播給所有選項，讓它們各自顯示對這張卡的成功率
    /// —— 對應企劃草圖的「A 50 / B 50 / C 50」。
    /// **只在尚未選定主要目標時廣播**（C18①）；選定之後畫面應該聚焦在該目標。
    ///
    /// 【出牌】拖到目標上放開就是出牌。已選定主要目標時，不論拖到哪都作用在該目標上。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ExploreCardDrag : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("拖曳")]
        [Tooltip("拖超過這個距離才算出牌，避免手抖誤觸")]
        public float playThresholdPixels = 80f;

        [Tooltip("拖曳時的縮放")]
        public float dragScale = 0.9f;

        // 「被選取時上浮多少」屬於排版參數，跟 hoverLift 一起放在 ExploreHandUI ——
        // 排版是它在做，參數散在兩個物件上會變成調一個要開 prefab、調另一個要開場景。

        public CardInstanceExplore Card { get; private set; }
        public bool IsDragging { get; private set; }

        private ExploreHandUI hand;
        private RectTransform rect;
        private CanvasGroup canvasGroup;
        private Canvas rootCanvas;

        private Vector2 startAnchoredPos;
        private Vector2 pointerDownPos;
        private Transform startParent;
        private int startSiblingIndex;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            rootCanvas = GetComponentInParent<Canvas>();
        }

        public void Bind(CardInstanceExplore card, ExploreHandUI owner)
        {
            Card = card;
            hand = owner;
        }

        /// <summary>
        /// C17：這張牌對當前目標屬性完全無效（`Effectiveness.None`）時變暗。
        ///
        /// 【只調 alpha，絕不動 blocksRaycasts / interactable】兩個理由：
        ///   1. **死牌必須仍然打得出去** —— 蓄意失敗是合法策略（C18⑦）。
        ///      打一張死牌會消耗手牌並讓目標衰減，那是有意義的操作。
        ///      所以變暗是「資訊」，不是「鎖定」。
        ///   2. 拖曳流程本來就在借用 blocksRaycasts（OnBeginDrag 會關掉它讓 raycast
        ///      穿透自己），這裡再去改它兩邊會打架。
        ///
        /// 也不要改用 image.enabled —— 停用的 Graphic 收不到任何 raycast 且不報錯，
        /// 卡片會變成看得到卻拖不動。
        /// </summary>
        public void SetDimmed(bool dimmed, float dimmedAlpha)
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) return;

            canvasGroup.alpha = dimmed ? Mathf.Clamp01(dimmedAlpha) : 1f;
        }

        /// <summary>
        /// 點選 = 兩段式出牌的第一段。第二段是點目標。
        ///
        /// 【為什麼不用 TwoStageConfirm】那個元件的 arm 與 confirm 都發生在同一個物件上；
        /// 出牌是「在卡片上啟動、在目標上確認」，跨兩個物件，形狀不合。
        /// 概念相同，但落點不一樣。
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            // 拖曳結束時 Unity 也會送 click，要濾掉，否則放開牌會順便把它選起來
            if (eventData.dragging) return;

            hand?.ToggleSelect(this);
        }

        // ==========================================
        // Hover → C17 全選項預覽
        // ==========================================
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (IsDragging || Card == null) return;

            hand?.NotifyCardHovered(this);

            // C18①：已選定主要目標時不廣播
            if (hand != null && hand.ShouldBroadcastPreview)
            {
                HoverPreviewBroadcaster.Instance?.Begin(Card.data);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (IsDragging) return;

            hand?.NotifyCardUnhovered(this);

            // ⚠️ 有選取中的卡就不能關預覽。
            // 兩段式出牌時玩家必須把滑鼠從卡片移到目標上，中間必然會離開卡片 ——
            // 無條件 End() 會讓機率在「正要點目標」的那一刻消失，等於沒得參考。
            hand?.RefreshPreviewForSelection();
        }

        // ==========================================
        // 拖曳
        // ==========================================
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Card == null) return;

            IsDragging = true;
            pointerDownPos = eventData.position;
            startAnchoredPos = rect.anchoredPosition;
            startParent = transform.parent;
            startSiblingIndex = transform.GetSiblingIndex();

            // 拖曳時提到最上層，並讓 raycast 穿透自己才打得到底下的目標
            if (rootCanvas != null) transform.SetParent(rootCanvas.transform, true);
            transform.SetAsLastSibling();
            canvasGroup.blocksRaycasts = false;
            transform.localScale = Vector3.one * dragScale;

            // 拖曳中持續顯示預覽，讓玩家看得到自己正拖向哪個目標
            if (hand != null && hand.ShouldBroadcastPreview)
            {
                HoverPreviewBroadcaster.Instance?.Begin(Card.data);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsDragging) return;
            rect.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!IsDragging) return;

            IsDragging = false;
            canvasGroup.blocksRaycasts = true;
            HoverPreviewBroadcaster.Instance?.End();

            bool draggedFarEnough =
                Vector2.Distance(eventData.position, pointerDownPos) >= playThresholdPixels;

            IProbabilityTarget target = draggedFarEnough ? FindTargetUnder(eventData) : null;

            bool played = target != null && hand != null && hand.TryPlay(this, target);

            if (!played) ReturnToHand();
        }

        /// <summary>找放開位置底下的判定目標。UI 與世界物件都要找。</summary>
        private IProbabilityTarget FindTargetUnder(PointerEventData eventData)
        {
            // 已選定主要目標時，出牌一律作用在它身上（C18①），不必看放在哪
            if (hand != null && hand.PrimaryTarget != null) return hand.PrimaryTarget;

            // UI 目標
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (RaycastResult r in results)
            {
                var t = r.gameObject.GetComponentInParent<IProbabilityTarget>();
                if (t != null) return t;
            }

            // 世界空間目標（2D Collider）
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector2 world = cam.ScreenToWorldPoint(eventData.position);
                Collider2D hit = Physics2D.OverlapPoint(world);
                if (hit != null)
                {
                    var t = hit.GetComponentInParent<IProbabilityTarget>();
                    if (t != null) return t;
                }
            }

            return null;
        }

        public void ReturnToHand()
        {
            if (startParent != null) transform.SetParent(startParent, false);
            transform.SetSiblingIndex(startSiblingIndex);
            rect.anchoredPosition = startAnchoredPos;
            transform.localScale = Vector3.one;
        }
    }
}
