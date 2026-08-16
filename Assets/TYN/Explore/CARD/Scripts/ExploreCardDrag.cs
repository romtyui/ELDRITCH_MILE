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

        /// <summary>
        /// 視覺子層。上浮只動這一層，**卡片根物件（＝可點區域）永遠不動**。
        ///
        /// 【為什麼一定要分開】上浮 55px、卡片高 263px —— 游標停在卡片下緣時，
        /// 上浮會把卡片從游標底下抽走 → Unity 送出 exit → 卡片落下 → 游標又進來 → enter，
        /// 一秒鐘閃好幾次。**這是「hover 改變了被 hover 的東西」的典型死結。**
        ///
        /// 拖曳仍然移動根物件（那時整張卡本來就該跟著滑鼠走）。
        /// </summary>
        private RectTransform visualRoot;

        /// <summary>由 ExploreHandUI 在生成卡片時建立視覺層並指定。</summary>
        public void SetVisualRoot(RectTransform root)
        {
            visualRoot = root;
        }

        /// <summary>設定上浮距離。只動視覺層，可點區域維持原位。</summary>
        public void SetLift(float lift)
        {
            if (visualRoot == null) return;
            visualRoot.anchoredPosition = new Vector2(0f, lift);
        }

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

            // 「要給誰看」的決策集中在 ExploreHandUI —— 卡片不該知道有沒有主要目標
            hand?.ShowPreviewFor(Card.data);
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
            hand?.SetLayerLocked(true);   // 拖曳期間手牌區維持在最上層
            pointerDownPos = eventData.position;
            startAnchoredPos = rect.anchoredPosition;
            startParent = transform.parent;
            startSiblingIndex = transform.GetSiblingIndex();

            // 移到專用的拖曳層（永遠在 Canvas 最上層），並讓 raycast 穿透自己才打得到底下的目標。
            //
            // ⚠️ 不要用「丟到 Canvas 底下 + SetAsLastSibling」—— 手牌區的 HoverRaiseLayer
            //    同一時間也在搶「最後一個」的位置，誰贏看執行順序，卡片會有機會被壓到對話框後面。
            RectTransform layer = hand != null ? hand.DragLayer : null;

            if (layer != null) transform.SetParent(layer, true);
            else if (rootCanvas != null) transform.SetParent(rootCanvas.transform, true);

            transform.SetAsLastSibling();
            canvasGroup.blocksRaycasts = false;
            transform.localScale = Vector3.one * dragScale;

            // TODO 暫時診斷（拖曳圖層確認後移除）
            Debug.Log($"[拖曳] 開始：父層={transform.parent.name}  siblingIndex={transform.GetSiblingIndex()}" +
                      $"  父層在 Canvas 的 index={transform.parent.GetSiblingIndex()}");

            // 拖曳中持續顯示預覽，讓玩家看得到自己正拖向哪個目標
            hand?.ShowPreviewFor(Card.data);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsDragging) return;
            rect.position = eventData.position;

            // 每幀更新「放開會打到誰」。這是拖曳時唯一能讓玩家知道自己瞄到哪的線索 ——
            // 卡片跟著游標走，目標又可能被對話框壓住一半，光看位置判斷不出來。
            DialogueEncounterController e = DialogueEncounterController.Instance;
            if (e != null) e.SetAimed(FindTargetUnder(eventData));
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!IsDragging) return;

            IsDragging = false;
            hand?.SetLayerLocked(false);
            canvasGroup.blocksRaycasts = true;
            HoverPreviewBroadcaster.Instance?.End();
            DialogueEncounterController.Instance?.ClearAimed();

            bool draggedFarEnough =
                Vector2.Distance(eventData.position, pointerDownPos) >= playThresholdPixels;

            // TODO 暫時診斷（拖放判定確認後移除）。
            //     ⚠️ 一定要放在這裡而不是 FindTargetUnder 裡面 ——
            //        那支現在每幀都會被呼叫（瞄準回饋），寫在裡面 Console 會被洗爆。
            {
                var dbg = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, dbg);

                var lines = new System.Text.StringBuilder();
                lines.Append($"[拖放] 放開於 {eventData.position}，命中 {dbg.Count} 個：");
                for (int i = 0; i < dbg.Count && i < 4; i++)
                {
                    var go = dbg[i].gameObject;
                    var tgt = go.GetComponentInParent<IProbabilityTarget>();
                    lines.Append($"\n   [{i}] {go.name} 判定目標={(tgt != null ? tgt.GetType().Name : "無")}");
                }
                Debug.Log(lines.ToString());
            }

            IProbabilityTarget target = draggedFarEnough ? FindTargetUnder(eventData) : null;

            bool played = target != null && hand != null && hand.TryPlay(this, target);

            if (!played) ReturnToHand();
        }

        /// <summary>找放開位置底下的判定目標。UI 與世界物件都要找。</summary>
        private IProbabilityTarget FindTargetUnder(PointerEventData eventData)
        {
            // 已選定主要目標時，出牌一律作用在它身上（C18①），不必看放在哪
            if (hand != null && hand.PrimaryTarget != null) return hand.PrimaryTarget;

            // UI 目標。
            //
            // ⚠️ **只認最上層那一個**，不可以掃過整份清單。
            //
            // RaycastAll 會回傳游標底下**所有**命中物，包含被別的東西完全遮住的。
            // 掃全部的話，只要判定目標在畫面上的某個位置藏著（例如特寫圖被對話框壓在後面），
            // 玩家把卡放在對話框中間也會被判定成「打在那個目標上」——
            // 明明畫面上根本看不到目標，牌卻出去了。
            //
            // 只認最上層＝「放在你看得到的東西上」，跟玩家的直覺一致。
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            if (results.Count > 0)
            {
                // ⚠️ 命中了 UI 就到此為止，**不可以再往世界找**。
                //
                // 世界裡的寶箱本身就是 IProbabilityTarget，而它躺在對話框後面。
                // 繼續往下找的話，把卡放在對話框正中間（UI 最上層是 text_box，不是目標）
                // 也會穿透過去打中寶箱 —— 玩家看到的是文字框，牌卻出去了。
                //
                // 語意：**點在 UI 上就由 UI 決定**；只有放在空白處才輪到世界物件。
                var t = results[0].gameObject.GetComponentInParent<IProbabilityTarget>();
                return t;   // 找不到就是 null ＝ 這一放不算出牌
            }

            // 世界空間目標（2D Collider）。
            // 只有「UI 完全沒命中」才會走到這裡 —— 也就是放在畫面的空白處。
            //
            // 【還留著的理由】對象化身（EncounterTargetView）生成失敗時，
            // 判定目標會退回世界裡的物件本身。那條路已經會噴警告，但不該連拖放都失效。
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
