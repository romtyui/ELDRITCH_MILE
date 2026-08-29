using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace EldritchMile.Shop
{
    using EldritchMile.Core;

    /// <summary>
    /// 商店貨架上的一格。
    ///
    /// 【它不知道自己賣的是什麼】格子只負責「顯示一筆商品資料 + 回報被點了」。
    /// 買不買得起、扣不扣得動錢，是 <see cref="ShopPanelUI"/> 與 Stage 的事。
    /// 這跟 <see cref="DialogueOptionUI"/> 只回報 OnClicked 是同一個分工 ——
    /// UI 元件一旦開始自己改遊戲狀態，就沒辦法在別的場合重用了。
    ///
    /// 【圖示還沒有美術】道具沒指定 icon 時，會用 id 算出一個固定的顏色當色塊。
    /// 用雜湊而不是隨機，是為了讓**同一個道具每次進店都是同一個顏色** ——
    /// 顏色會變的話，玩家沒辦法用顏色記東西，測試時也認不出來。
    /// </summary>
    public class ShopSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("元件")]
        [Tooltip("商品圖示。沒有美術時當色塊用")]
        public Image iconImage;

        [Tooltip("商品名。有美術之後可以關掉")]
        public TextMeshProUGUI labelText;

        [Tooltip("價格")]
        public TextMeshProUGUI priceText;

        [Tooltip("數量。1 個時自動隱藏")]
        public TextMeshProUGUI countText;

        [Tooltip("售出後蓋上去的東西（打叉、變暗的板子…）。可留空")]
        public GameObject soldOutOverlay;

        [Header("樣式")]
        [Tooltip("買不起時整格的透明度")]
        [Range(0.1f, 1f)] public float unaffordableAlpha = 0.45f;

        [Tooltip("買不起時價格的顏色")]
        public Color unaffordableColor = new Color(0.85f, 0.35f, 0.35f);

        public Color affordableColor = Color.white;

        [Tooltip("滑鼠移上去時整格乘上的顏色。預設稍微變暗。\n\n" +
                 "【為什麼是變暗不是上浮】跟 EncounterTarget 的「被瞄準」用同一種語言 ——\n" +
                 "玩家在探索時已經學過「變暗＝我正指著它」，商店不該再教一套新的。\n" +
                 "而且上浮會把格子從游標底下抽走（HANDOFF §4.6 那個坑），變暗不會動到版面")]
        public Color hoverTint = new Color(0.78f, 0.78f, 0.78f, 1f);

        /// 被點了。參數是自己，Panel 靠它知道是哪一格。
        public event Action<ShopSlotUI> OnClicked;

        public string ItemId { get; private set; } = "";
        public int Price { get; private set; }
        public int Count { get; private set; }
        public bool IsEmpty => string.IsNullOrEmpty(ItemId);
        public bool SoldOut { get; private set; }

        private CanvasGroup group;

        /// <summary>格子底框。變暗要作用在它與圖示上，所以要記得原本的顏色。</summary>
        private Image frameImage;
        private Color frameBaseColor = Color.white;
        private Color iconBaseColor = Color.white;
        private bool hovered;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();

            frameImage = GetComponent<Image>();
            if (frameImage != null) frameBaseColor = frameImage.color;
        }

        /// <summary>綁一筆商品。</summary>
        public void Bind(string itemId, int count, int price)
        {
            ItemId = itemId ?? "";
            Count = Mathf.Max(1, count);
            Price = Mathf.Max(0, price);
            SoldOut = false;

            ItemData data = GameFlowManager.Item(ItemId);

            if (labelText != null) labelText.text = data != null ? data.Label : ItemId;

            if (priceText != null)
            {
                priceText.gameObject.SetActive(true);
                priceText.text = Price.ToString();
            }

            if (countText != null)
            {
                countText.gameObject.SetActive(Count > 1);
                countText.text = "×" + Count;
            }

            if (iconImage != null)
            {
                // ⚠️ 貨架用的是**彩色**那一張（ShelfIcon），不是持有欄的白色剪影。
                //    沒有彩色版時 ShelfIcon 自己會退回 icon，這裡不必再判一次
                Sprite shelf = data != null ? data.ShelfIcon : null;

                if (shelf != null)
                {
                    iconImage.sprite = shelf;
                    iconImage.color = Color.white;
                }
                else
                {
                    // 沒有美術 → 用 id 算一個固定顏色的色塊。sprite 維持原樣（通常是圓角方塊）
                    iconImage.color = PlaceholderColor(ItemId);
                }
                iconImage.enabled = true;

                // 底色要在**設定完顏色之後**才記 —— 每件商品的色塊都不一樣
                iconBaseColor = iconImage.color;
            }

            if (soldOutOverlay != null) soldOutOverlay.SetActive(false);

            hovered = false;
            ApplyTint();

            group.alpha = 1f;
            group.blocksRaycasts = true;
            gameObject.SetActive(true);
        }

        /// <summary>這一格沒有商品。整格藏起來，不是顯示一個空框。</summary>
        public void SetEmpty()
        {
            ItemId = "";
            hovered = false;
            Count = 0;
            Price = 0;
            SoldOut = false;

            if (soldOutOverlay != null) soldOutOverlay.SetActive(false);
            if (labelText != null) labelText.text = "";
            if (priceText != null) priceText.gameObject.SetActive(false);
            if (countText != null) countText.gameObject.SetActive(false);
            if (iconImage != null) iconImage.enabled = false;

            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        /// <summary>
        /// 賣掉了。**格子留著不刪** —— 貨架上留一個空位是「你買過了」的資訊，
        /// 直接消失的話後面的商品會往前補，玩家會以為自己看錯。
        /// </summary>
        public void MarkSoldOut()
        {
            SoldOut = true;

            if (soldOutOverlay != null) soldOutOverlay.SetActive(true);
            if (priceText != null) priceText.gameObject.SetActive(false);
            if (countText != null) countText.gameObject.SetActive(false);

            group.alpha = unaffordableAlpha;
            group.blocksRaycasts = false;

            hovered = false;
            ApplyTint();
        }

        /// <summary>
        /// 依玩家現在的錢更新樣式。**買不起也不擋點擊** ——
        /// 點下去要能聽到店主說「你錢不夠」，比按不動好懂。
        /// </summary>
        public void SetAffordable(bool affordable)
        {
            if (IsEmpty || SoldOut) return;

            group.alpha = affordable ? 1f : unaffordableAlpha;
            if (priceText != null) priceText.color = affordable ? affordableColor : unaffordableColor;
        }

        // ==========================================
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging) return;
            if (IsEmpty || SoldOut) return;

            OnClicked?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (IsEmpty || SoldOut) return;

            hovered = true;
            ApplyTint();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            ApplyTint();
        }

        /// <summary>
        /// 把 hover 的變暗套上去。
        ///
        /// ⚠️ 基準永遠是**記下來的底色**，不是當下的顏色 ——
        /// 拿當下的顏色去乘，連續進出會一次比一次暗，越疊越黑。
        /// 這跟 `EncounterTargetView.SetTargeted` 是同一段註記，同一個理由。
        /// </summary>
        private void ApplyTint()
        {
            Color t = hovered ? hoverTint : Color.white;

            if (frameImage != null) frameImage.color = Multiply(frameBaseColor, t);
            if (iconImage != null && iconImage.enabled) iconImage.color = Multiply(iconBaseColor, t);
        }

        /// <summary>只乘 RGB，alpha 維持底色的 —— 變暗不該順便改透明度。</summary>
        private static Color Multiply(Color b, Color t)
        {
            return new Color(b.r * t.r, b.g * t.g, b.b * t.b, b.a);
        }

        /// <summary>
        /// id → 固定的色塊顏色。同一個 id 永遠同一個顏色。
        /// 飽和度與明度壓在中間帶，免得抽到接近黑或接近白而看不見文字。
        /// </summary>
        public static Color PlaceholderColor(string id)
        {
            if (string.IsNullOrEmpty(id)) return Color.gray;

            unchecked
            {
                int h = 17;
                for (int i = 0; i < id.Length; i++) h = h * 31 + id[i];

                float hue = Mathf.Abs(h % 360) / 360f;
                return Color.HSVToRGB(hue, 0.45f, 0.75f);
            }
        }
    }
}
