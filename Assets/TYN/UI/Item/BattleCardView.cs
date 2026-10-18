using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EldritchMile.UI
{
    /// <summary>
    /// 武器在「牌」上的樣子：卡面 → 武器圖 → 卡框三層，加上**費用與效果文字**。
    /// 商店貨架與購買視窗的大圖都用這支。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【文字從哪來】（2026-10-18 Romtyui 回報：商店卡面上的效果文字掉了）
    /// 舊版只有三層圖、從來沒有字。效果文字的組法（token 換數值、上色、關鍵字）
    /// 是 Romtyui 的 <see cref="CardViewUI"/> 在做 —— 抄一份就是兩個真相，他改規則這裡就錯。
    /// 所以這裡掛一張**藏起來的** card_template（<see cref="cardView"/>），
    /// 只拿它 Bind 之後組好的字串，貼到我們自己的 <see cref="effectLabel"/> 上。
    /// Bind 在物件隱藏時也會跑（純字串處理），不需要它出現在畫面上。
    ///
    /// 【為什麼不直接顯示他的牌】他的 card_template 把 768x1166 的卡框畫成近正方形，
    /// 放上貨架會變寬、擠到旁邊那格；他卡冊裡那幾張則是手排、根物件還有非等比縮放。
    /// 商店的比例是之前跟美術對過的，所以畫面維持我們的三層，只借他的字。
    ///
    /// 【跟 <see cref="RelicCardView"/> 是同一個形狀】外面用起來一樣：Show / Hide / SetTint。
    /// </summary>
    public class BattleCardView : MonoBehaviour
    {
        [Header("三層（由下往上）")]
        [Tooltip("卡面層（描述那塊底）")]
        public Image faceImage;

        [Tooltip("武器層。卡面上方那張圖")]
        public Image artworkImage;

        [Tooltip("卡框層。蓋在最上面")]
        public Image frameImage;

        [Header("文字（來源是 Romtyui 的 CardViewUI）")]
        [Tooltip("藏起來的 card_template。只用來組效果文字，不會顯示")]
        public CardViewUI cardView;

        [Tooltip("卡面上的效果文字")]
        public TMP_Text effectLabel;

        [Tooltip("左上角那格的費用")]
        public TMP_Text costLabel;

        /// <summary>
        /// 這張牌的效果文字（跟卡面上印的是同一串，含上色的 rich text）。
        /// 購買視窗右側的說明讀這個 —— 卡面和說明保證一致。
        /// </summary>
        public string EffectText { get; private set; } = "";

        private Color effectBase = Color.black, costBase = Color.black;
        private bool captured;

        /// <summary>換一張牌並顯示（含費用與效果文字）。</summary>
        public void Show(CardData card)
        {
            CaptureBaseColors();

            EffectText = "";
            string cost = "";

            if (card != null && cardView != null)
            {
                // 他的牌保持隱藏。Bind 只是把字組出來
                cardView.gameObject.SetActive(false);
                cardView.Bind(new CardInstance(card));
                if (cardView.descriptionText != null) EffectText = cardView.descriptionText.text;
                if (cardView.costText != null) cost = cardView.costText.text;
            }

            SetLabel(effectLabel, EffectText);
            SetLabel(costLabel, cost);
            ShowLayers(card != null ? card.visualData : null);
        }

        /// <summary>
        /// 只有卡面資料、沒有牌本身時用 —— 畫不出效果文字。
        /// <paramref name="visual"/> 是 null 的話**照樣顯示空牌**，看得出「這格是一張牌、只是還沒有美術」。
        /// </summary>
        public void Show(CardVisualData visual)
        {
            EffectText = "";
            SetLabel(effectLabel, "");
            SetLabel(costLabel, "");
            ShowLayers(visual);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>整張牌一起染色（含文字）。貨架的 hover「變暗」要作用在整張牌上。</summary>
        public void SetTint(Color tint)
        {
            CaptureBaseColors();
            if (faceImage != null) faceImage.color = tint;
            if (artworkImage != null) artworkImage.color = tint;
            if (frameImage != null) frameImage.color = tint;
            if (effectLabel != null) effectLabel.color = effectBase * tint;
            if (costLabel != null) costLabel.color = costBase * tint;
        }

        private void ShowLayers(CardVisualData visual)
        {
            Apply(faceImage, visual != null ? visual.cardFaceSprite : null);
            Apply(artworkImage, visual != null ? visual.artworkSprite : null);
            Apply(frameImage, visual != null ? visual.cardFrameSprite : null);
            gameObject.SetActive(true);
        }

        private void CaptureBaseColors()
        {
            if (captured) return;
            captured = true;
            if (effectLabel != null) effectBase = effectLabel.color;
            if (costLabel != null) costBase = costLabel.color;
        }

        private static void SetLabel(TMP_Text label, string text)
        {
            if (label == null) return;
            label.text = text ?? "";
            label.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        private static void Apply(Image img, Sprite sprite)
        {
            if (img == null) return;
            img.sprite = sprite;
            img.enabled = sprite != null;
        }
    }
}
