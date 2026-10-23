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
    /// 【遺物圖放大，邊角讓框蓋住】（2026-10-19 美術要求）彩圖本身左右留很多透明邊
    /// （畫的部分只佔寬度的一半左右），原本的大小在牌上只剩一小塊。
    /// 現在 artImage 的框是**牌寬的 1.45 倍**（正方形，preserveAspect 自己算高），
    /// 畫的部分大約填滿框的開口，邊角被框蓋住一點是刻意的。
    /// 牌的根物件掛了 RectMask2D：寬的遺物（釣竿、魚叉）超出牌的部分會被裁掉，不會跑到隔壁格。
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
                CenterOnDrawing(art);
            }

            gameObject.SetActive(true);
        }

        [Tooltip("用「畫的部分」置中，而不是整張圖置中。\n" +
                 "（2026-10-19 評圖：人魚的畫像沒有置中）彩圖本身的畫面不在畫布正中間 ——\n" +
                 "實測偏移從 -5.8%（燒毀的樂譜）到 +4.9%（紅寶石）。圖放大之後偏移跟著放大，看得出歪。")]
        public bool centerOnDrawing = true;

        private Vector2 artHome;
        private bool artHomeCaptured;

        /// <summary>
        /// 依 sprite 的 Tight 網格（貼著不透明部分）算出畫的中心，把 artImage 反向挪過去。
        /// 網格是匯入時就算好的，不需要貼圖可讀；Full Rect 的圖網格就是整張，偏移為 0、等於不處理。
        /// </summary>
        private void CenterOnDrawing(Sprite art)
        {
            RectTransform rt = artImage.rectTransform;
            if (!artHomeCaptured) { artHome = rt.anchoredPosition; artHomeCaptured = true; }

            Vector2 offset = Vector2.zero;
            if (centerOnDrawing && art != null)
            {
                Vector2[] v = art.vertices;
                Bounds b = art.bounds;
                if (v != null && v.Length > 0 && b.size.x > 0f && b.size.y > 0f)
                {
                    Vector2 mn = v[0], mx = v[0];
                    for (int i = 1; i < v.Length; i++) { mn = Vector2.Min(mn, v[i]); mx = Vector2.Max(mx, v[i]); }
                    Vector2 frac = new Vector2(((mn.x + mx.x) * 0.5f - b.center.x) / b.size.x,
                                               ((mn.y + mx.y) * 0.5f - b.center.y) / b.size.y);

                    // preserveAspect：圖在框裡實際顯示的大小
                    Vector2 box = rt.rect.size;
                    float aspect = b.size.x / b.size.y;
                    Vector2 shown = box.x / box.y > aspect ? new Vector2(box.y * aspect, box.y) : new Vector2(box.x, box.x / aspect);
                    offset = new Vector2(frac.x * shown.x, frac.y * shown.y);
                }
            }
            rt.anchoredPosition = artHome - offset;
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
