using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace EldritchMile.Core
{
    /// <summary>
    /// 打牌環節中，互動對象在對話框裡的化身。
    ///
    /// 【為什麼需要它】原本卡片是拖到世界裡的寶箱上。三個問題：
    ///   1. 世界物件可能被對話框或其他東西蓋住，當拖曳目標不可靠
    ///   2. 要讓卡片打得到世界，就得讓 raycast 穿透對話框 —— 那又會變成
    ///      「打牌時還能點到背景」，互動邊界模糊
    ///   3. 機率標籤浮在世界物件頭上，位置受場景擺設影響，時常被遮
    ///
    /// 所以進入打牌環節後，互動主體整個搬進對話框：大圖是拖曳目標、
    /// 機率顯示在大圖頭上、壓黑層負責擋住世界。玩家的注意力與可點範圍一致。
    ///
    /// 【實作】本類別實作 IProbabilityTarget 但**不持有任何狀態** ——
    /// 屬性、衰減進度、判定結果全部轉發給真正的目標（世界裡的寶箱／NPC）。
    /// 它只負責「長什麼樣」與「接收點擊」。
    /// </summary>
    public class EncounterTargetView : MonoBehaviour, IProbabilityTarget, IPointerClickHandler
    {
        /// <summary>
        /// 玩家點了這個大圖。兩段式出牌的第二段接這裡。
        ///
        /// 用事件而非直接呼叫，是為了讓 Core 不必反過來認識 Explore ——
        /// 由 ExploreStageController 在生成時訂閱。
        /// </summary>
        public event Action<EncounterTargetView> OnClicked;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging) return;   // 拖曳放開時 Unity 也會送 click，要濾掉
            OnClicked?.Invoke(this);
        }

        [Header("元件")]
        [Tooltip("顯示對象的大圖")]
        public Image image;

        [Tooltip("機率預覽標籤。設成 Image 的子物件並錨定 top-center，就會自動跟著圖的尺寸走")]
        public TextMeshProUGUI previewLabel;

        [Header("尺寸")]
        [Tooltip("大圖能佔用的最大範圍。實際尺寸會依圖的比例縮進這個範圍內，\n" +
                 "不是拉伸填滿 —— 所以寬扁的木箱與細長的立繪都不會變形")]
        public Vector2 maxSize = new Vector2(420f, 520f);

        [Tooltip("勾選則小圖不會被放大，維持原始像素尺寸（適合像素美術）")]
        public bool neverUpscale = false;

        [Header("預覽樣式")]
        public Color normalColor = Color.white;
        public Color immuneColor = new Color(0.5f, 0.5f, 0.5f);

        [Tooltip("屬性完全不合時顯示什麼。不要用「0%」—— 要讓玩家看得出是屬性問題而非運氣差")]
        public string immuneText = "✕";

        /// 真正的判定對象。狀態的唯一真相在它身上。
        public IProbabilityTarget Source { get; private set; }

        public void Bind(IProbabilityTarget source, Sprite closeUp)
        {
            Source = source;

            if (closeUp == null)
            {
                // 沒有特寫圖 → Image 沒東西可畫。若順手把 Image 停用，
                // 它連 raycast 都收不到，於是「看不見」又「點不到」，
                // 而且完全不會有錯誤訊息 —— 非常難查，所以在這裡明講。
                Debug.LogWarning(
                    $"[打牌] 「{(source != null ? source.DisplayName : name)}」沒有設定 Close Up Sprite。\n" +
                    "對話框裡不會有圖，卡片也就沒有東西可以打。\n" +
                    "請在該互動物件的 Inspector 設定 Close Up Sprite。",
                    this
                );
            }

            if (image != null)
            {
                image.sprite = closeUp;

                // ⚠️ 大圖是 UI，靠 GraphicRaycaster + raycastTarget 接收拖放，
                //    **不是** Collider2D（那是世界物件用的）。
                //    而且 Image 必須保持 enabled —— 停用的 Graphic 收不到任何 raycast。
                image.enabled = true;
                image.raycastTarget = true;

                // 沒有圖時用全透明避免畫出白方塊，但仍保有可點區域
                image.color = closeUp != null
                    ? new Color(image.color.r, image.color.g, image.color.b, 1f)
                    : new Color(image.color.r, image.color.g, image.color.b, 0f);

                FitToSprite(closeUp);
            }

            HidePreview();
        }

        /// <summary>
        /// 依圖的比例調整 RectTransform，讓它「就是那張圖的大小」。
        ///
        /// 【為什麼不用 Preserve Aspect】那個只是把圖 letterbox 在原本的框裡，
        /// RectTransform 仍是框的尺寸。結果是：
        ///   · 點擊範圍包含左右（或上下）的空白邊
        ///   · 錨在頂端的機率標籤會浮在空白上方，而不是貼著圖
        /// 直接改 RectTransform 就沒有這些落差，寬扁的木箱與細長的立繪都各自合身。
        /// </summary>
        private void FitToSprite(Sprite sprite)
        {
            if (sprite == null) return;

            var rt = image.rectTransform;
            float w = sprite.rect.width;
            float h = sprite.rect.height;
            if (w <= 0f || h <= 0f) return;

            float scale = Mathf.Min(maxSize.x / w, maxSize.y / h);
            if (neverUpscale) scale = Mathf.Min(scale, 1f);

            rt.sizeDelta = new Vector2(w * scale, h * scale);

            // 尺寸是自己算的，preserveAspect 就沒有必要了；
            // 留著也不會錯，但關掉比較不會讓人誤以為是它在負責。
            image.preserveAspect = false;
        }

        // ==========================================
        // IProbabilityTarget —— 全部轉發，不自己存狀態
        // ==========================================
        public string DisplayName => Source != null ? Source.DisplayName : name;

        public ExploreAttribute Attribute =>
            Source != null ? Source.Attribute : ExploreAttribute.None;

        public float CurrentDecayMultiplier =>
            Source != null ? Source.CurrentDecayMultiplier : 1f;

        public void ApplyDecay(float step)
        {
            Source?.ApplyDecay(step);
        }

        public void OnCheckResult(bool success, float usedRate)
        {
            // 先讓真正的目標處理結果（開箱、給道具、變更狀態），
            // 它可能會透過 PopupService 顯示後續文字
            Source?.OnCheckResult(success, usedRate);
        }

        public void ShowPreview(float rate, Effectiveness eff)
        {
            if (previewLabel == null) return;

            previewLabel.gameObject.SetActive(true);

            if (eff == Effectiveness.None)
            {
                previewLabel.text = immuneText;
                previewLabel.color = immuneColor;
            }
            else
            {
                previewLabel.text = Mathf.RoundToInt(rate * 100f).ToString();
                previewLabel.color = normalColor;
            }
        }

        public void HidePreview()
        {
            if (previewLabel != null) previewLabel.gameObject.SetActive(false);
        }
    }
}
