using UnityEngine;
using UnityEngine.UI;

namespace EldritchMile.UI
{
    /// <summary>
    /// 遺物在「牌」上的樣子：底圖 → 遺物圖（置中）→ 外框，三層疊出來。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【為什麼分層，不要事先合成好】（2026-10-01 美術給了 `遺物牌框`）
    /// 底圖與外框**每一件遺物都一樣**，只有中間那張圖不同。
    /// 事先合成的話，18 件遺物就要 18 張大圖，而且框一改就要全部重出。
    /// 分層只要三張：底圖、框、以及遺物自己本來就有的彩圖。
    ///
    /// 【遺物圖是置中的】美術指定的擺法。遺物的彩圖是**橫的**（約 1.66），
    /// 牌是直的（0.65），所以置中之後上下會留白 —— 那是刻意的，
    /// 不要為了填滿而把圖拉長。<see cref="artImage"/> 的 `preserveAspect` 要開著。
    ///
    /// 【誰在用】商店貨架的格子、購買確認視窗。兩邊共用這一支，
    /// 之後持有欄要改成牌的樣子也接得上。
    /// </summary>
    public class RelicCardView : MonoBehaviour
    {
        [Header("三層（由下往上）")]
        [Tooltip("牌的底圖（`商店收藏品底圖`）。整張鋪滿，不留邊")]
        public Image backImage;

        [Tooltip("遺物本身的彩圖。**置中、preserveAspect 要開**")]
        public Image artImage;

        [Tooltip("外框（`商店收藏品框僅框`）。蓋在最上面，中間是空的")]
        public Image frameImage;

        /// <summary>
        /// 換一張遺物圖並顯示。
        ///
        /// 沒有圖也**照樣顯示空牌**：底圖與框還在，看得出「這格有東西、只是還沒有美術」，
        /// 比整張消失好判讀。
        /// </summary>
        public void Show(Sprite art)
        {
            if (artImage != null)
            {
                artImage.sprite = art;
                artImage.enabled = art != null;
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 整張牌一起染色。貨架的 hover「變暗」要作用在三層上 ——
        /// 只染其中一層的話，暗的跟亮的疊在一起會很奇怪。
        /// </summary>
        public void SetTint(Color tint)
        {
            if (backImage != null) backImage.color = tint;
            if (artImage != null) artImage.color = tint;
            if (frameImage != null) frameImage.color = tint;
        }
    }
}
