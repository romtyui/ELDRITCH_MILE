using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 把一組場景美術**對齊到相機**，讓它剛好蓋滿畫面。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【為什麼需要】美術原稿的中心不一定在相機中心。
    /// 祭壇那張的實際範圍是 y −6.12 ~ 5.08，而相機（正交、size 5、位在 y = 1）
    /// 看到的是 y −4 ~ 6 —— **上緣差 0.92 個單位**，1080p 換算約 78px 的天空底色。
    /// 下面反而多出 2.12，也就是圖整個偏低。
    ///
    /// 這不是祭壇特有的：房間美術是同一組數字，只是那邊靠 prefab 根
    /// 整個往上擺了 1 才剛好蓋住。**一張新的圖比例只要不一樣，那個手調值就失效了。**
    ///
    /// 【為什麼用量的而不是填一個位移】填死的話，換相機、換解析度、
    /// 換一張比例不同的背景都要重調，而且**壞掉的時候不會有任何訊息** ——
    /// 只會有一條天空色的邊，看起來像美術沒畫滿。
    ///
    /// 【什麼時候跑】`OnEnable` 跑一次就好。Stage 是 Instantiate 出來的，
    /// 所以每次進場都會重算；相機在遊戲中不會換。
    /// </summary>
    [DefaultExecutionOrder(100)]   // 讓美術自己的初始化先跑完再量
    public class BackdropFit : MonoBehaviour
    {
        [Tooltip("拿哪一張當「背景本體」來量。\n\n" +
                 "⚠️ **建議明確指定。** 留空會自動挑面積最大的那個 SpriteRenderer，\n" +
                 "但前景小物件常常比背景還寬（祭壇那張的玻璃瓶就是），\n" +
                 "猜錯的話會照著一個小物件去對齊，畫面反而更歪。\n" +
                 "自動挑的時候會在 Console 說一聲。")]
        public SpriteRenderer background;

        [Tooltip("對齊之後如果還是蓋不滿，就整組放大到蓋滿。\n\n" +
                 "⚠️ 放大會讓美術**失去原本的取景**（邊緣被裁掉）。\n" +
                 "所以預設只有在真的蓋不滿時才動，而且會在 Console 說一聲 ——\n" +
                 "看到那行就表示這張圖對這個畫面比例來說太小，該請美術補。")]
        public bool upscaleIfTooSmall = true;

        [Tooltip("放大時多留一點餘裕，避免邊緣剛好切齊時因為浮點誤差露出一條縫")]
        [Range(1f, 1.2f)] public float upscalePadding = 1.01f;

        [Tooltip("把過程印出來。對位不順的時候打開")]
        public bool verbose = false;

        private void OnEnable() => Fit();

        /// <summary>手動重算。換了解析度或相機之後可以叫一次。</summary>
        public void Fit()
        {
            Camera cam = Camera.main;
            if (cam == null || !cam.orthographic) return;

            SpriteRenderer bg = Resolve();
            if (bg == null) return;

            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            Vector3 eye = cam.transform.position;

            // ── 1. 先置中 ──
            //    量的是**世界座標的 bounds**，所以父層的縮放與子物件的位移都已經算進去了
            Bounds b = bg.bounds;
            Vector3 shift = new Vector3(eye.x - b.center.x, eye.y - b.center.y, 0f);
            transform.position += shift;

            if (!upscaleIfTooSmall)
            {
                if (verbose) Debug.Log($"[背景對齊] {name} 位移 {shift}", this);
                return;
            }

            // ── 2. 置中之後還是蓋不滿才放大 ──
            //    ⚠️ 一定要重新量：上面那一步已經動過位置了
            b = bg.bounds;
            float needX = (halfW * 2f) / Mathf.Max(0.0001f, b.size.x);
            float needY = (halfH * 2f) / Mathf.Max(0.0001f, b.size.y);
            float need = Mathf.Max(needX, needY);

            if (need <= 1f)
            {
                if (verbose) Debug.Log($"[背景對齊] {name} 位移 {shift}，尺寸夠，不放大", this);
                return;
            }

            float k = need * upscalePadding;
            transform.localScale = new Vector3(
                transform.localScale.x * k, transform.localScale.y * k, transform.localScale.z);

            // 放大是以自己的軸心為準，圖心會跟著跑掉 —— 再置中一次
            b = bg.bounds;
            transform.position += new Vector3(eye.x - b.center.x, eye.y - b.center.y, 0f);

            Debug.Log(
                $"[背景對齊] {name} 蓋不滿相機，整組放大 {k:0.000} 倍。\n" +
                "⚠️ 放大等於裁掉邊緣、失去美術原本的取景 —— " +
                "這張圖對現在的畫面比例來說太小，最好請美術補一張。", this);
        }

        /// <summary>
        /// 要拿哪一張當背景。**指定了就用指定的**，這是唯一可靠的答案。
        ///
        /// 沒指定時挑面積最大的那個，但那只是猜 —— 祭壇那組的「玻璃瓶」
        /// 比背景還寬（21.79 vs 19.91），猜出來的就是它。所以要吵一聲。
        /// </summary>
        private SpriteRenderer Resolve()
        {
            if (background != null) return background;

            SpriteRenderer best = null;
            float bestArea = 0f;

            SpriteRenderer[] all = GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null || all[i].sprite == null) continue;

                float area = all[i].bounds.size.x * all[i].bounds.size.y;
                if (area <= bestArea) continue;

                bestArea = area;
                best = all[i];
            }

            if (best != null)
            {
                Debug.LogWarning(
                    $"[背景對齊] {name} 沒有指定 Background，自動挑了面積最大的「{best.name}」。\n" +
                    "⚠️ 前景小物件常常比背景還寬，猜錯就會照著它對齊 —— " +
                    "把真正的背景拉進 Background 那一格比較保險。", this);
            }

            return best;
        }
    }
}
