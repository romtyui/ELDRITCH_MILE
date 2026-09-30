using UnityEngine;
using UnityEngine.UI;

namespace EldritchMile.UI
{
    /// <summary>
    /// 武器在「牌」上的樣子：卡面 → 武器圖 → 卡框，三層疊出來。
    /// 資料來自 Romtyui 的 <see cref="CardVisualData"/>。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【跟 <see cref="RelicCardView"/> 是同一個形狀】商店裡兩種商品都是「一張牌」：
    /// 遺物是底圖＋圖＋外框，武器是卡面＋武器＋卡框。分開兩支是因為
    /// 資料來源不同（遺物讀 ItemData.ShelfIcon，武器讀 CardVisualData），
    /// 但外面用起來一樣：Show / Hide / SetTint。
    ///
    /// 【三層的相對比例抄自 `card_template`，但不壓扁】
    /// 戰鬥裡那個 prefab 把 768x1166 的卡框顯示成接近正方形；
    /// 商店要跟遺物牌並排，所以用卡框原本的長寬比。
    /// 換句話說：**這裡的版面不是戰鬥的版面**，改戰鬥那邊不會影響這裡，反之亦然。
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

        /// <summary>
        /// 換一張牌並顯示。
        ///
        /// <paramref name="visual"/> 是 null 的話**照樣顯示空牌** ——
        /// 三層都會是空的，但物件還在，看得出「這格是一張牌、只是還沒有美術」。
        /// </summary>
        public void Show(CardVisualData visual)
        {
            Apply(faceImage, visual != null ? visual.cardFaceSprite : null);
            Apply(artworkImage, visual != null ? visual.artworkSprite : null);
            Apply(frameImage, visual != null ? visual.cardFrameSprite : null);

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>整張牌一起染色。貨架的 hover「變暗」要作用在三層上。</summary>
        public void SetTint(Color tint)
        {
            if (faceImage != null) faceImage.color = tint;
            if (artworkImage != null) artworkImage.color = tint;
            if (frameImage != null) frameImage.color = tint;
        }

        private static void Apply(Image img, Sprite sprite)
        {
            if (img == null) return;
            img.sprite = sprite;
            img.enabled = sprite != null;
        }
    }
}
