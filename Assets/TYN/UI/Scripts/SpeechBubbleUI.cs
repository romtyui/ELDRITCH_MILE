using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EldritchMile.UI
{
    /// <summary>
    /// 常駐對話氣泡（業界叫 **bark**）。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【bark 與對話框是兩種東西，不要合併】
    ///
    ///   bark（這個）    進場自動冒出來、跟著角色、沒有選項、漏看也沒差。
    ///                   商店店主的「歡迎光臨」、路人的碎念。
    ///   對話框（既有）  排隊播放、要按才過、可以帶選項與判定。劇情走這個。
    ///
    /// 合併的下場是隨機寒暄會插進劇情中間，或劇情被寒暄蓋掉。
    /// Pixel Crushers 的 Dialogue System、Yarn Spinner 都把兩者分開，理由一樣。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【空間轉換：這個元件真正在解的問題】
    ///
    /// 角色可能是世界空間的 Sprite，也可能是畫在背景圖上的一團像素；
    /// 氣泡則永遠是 UI。兩邊的座標系不同，所以要：
    ///
    ///     錨點的世界座標 → 螢幕座標 → 氣泡父物件的區域座標
    ///
    /// 直接把氣泡設成角色的子物件是行不通的：角色若是世界物件，UI 掛不上去；
    /// 角色若是背景圖的一部分，氣泡會跟著背景一起被 CanvasScaler 縮放而變形。
    ///
    /// 【錨點放哪】在角色頭頂／嘴巴上方放一個空物件就好。
    /// 是 RectTransform（畫在背景上的角色）或一般 Transform（世界角色）都可以，
    /// 這裡會自己判斷該用哪個相機換算。
    /// </summary>
    public class SpeechBubbleUI : MonoBehaviour
    {
        /// <summary>
        /// 場上通常只有一個氣泡在用（同時只有一個 Stage 活著）。
        /// Stage 不必在 Inspector 拉引用也找得到。
        /// </summary>
        public static SpeechBubbleUI Instance { get; private set; }

        [Header("元件")]
        [Tooltip("整顆氣泡的根。淡入淡出與定位都作用在這個上面")]
        public RectTransform bubbleRoot;

        [Tooltip("氣泡內文")]
        public TextMeshProUGUI text;

        [Tooltip("說話者的名字。可留空 —— 氣泡指著誰通常已經很清楚了")]
        public TextMeshProUGUI speakerText;

        [Header("定位")]
        [Tooltip("要跟著誰。可以在執行時由 Show() 指定")]
        public Transform anchor;

        [Tooltip("相對錨點的偏移（UI 像素）。往上是正的")]
        public Vector2 offset = new Vector2(0f, 40f);

        [Tooltip("把氣泡夾在這個範圍內，不讓它跑出畫面。留空則不夾")]
        public RectTransform bounds;

        [Tooltip("邊界內縮多少像素")]
        public float boundsPadding = 12f;

        [Header("行為")]
        [Tooltip("顯示幾秒後自動收起。**0 = 常駐不收** —— 商店店主用 0")]
        [Min(0f)] public float autoHideSeconds = 0f;

        [Tooltip("淡入淡出秒數")]
        [Min(0f)] public float fadeSeconds = 0.15f;

        private CanvasGroup group;
        private Canvas ownCanvas;
        private float hideAt;
        private float targetAlpha;

        public bool IsShowing { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;

            if (bubbleRoot == null) bubbleRoot = transform as RectTransform;

            group = bubbleRoot != null ? bubbleRoot.GetComponent<CanvasGroup>() : null;
            if (group == null && bubbleRoot != null) group = bubbleRoot.gameObject.AddComponent<CanvasGroup>();

            ownCanvas = GetComponentInParent<Canvas>();

            // 一開始就藏起來。**用 alpha 而不是 SetActive** ——
            // SetActive(false) 的話 LateUpdate 不會跑，之後 Show() 的第一幀會出現在舊位置閃一下
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
            }

            targetAlpha = 0f;
            IsShowing = false;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ==========================================
        // 顯示
        // ==========================================
        public void Show(string message) => Show(message, anchor, null);

        public void Show(string message, Transform followTarget) => Show(message, followTarget, null);

        /// <summary>
        /// 顯示一句話。<paramref name="speaker"/> 留 null 就沿用原本的名字。
        /// </summary>
        public void Show(string message, Transform followTarget, string speaker)
        {
            if (string.IsNullOrEmpty(message)) { Hide(); return; }

            if (followTarget != null) anchor = followTarget;

            if (text != null) text.text = message;

            if (speakerText != null && speaker != null)
            {
                speakerText.text = speaker;
                speakerText.gameObject.SetActive(!string.IsNullOrEmpty(speaker));
            }

            targetAlpha = 1f;
            IsShowing = true;

            hideAt = autoHideSeconds > 0f ? Time.unscaledTime + autoHideSeconds : 0f;

            // 立刻定位一次，不要等 LateUpdate —— 否則淡入的第一幀在上一個角色頭上
            UpdatePosition();
        }

        public void Hide()
        {
            targetAlpha = 0f;
            IsShowing = false;
            hideAt = 0f;
        }

        // ==========================================
        private void LateUpdate()
        {
            if (hideAt > 0f && Time.unscaledTime >= hideAt) Hide();

            if (group != null && !Mathf.Approximately(group.alpha, targetAlpha))
            {
                group.alpha = fadeSeconds <= 0f
                    ? targetAlpha
                    : Mathf.MoveTowards(group.alpha, targetAlpha, Time.unscaledDeltaTime / fadeSeconds);
            }

            // 淡出中也要繼續跟著，否則氣泡會邊消失邊留在原地
            if (IsShowing || (group != null && group.alpha > 0f)) UpdatePosition();
        }

        private void UpdatePosition()
        {
            if (bubbleRoot == null || anchor == null) return;

            RectTransform parent = bubbleRoot.parent as RectTransform;
            if (parent == null) return;

            // 錨點在世界裡還是在 UI 上，決定要用哪個相機把它換成螢幕座標。
            // 判斷依據是「它屬不屬於某個 Canvas」，不是它的型別 ——
            // RectTransform 也可能掛在 world space canvas 上。
            Camera anchorCam = ResolveCameraFor(anchor);
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(anchorCam, anchor.position);

            // 換算回氣泡父物件的區域座標。這裡用的是**氣泡自己的**相機，不是錨點的
            Camera uiCam = CameraOf(ownCanvas);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, uiCam, out Vector2 local))
            {
                return;
            }

            bubbleRoot.anchoredPosition = local + offset;

            ClampInsideBounds();
        }

        /// <summary>
        /// 不讓氣泡跑出畫面。角色站在螢幕邊緣時，氣泡會有一半在外面。
        /// </summary>
        private void ClampInsideBounds()
        {
            if (bounds == null) return;

            RectTransform parent = bubbleRoot.parent as RectTransform;
            if (parent == null) return;

            Rect limit = bounds.rect;
            Vector2 size = bubbleRoot.rect.size;
            Vector2 pivot = bubbleRoot.pivot;

            // 氣泡在自己父物件座標下的左下與右上
            Vector2 pos = bubbleRoot.anchoredPosition;
            float left = pos.x - size.x * pivot.x;
            float bottom = pos.y - size.y * pivot.y;

            float minX = limit.xMin + boundsPadding;
            float maxX = limit.xMax - boundsPadding - size.x;
            float minY = limit.yMin + boundsPadding;
            float maxY = limit.yMax - boundsPadding - size.y;

            // 氣泡比邊界還大時 Clamp 的上下限會反過來，那時候就別動它
            if (maxX > minX) left = Mathf.Clamp(left, minX, maxX);
            if (maxY > minY) bottom = Mathf.Clamp(bottom, minY, maxY);

            bubbleRoot.anchoredPosition = new Vector2(
                left + size.x * pivot.x,
                bottom + size.y * pivot.y);
        }

        private static Camera ResolveCameraFor(Transform t)
        {
            Canvas canvas = t.GetComponentInParent<Canvas>();

            // 不屬於任何 Canvas ＝ 世界裡的東西，用主相機換算
            return canvas != null ? CameraOf(canvas) : Camera.main;
        }

        /// <summary>
        /// Overlay 的 Canvas 要傳 null 而不是 Camera.main ——
        /// 傳了相機的話換算會整個偏掉，而且不會有任何錯誤訊息。
        /// </summary>
        private static Camera CameraOf(Canvas canvas)
        {
            if (canvas == null) return null;

            Canvas root = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
            return root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
        }
    }
}
