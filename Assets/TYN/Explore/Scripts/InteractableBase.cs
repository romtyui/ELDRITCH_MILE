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
        [Header("基本")]
        public string displayName = "未命名物件";

        [Tooltip("互動一次後就不能再互動")]
        public bool singleUse = true;

        [Header("視覺切換")]
        [Tooltip("留空會自動抓同物件上的 SpriteRenderer")]
        public SpriteRenderer targetRenderer;

        [Tooltip("互動後替換的圖（例如打開的寶箱）。留空則不換")]
        public Sprite interactedSprite;

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

        [Header("游標 (C8)")]
        [Tooltip("滑過時是否顯示可抓取的手勢")]
        public bool showGrabCursor = true;

        protected bool hasInteracted;
        protected RoomController room;

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
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!CanInteract) return;
            Interact();
        }

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

            if (targetRenderer != null && interactedSprite != null)
            {
                targetRenderer.sprite = interactedSprite;
            }

            SetCursor(CursorType.Idle);

            if (room != null) room.ReportInteracted(this);
        }

        protected static void SetCursor(CursorType type)
        {
            if (CursorManager.Instance != null) CursorManager.Instance.SetCursor(type);
        }
    }
}
