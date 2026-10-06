using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EldritchMile.UI
{
    /// <summary>
    /// 文字比框長的時候，改成跑馬燈來回播；放得下就維持原樣。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【要解決什麼】（2026-09-17 美術）有些名字刻意取得很長（趣味玩法），
    /// 例如商店的商品名。縮字級會小到看不清楚，換行會撐破格子 —— 跑馬燈兩個問題都沒有。
    ///
    /// 【怎麼掛】掛在 TMP 文字上就好。執行時會自動在它外面包一層**有遮罩的容器**
    /// （大小、位置跟原本的文字框完全一樣），文字在容器裡左右移動，超出的部分被遮掉。
    /// 不用手動改 prefab 的階層。
    ///
    /// 【節奏】停在開頭 → 往左捲到看見結尾 → 停一下 → 淡出、回到開頭、淡入 → 重來。
    /// 換了文字（例如格子綁了別的商品）會自動重新判斷。
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class MarqueeText : MonoBehaviour
    {
        [Tooltip("捲動速度（畫布單位／秒）")]
        [Min(1f)] public float speed = 60f;

        [Tooltip("開頭停多久才開始捲（秒）")]
        [Min(0f)] public float startDelay = 1.2f;

        [Tooltip("捲到尾巴停多久（秒）")]
        [Min(0f)] public float endDelay = 1.2f;

        [Tooltip("回到開頭時淡出、淡入各多久（秒）。0 = 直接跳回去")]
        [Min(0f)] public float fadeSeconds = 0.2f;

        private TMP_Text text;
        private RectTransform rt;
        private RectTransform viewport;

        private TextAlignmentOptions originalAlignment;
        private TextWrappingModes originalWrap;
        private TextOverflowModes originalOverflow;
        private float baseAlpha = 1f;

        private string lastText;
        private float lastViewportWidth = -1f;
        private float overflow;
        private float timer;

        private enum Phase { Static, StartWait, Scroll, EndWait, FadeOut, FadeIn }
        private Phase phase = Phase.Static;

        private void Awake()
        {
            text = GetComponent<TMP_Text>();
            rt = (RectTransform)transform;

            originalAlignment = text.alignment;
            originalWrap = text.textWrappingMode;
            originalOverflow = text.overflowMode;
            baseAlpha = text.alpha;

            EnsureViewport();
        }

        private void OnDisable()
        {
            // 下次顯示時從頭開始，不要停在捲到一半的位置
            lastText = null;
            if (rt != null) SetX(0f);
            if (text != null) text.alpha = baseAlpha;
        }

        /// <summary>
        /// 在文字外面包一層有遮罩的容器，大小與位置照抄原本的文字框。
        /// 已經包過（例如被停用再啟用）就沿用。
        /// </summary>
        private void EnsureViewport()
        {
            RectTransform parent = rt.parent as RectTransform;
            if (parent != null && parent.name == name + "_Marquee" && parent.GetComponent<RectMask2D>() != null)
            {
                viewport = parent;
                return;
            }

            GameObject go = new GameObject(name + "_Marquee", typeof(RectTransform), typeof(RectMask2D));
            go.layer = gameObject.layer;
            viewport = (RectTransform)go.transform;
            viewport.SetParent(rt.parent, false);
            viewport.SetSiblingIndex(rt.GetSiblingIndex());

            viewport.anchorMin = rt.anchorMin;
            viewport.anchorMax = rt.anchorMax;
            viewport.pivot = rt.pivot;
            viewport.anchoredPosition = rt.anchoredPosition;
            viewport.sizeDelta = rt.sizeDelta;
            viewport.localRotation = rt.localRotation;
            viewport.localScale = rt.localScale;

            rt.SetParent(viewport, false);
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;

            // 文字錨在容器左邊、高度撐滿，寬度由 Measure 決定
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(viewport.rect.width, 0f);
            rt.anchoredPosition = Vector2.zero;
        }

        private void Update()
        {
            if (viewport == null) return;

            float vw = viewport.rect.width;
            if (text.text != lastText || !Mathf.Approximately(vw, lastViewportWidth)) Measure(vw);

            if (phase == Phase.Static) return;

            float dt = Time.unscaledDeltaTime;
            timer += dt;

            switch (phase)
            {
                case Phase.StartWait:
                    if (timer >= startDelay) { phase = Phase.Scroll; timer = 0f; }
                    break;

                case Phase.Scroll:
                {
                    float x = rt.anchoredPosition.x - speed * dt;
                    if (x <= -overflow) { x = -overflow; phase = Phase.EndWait; timer = 0f; }
                    SetX(x);
                    break;
                }

                case Phase.EndWait:
                    if (timer >= endDelay) { phase = fadeSeconds > 0f ? Phase.FadeOut : Phase.StartWait; timer = 0f; if (fadeSeconds <= 0f) SetX(0f); }
                    break;

                case Phase.FadeOut:
                    text.alpha = Mathf.Lerp(baseAlpha, 0f, Mathf.Clamp01(timer / fadeSeconds));
                    if (timer >= fadeSeconds) { SetX(0f); phase = Phase.FadeIn; timer = 0f; }
                    break;

                case Phase.FadeIn:
                    text.alpha = Mathf.Lerp(0f, baseAlpha, Mathf.Clamp01(timer / fadeSeconds));
                    if (timer >= fadeSeconds) { text.alpha = baseAlpha; phase = Phase.StartWait; timer = 0f; }
                    break;
            }
        }

        /// <summary>量文字實際要多寬，決定要不要捲。</summary>
        private void Measure(float vw)
        {
            lastText = text.text;
            lastViewportWidth = vw;
            timer = 0f;
            text.alpha = baseAlpha;

            float w = string.IsNullOrEmpty(text.text)
                ? 0f
                : text.GetPreferredValues(text.text, float.PositiveInfinity, viewport.rect.height).x;

            if (w <= vw + 0.5f)
            {
                // 放得下：完全恢復原本的樣子
                text.alignment = originalAlignment;
                text.textWrappingMode = originalWrap;
                text.overflowMode = originalOverflow;
                rt.sizeDelta = new Vector2(vw, 0f);
                SetX(0f);
                phase = Phase.Static;
                return;
            }

            // 放不下：單行、靠左（保留原本的垂直對齊），開始捲
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.alignment = (TextAlignmentOptions)(((int)originalAlignment & ~0xFF) | (int)HorizontalAlignmentOptions.Left);

            overflow = w - vw;
            rt.sizeDelta = new Vector2(w, 0f);
            SetX(0f);
            phase = Phase.StartWait;
        }

        private void SetX(float x)
        {
            Vector2 p = rt.anchoredPosition;
            p.x = x;
            rt.anchoredPosition = p;
        }
    }
}
