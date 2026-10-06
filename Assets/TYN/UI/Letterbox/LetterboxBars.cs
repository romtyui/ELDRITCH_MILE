using UnityEngine;
using UnityEngine.UI;

namespace EldritchMile.UI
{
    /// <summary>
    /// 螢幕不是 16:9 時，把 16:9 以外的地方蓋成黑邊。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【要解決什麼】（2026-09-15 build 回報「戰鬥畫面的外框是螢光藍綠」）
    /// 在 1920×1200（16:10）重現：戰鬥的背景只蓋 16:9 那一塊，上下多出來的地方露出底下的淺灰色，
    /// 再被戰鬥的 `Global Volume (1)`（藍綠色濾鏡 ＋ Bloom 10）染成螢光藍綠。
    /// 關掉那個 Volume 之後邊條變成淺灰 —— 所以顏色來自後製，露出來的原因是長寬比。
    /// 編輯器的 Game 視窗是 16:9 所以一直看不到。
    ///
    /// 【為什麼是一塊排序 -100 的 Overlay 畫布】
    ///   · Overlay 畫布一律畫在相機（含後製）**之後** —— 蓋得住被染色的那一塊
    ///   · 排序比所有遊戲 UI 都低（戰鬥最低的 SceneCanvas 是 0）—— 手牌、血條伸進邊條也不會被蓋掉
    ///   · 不擋滑鼠
    ///
    /// 【哪些環節顯示】由同物件上的 `UIPanel.visibleInStages` 決定（目前只有 Battle）。
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class LetterboxBars : MonoBehaviour
    {
        [Tooltip("內容的長寬比。超出這個比例的部分蓋黑")]
        public float targetAspect = 16f / 9f;

        [Tooltip("邊條顏色")]
        public Color color = Color.black;

        [Tooltip("畫布排序。**要比所有遊戲 UI 低**，才不會蓋到伸進邊條的 UI")]
        public int sortingOrder = -100;

        private RectTransform top, bottom, left, right;
        private int lastW = -1, lastH = -1;

        private void Awake()
        {
            Canvas c = GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = sortingOrder;

            top = Bar("Top");
            bottom = Bar("Bottom");
            left = Bar("Left");
            right = Bar("Right");
        }

        private void OnEnable()
        {
            lastW = -1;   // 重新顯示時一定重排 —— 隱藏期間解析度可能變了
            Layout();
        }

        private void Update()
        {
            if (Screen.width != lastW || Screen.height != lastH) Layout();
        }

        private RectTransform Bar(string barName)
        {
            Transform existing = transform.Find(barName);
            GameObject go = existing != null
                ? existing.gameObject
                : new GameObject(barName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

            go.transform.SetParent(transform, false);
            Image img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;

            RectTransform rt = (RectTransform)go.transform;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        private void Layout()
        {
            if (top == null) return;

            lastW = Screen.width;
            lastH = Screen.height;
            if (lastW <= 0 || lastH <= 0) return;

            float screenAspect = (float)lastW / lastH;

            if (screenAspect > targetAspect + 0.001f)
            {
                // 比 16:9 寬（例如 21:9）→ 左右黑邊
                float frac = (1f - lastH * targetAspect / lastW) * 0.5f;
                Set(left, 0f, 0f, frac, 1f);
                Set(right, 1f - frac, 0f, 1f, 1f);
                Set(top, 0f, 1f, 1f, 1f);
                Set(bottom, 0f, 0f, 1f, 0f);
            }
            else if (screenAspect < targetAspect - 0.001f)
            {
                // 比 16:9 高（例如 16:10 筆電）→ 上下黑邊
                float frac = (1f - lastW / targetAspect / lastH) * 0.5f;
                Set(bottom, 0f, 0f, 1f, frac);
                Set(top, 0f, 1f - frac, 1f, 1f);
                Set(left, 0f, 0f, 0f, 1f);
                Set(right, 1f, 0f, 1f, 1f);
            }
            else
            {
                Set(top, 0f, 1f, 1f, 1f);
                Set(bottom, 0f, 0f, 1f, 0f);
                Set(left, 0f, 0f, 0f, 1f);
                Set(right, 1f, 0f, 1f, 1f);
            }
        }

        private static void Set(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
