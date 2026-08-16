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
        [Tooltip("講完幾秒後自動消失。**0 = 常駐不收**")]
        [Min(0f)] public float autoHideSeconds = 4f;

        [Header("動效")]
        [Tooltip("冒出來的時間")]
        [Min(0.01f)] public float popInSeconds = 0.22f;

        [Tooltip("縮回去的時間。比冒出來短 —— 消失拖太久會擋住下一句")]
        [Min(0.01f)] public float popOutSeconds = 0.12f;

        [Tooltip("冒出來時衝過頭多少（0 = 沒有彈性，0.12 ≈ 衝過 12% 再回來）。\n" +
                 "這是「氣泡感」的來源 —— 純線性放大看起來像面板展開，不像氣泡")]
        [Range(0f, 0.4f)] public float popOvershoot = 0.12f;

        [Tooltip("起始縮放。太小會像從一個點長出來，0.8 左右最自然")]
        [Range(0.1f, 1f)] public float popFromScale = 0.8f;

        /// <summary>
        /// 氣泡的四個狀態。
        ///
        /// 【為什麼要有 Out 這個狀態】組員要的是「換句話時**先消失再冒出來**」，
        /// 不是直接把文字換掉。直接換字的話玩家常常沒發現內容變了 ——
        /// 一顆一直掛在那裡的氣泡，字換了跟沒換看起來一樣。
        /// 縮回去再彈出來，眼睛才會被拉回去。
        /// </summary>
        private enum Phase { Hidden, In, Hold, Out }

        private CanvasGroup group;
        private Canvas ownCanvas;

        private Phase phase = Phase.Hidden;
        private float phaseT;        // In / Out 的進度 0→1
        private float holdUntil;

        // 「縮回去之後要接著講的下一句」。Out 播完才會套用
        private bool hasPending;
        private string pendingMessage;
        private string pendingSpeaker;
        private Transform pendingAnchor;

        public bool IsShowing => phase != Phase.Hidden;

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

            phase = Phase.Hidden;
            if (bubbleRoot != null) bubbleRoot.localScale = Vector3.one * popFromScale;
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

            pendingMessage = message;
            pendingSpeaker = speaker;
            pendingAnchor = followTarget;
            hasPending = true;

            // 已經有一顆在畫面上 → 先縮回去，Out 播完才換內容再彈出來。
            // 已經在縮了就不要打斷它，讓它把這一段播完自然接上新的內容。
            if (phase == Phase.In || phase == Phase.Hold)
            {
                BeginOut();
                return;
            }

            if (phase == Phase.Out) return;   // 正在縮，pending 已經更新，等它播完

            ApplyPending();
            BeginIn();
        }

        /// <summary>收起氣泡。會播縮回去的動作，不是瞬間消失。</summary>
        public void Hide()
        {
            hasPending = false;

            if (phase == Phase.Hidden || phase == Phase.Out) return;

            BeginOut();
        }

        /// <summary>不播動作、直接消失。轉場（Stage 離開）時用，免得殘影留到下一個畫面。</summary>
        public void HideImmediate()
        {
            hasPending = false;
            phase = Phase.Hidden;

            if (group != null) group.alpha = 0f;
            if (bubbleRoot != null) bubbleRoot.localScale = Vector3.one * popFromScale;
        }

        // ==========================================
        private void ApplyPending()
        {
            if (pendingAnchor != null) anchor = pendingAnchor;

            if (text != null) text.text = pendingMessage;

            if (speakerText != null && pendingSpeaker != null)
            {
                speakerText.text = pendingSpeaker;
                speakerText.gameObject.SetActive(!string.IsNullOrEmpty(pendingSpeaker));
            }

            hasPending = false;

            // 立刻定位一次，不要等 LateUpdate ——
            // 否則彈出來的第一幀會出現在上一個角色頭上
            UpdatePosition();
        }

        private void BeginIn()
        {
            phase = Phase.In;
            phaseT = 0f;
        }

        private void BeginOut()
        {
            phase = Phase.Out;
            phaseT = 0f;
        }

        // ==========================================
        private void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;

            switch (phase)
            {
                case Phase.In:
                    phaseT += dt / popInSeconds;

                    // 透明度收得比縮放快 —— 讓它「已經在那裡了，只是還在彈」，
                    // 而不是半透明地慢慢浮現
                    SetVisual(Mathf.Clamp01(phaseT * 2f), PopScale(Mathf.Clamp01(phaseT)));

                    if (phaseT >= 1f)
                    {
                        phase = Phase.Hold;
                        holdUntil = autoHideSeconds > 0f ? Time.unscaledTime + autoHideSeconds : 0f;
                    }
                    break;

                case Phase.Hold:
                    SetVisual(1f, 1f);
                    if (holdUntil > 0f && Time.unscaledTime >= holdUntil) Hide();
                    break;

                case Phase.Out:
                    phaseT += dt / popOutSeconds;
                {
                    float t = Mathf.Clamp01(phaseT);
                    SetVisual(1f - t, Mathf.Lerp(1f, popFromScale, t));
                }

                    if (phaseT >= 1f)
                    {
                        if (hasPending)
                        {
                            // 縮完了、而且有下一句在等 → 換內容再彈一次
                            ApplyPending();
                            BeginIn();
                        }
                        else
                        {
                            HideImmediate();
                        }
                    }
                    break;
            }

            // 縮回去的過程也要繼續跟著錨點，否則氣泡會邊消失邊留在原地
            if (phase != Phase.Hidden) UpdatePosition();
        }

        private void SetVisual(float alpha, float scale)
        {
            if (group != null) group.alpha = Mathf.Clamp01(alpha);
            if (bubbleRoot != null) bubbleRoot.localScale = Vector3.one * scale;
        }

        /// <summary>
        /// 冒出來的縮放曲線：從 <see cref="popFromScale"/> 衝過 1、再回到 1。
        ///
        /// 這是 ease-out-back。**衝過頭那一下就是「氣泡感」** ——
        /// 線性放大看起來像面板展開，不像有東西彈出來。
        /// </summary>
        private float PopScale(float t)
        {
            float c1 = popOvershoot * 10f * 0.17f;   // 0.12 → ≈0.2 的回彈量
            float c3 = c1 + 1f;

            float u = t - 1f;
            float eased = 1f + c3 * u * u * u + c1 * u * u;

            return Mathf.LerpUnclamped(popFromScale, 1f, eased);
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
