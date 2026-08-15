using UnityEngine;
using UnityEngine.EventSystems;

namespace EldritchMile.Explore
{
    using EldritchMile.Core;

    /// <summary>
    /// 場景互動物件的共同基底。
    ///
    /// 【取代什麼】封存的 InspectableObject / ContainerObject 各自實作了一遍
    /// hover、點擊、圖片替換、回報房間 —— 重複而且行為不一致。這裡收斂成一份。
    ///
    /// 【C8 游標】滑過可互動物件時切成「張開的手」，點下去變「握拳」。
    /// CursorManager 已有 HoverChest / HoldChest 兩個狀態可直接用。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class InteractableBase : MonoBehaviour,
        IInteractable, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>互動完成後這個物件怎麼處理。</summary>
        public enum AfterInteract
        {
            /// 有 Interacted Sprite 就換圖留著；沒有就從畫面消失。
            /// 這對應直覺：撿走的道具不該還躺在原地，開過的箱子則應該留著（換成開啟的圖）。
            Auto,

            /// 一律留在畫面上
            KeepVisible,

            /// 一律消失
            Disappear,
        }

        [Header("基本")]
        public string displayName = "未命名物件";

        [Tooltip("互動一次後就不能再互動")]
        public bool singleUse = true;

        [Tooltip("互動完成後怎麼處理這個物件")]
        public AfterInteract afterInteract = AfterInteract.Auto;

        [Header("視覺切換")]
        [Tooltip("留空會自動抓同物件上的 SpriteRenderer")]
        public SpriteRenderer targetRenderer;

        [Tooltip("互動後替換的圖（例如打開的寶箱）。留空則不換")]
        public Sprite interactedSprite;

        [Tooltip("判定**徹底失敗**後替換的圖（例如撬壞的鎖）。留空則維持原本的圖。\n\n" +
                 "⚠️ 不要跟上面的 Interacted Sprite 共用 —— 那張是「打開的寶箱」，" +
                 "沒撬開的箱子長成打開的樣子會直接誤導玩家以為自己成功了")]
        public Sprite failedSprite;

        [System.Serializable]
        public class VisualVariant
        {
            [Tooltip("未互動時的圖")]
            public Sprite normal;

            [Tooltip("互動後的圖。留空則沿用上方的 Interacted Sprite")]
            public Sprite interacted;
        }

        [Header("隨機外觀 (C6)")]
        [Tooltip("同一個物件的不同轉向／樣式。生成時隨機挑一組，留空則使用上方固定的圖。\n" +
                 "⚠️ 若圖本身已有不同轉向，建議把 SpawnSlot 的 Rotation Range 設成 0,0，避免又轉一次")]
        public System.Collections.Generic.List<VisualVariant> visualVariants = new System.Collections.Generic.List<VisualVariant>();

        [Header("特寫圖")]
        [Tooltip("互動時借用對話框的立繪位置顯示的放大圖。\n" +
                 "冒險遊戲的慣例：把注意力從場景拉到「你正在處理的這個東西」上。\n" +
                 "留空則不顯示，立繪位置維持空白")]
        public Sprite closeUpSprite;

        [Header("游標 (C8)")]
        [Tooltip("滑過時是否顯示可抓取的手勢")]
        public bool showGrabCursor = true;

        protected bool hasInteracted;
        protected RoomController room;

        /// 已結案，而且是**失敗**收場（不是成功處理完）。供子類別給出不同的提示文字。
        public bool FailedPermanently { get; protected set; }

        /// 所屬的探索 Stage。房間生成在 Stage 底下，所以往上找得到。
        protected ExploreStageController stage;

        public string DisplayName => displayName;
        public bool CanInteract => !(singleUse && hasInteracted);
        public bool ShowGrabCursor => showGrabCursor && CanInteract;

        protected virtual void Awake()
        {
            if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
        }

        protected virtual void Start()
        {
            room = GetComponentInParent<RoomController>();
            if (room != null) room.Register(this);

            stage = GetComponentInParent<ExploreStageController>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // 兩段式出牌的第二段：手上有選取的卡就是要打在這，
            // 不是要執行這個物件本來的互動（例如又跳一次「上了鎖」的提示）。
            if (stage != null && this is IProbabilityTarget target &&
                stage.TryPlaySelectedCardOn(target))
            {
                return;
            }

            if (!CanInteract)
            {
                OnInteractBlocked();
                return;
            }

            Interact();
        }

        /// <summary>
        /// 已經處理完的物件又被點了一次。預設沉默 —— 撿走的道具不需要再說什麼。
        ///
        /// 但**徹底失敗**的物件沉默會很糟：玩家看到箱子還在、游標卻沒反應，
        /// 分不出「這已經結束了」與「遊戲卡住了」。子類別覆寫成一句話即可。
        /// </summary>
        protected virtual void OnInteractBlocked() { }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!ShowGrabCursor) return;
            SetCursor(CursorType.HoverChest);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetCursor(CursorType.Idle);
        }

        /// <summary>
        /// 子類別實作實際行為。完成後記得呼叫 MarkDone()。
        /// </summary>
        public abstract void Interact();

        /// <summary>
        /// C6：生成時隨機挑一組外觀。由 RoomController 在 Instantiate 之後呼叫，
        /// 用房間的 seed 保證同一場 run 進同一個房間看到的是同一組圖。
        /// </summary>
        public virtual void ApplyRandomVariant(System.Random rng)
        {
            if (visualVariants == null || visualVariants.Count == 0) return;

            VisualVariant v = visualVariants[rng.Next(visualVariants.Count)];
            if (v == null) return;

            if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();

            if (v.normal != null && targetRenderer != null) targetRenderer.sprite = v.normal;
            if (v.interacted != null) interactedSprite = v.interacted;
        }

        /// <summary>
        /// 標記為已互動：換圖、切游標、回報房間。
        /// 子類別在「這次互動真的完成了」時呼叫 —— 注意判定失敗**不算**完成，
        /// 因為 C12 允許再試一次。
        /// </summary>
        protected void MarkDone()
        {
            hasInteracted = true;
            SetCursor(CursorType.Idle);

            bool hasSwapSprite = interactedSprite != null;

            bool disappear =
                afterInteract == AfterInteract.Disappear ||
                (afterInteract == AfterInteract.Auto && !hasSwapSprite);

            if (disappear)
            {
                // 先回報再隱藏 —— ReportInteracted 可能會觸發「房間清空」的後續流程，
                // 物件已停用時仍需正常計數
                if (room != null) room.ReportInteracted(this);
                gameObject.SetActive(false);
                return;
            }

            if (hasSwapSprite && targetRenderer != null)
            {
                targetRenderer.sprite = interactedSprite;
            }

            if (room != null) room.ReportInteracted(this);
        }

        /// <summary>
        /// 標記為**失敗結案**：嘗試機會用盡但沒成功。
        ///
        /// 與 MarkDone() 的三個差別：
        ///   1. 換的是 failedSprite（撬壞的鎖），不是 interactedSprite（打開的箱子）
        ///   2. **絕不消失** —— 沒撬開的箱子憑空不見會讓玩家以為自己成功了。
        ///      所以這裡完全不看 afterInteract
        ///   3. 設 FailedPermanently，讓再次點擊時能給出「已經弄壞了」而不是沉默
        ///
        /// 一樣要回報房間 —— 失敗也是一種「處理完了」，
        /// 不回報的話 C13 的房間清空永遠不會觸發。
        /// </summary>
        protected void MarkFailed()
        {
            if (hasInteracted) return;   // 已經成功開過了，這次呼叫不算數

            hasInteracted = true;
            FailedPermanently = true;
            SetCursor(CursorType.Idle);

            if (failedSprite != null)
            {
                if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
                if (targetRenderer != null) targetRenderer.sprite = failedSprite;
            }

            if (room != null) room.ReportInteracted(this);
        }

        protected static void SetCursor(CursorType type)
        {
            if (CursorManager.Instance != null) CursorManager.Instance.SetCursor(type);
        }
    }
}
