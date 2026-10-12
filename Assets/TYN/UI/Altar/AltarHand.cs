using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EldritchMile.UI
{
    /// <summary>
    /// 祭壇的一隻手。掌與拇指是分開的兩張圖，所以可以做出關節。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【「優美」從哪裡來】不是靠更久的時間，是靠三件事：
    ///   1. **掌先動，拇指慢一拍** —— 真實的肢體不會整塊一起到位。
    ///      `thumbDelay` 就是那一拍。
    ///   2. **拇指會過頭再回來**（EaseOutBack）。掌是乾淨的減速（EaseOutCubic）。
    ///      兩條不同的曲線疊在一起，看起來才像有重量。
    ///   3. **到位之後還在微微呼吸** —— 完全靜止的手會變成貼紙。
    ///
    /// 【為什麼不用 Animator】這些值美術一定會反覆調。寫在 Inspector 上
    /// 改完馬上看得到，不用進動畫視窗；而且兩隻手共用同一支，左右各自配參數。
    ///
    /// 【點擊範圍是獨立的一塊，不是整張圖】手的圖是一個很大的旋轉矩形，
    /// 裡面大半是透明的。拿整張去收點擊的話，點旁邊的空白也會選到它。
    /// 用 alphaHitTestMinimumThreshold 可以精準，但要把貼圖設成 Read/Write
    /// （這張 1030x2048 會多吃 8MB 常駐記憶體）。所以改放一塊看不見的
    /// <see cref="hitArea"/> 蓋在掌心上，便宜又夠用。
    /// </summary>
    public class AltarHand : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("元件")]
        [Tooltip("手掌那張圖。pivot 要設在**手腕**上，旋轉才會像從手腕轉")]
        public RectTransform palm;

        [Tooltip("拇指那張圖。pivot 設在拇指根部")]
        public RectTransform thumb;

        [Tooltip("看不見但收點擊的那一塊，蓋在掌心。可留空 = 不能點")]
        public Graphic hitArea;

        [Tooltip("之後美術要加的神牌特效掛在這裡。現在是空的")]
        public RectTransform fxAnchor;

        [Header("伸出來之前在哪（相對於擺好的位置）")]
        [Tooltip("畫面外的偏移。左手往左上、右手往右上，照美術稿是從畫面外伸進來")]
        public Vector2 offscreenOffset = new Vector2(-760f, 320f);

        [Tooltip("畫面外時多轉幾度。伸進來的過程會轉回 0")]
        public float offscreenAngle = 18f;

        [Header("伸出來")]
        [Min(0.05f)] public float reachSeconds = 1.1f;

        [Tooltip("拇指比手掌慢幾秒才跟上。**這一拍是關節感的來源**，0 的話整隻手會像一塊板子")]
        [Min(0f)] public float thumbDelay = 0.18f;

        [Tooltip("拇指張開到最大時比定位多轉幾度（之後會收回來）。0 = 不過頭")]
        public float thumbOvershoot = 9f;

        [Header("待機呼吸")]
        [Tooltip("上下飄幾個單位。0 = 完全不動")]
        [Min(0f)] public float breathAmplitude = 7f;

        [Tooltip("一次呼吸幾秒")]
        [Min(0.1f)] public float breathSeconds = 3.4f;

        [Header("滑鼠移上去（要先可選才有反應）")]
        [Tooltip("往畫面中央再伸出去多少")]
        public Vector2 hoverPush = new Vector2(46f, -18f);

        [Min(0.01f)] public float hoverSeconds = 0.22f;

        [Header("授予神牌")]
        [Tooltip("被選中時往中央推多少")]
        public Vector2 grantPush = new Vector2(150f, -60f);

        [Min(0.05f)] public float grantSeconds = 0.9f;

        /// <summary>玩家點了這隻手。只有 <see cref="Pickable"/> 時才會發。</summary>
        public event Action<AltarHand> OnPicked;

        /// <summary>這隻手現在能不能點。提示還沒出現、或玩家選了「否」之前都是 false。</summary>
        public bool Pickable { get; private set; }

        private RectTransform self;
        private Vector2 restPos;
        private float restAngle;
        private float thumbRestAngle;
        private bool hovered;
        private float breathPhase;
        private Coroutine motion;

        // 伸出來的進度（0 = 在畫面外，1 = 定位）。呼吸與 hover 都疊在這之上
        private float reach;
        private Vector2 hoverOffset;
        private Vector2 grantOffset;

        private void Awake()
        {
            self = (RectTransform)transform;
            restPos = self.anchoredPosition;
            restAngle = self.localEulerAngles.z;
            if (thumb != null) thumbRestAngle = thumb.localEulerAngles.z;
            breathPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

            PrepareHidden();
        }

        /// <summary>擺回畫面外、不可點。進場前叫一次。</summary>
        public void PrepareHidden()
        {
            StopMotion();
            reach = 0f;
            hoverOffset = Vector2.zero;
            grantOffset = Vector2.zero;
            Pickable = false;
            hovered = false;
            SetHitEnabled(false);
            if (thumb != null) thumb.localEulerAngles = new Vector3(0f, 0f, thumbRestAngle - thumbOvershoot);
            Apply();
        }

        /// <summary>從畫面外伸進來。<paramref name="delay"/> 讓兩隻手錯開，不要同時到。</summary>
        public Coroutine ReachIn(float delay)
        {
            StopMotion();
            motion = StartCoroutine(ReachRoutine(delay));
            return motion;
        }

        /// <summary>縮回畫面外。玩家選「否」或拿完牌之後用。</summary>
        public Coroutine Withdraw()
        {
            StopMotion();
            SetHitEnabled(false);
            Pickable = false;
            motion = StartCoroutine(WithdrawRoutine());
            return motion;
        }

        /// <summary>開放／關閉點選。提示出現之前兩隻手都只是演出，點不得。</summary>
        public void SetPickable(bool on)
        {
            Pickable = on;
            SetHitEnabled(on);
            if (!on && hovered) { hovered = false; }
        }

        /// <summary>被選中：把牌遞出來的那一下。</summary>
        public Coroutine PlayGrant()
        {
            StopMotion();
            SetHitEnabled(false);
            Pickable = false;
            motion = StartCoroutine(GrantRoutine());
            return motion;
        }

        // ==========================================
        private IEnumerator ReachRoutine(float delay)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

            float t = 0f;
            while (t < reachSeconds)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / reachSeconds);

                reach = EaseOutCubic(u);

                // 拇指晚一拍，而且用會過頭的曲線 —— 關節感就在這裡
                float tt = Mathf.Clamp01((t - thumbDelay) / Mathf.Max(0.01f, reachSeconds - thumbDelay));
                if (thumb != null)
                    thumb.localEulerAngles = new Vector3(0f, 0f,
                        Mathf.LerpUnclamped(thumbRestAngle - thumbOvershoot, thumbRestAngle, EaseOutBack(tt)));

                Apply();
                yield return null;
            }

            reach = 1f;
            if (thumb != null) thumb.localEulerAngles = new Vector3(0f, 0f, thumbRestAngle);
            Apply();
            motion = null;
        }

        private IEnumerator WithdrawRoutine()
        {
            float from = reach;
            float t = 0f;
            float dur = reachSeconds * 0.8f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                reach = Mathf.Lerp(from, 0f, EaseInCubic(Mathf.Clamp01(t / dur)));
                Apply();
                yield return null;
            }
            reach = 0f;
            Apply();
            motion = null;
        }

        private IEnumerator GrantRoutine()
        {
            float t = 0f;
            while (t < grantSeconds)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / grantSeconds);

                // 先推出去，後段停住 —— 停住那一下是「交給你了」
                grantOffset = Vector2.Lerp(Vector2.zero, grantPush, EaseOutBack(Mathf.Clamp01(u / 0.6f)));

                if (thumb != null)
                    thumb.localEulerAngles = new Vector3(0f, 0f,
                        thumbRestAngle + Mathf.Sin(u * Mathf.PI) * thumbOvershoot * 1.6f);

                Apply();
                yield return null;
            }
            motion = null;
        }

        private void Update()
        {
            if (breathAmplitude > 0f && reach > 0.99f)
            {
                breathPhase += Time.unscaledDeltaTime / breathSeconds * Mathf.PI * 2f;
                Apply();
            }

            Vector2 wantHover = (hovered && Pickable) ? hoverPush : Vector2.zero;
            if ((hoverOffset - wantHover).sqrMagnitude > 0.01f)
            {
                float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime / hoverSeconds);
                hoverOffset = Vector2.Lerp(hoverOffset, wantHover, k);
                Apply();
            }
        }

        private void Apply()
        {
            float breath = (reach > 0.99f && breathAmplitude > 0f)
                ? Mathf.Sin(breathPhase) * breathAmplitude : 0f;

            Vector2 from = restPos + offscreenOffset;
            Vector2 pos = Vector2.LerpUnclamped(from, restPos, reach);

            self.anchoredPosition = pos + hoverOffset + grantOffset + new Vector2(0f, breath);
            self.localEulerAngles = new Vector3(0f, 0f,
                Mathf.LerpUnclamped(restAngle + offscreenAngle, restAngle, reach));
        }

        private void SetHitEnabled(bool on)
        {
            if (hitArea != null) hitArea.raycastTarget = on;
        }

        private void StopMotion()
        {
            if (motion != null) { StopCoroutine(motion); motion = null; }
        }

        // ==========================================
        public void OnPointerEnter(PointerEventData e) { if (Pickable) hovered = true; }
        public void OnPointerExit(PointerEventData e) { hovered = false; }

        public void OnPointerClick(PointerEventData e)
        {
            if (!Pickable || e.dragging) return;
            var h = OnPicked;
            if (h != null) h(this);
        }

        // ==========================================
        private static float EaseOutCubic(float x) { float f = 1f - x; return 1f - f * f * f; }
        private static float EaseInCubic(float x) { return x * x * x; }

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float f = x - 1f;
            return 1f + c3 * f * f * f + c1 * f * f;
        }
    }
}
