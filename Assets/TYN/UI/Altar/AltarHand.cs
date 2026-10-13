using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EldritchMile.UI
{
    /// <summary>
    /// 祭壇的一隻手 —— 用**正向運動學關節鏈**驅動，不是整塊圖在平移。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【為什麼改寫】（2026-10-12）第一版是「整隻手沿直線滑進來 + 拇指轉一下」。
    /// 直線移動是這種演出最容易出戲的地方 —— **真實的肢體不會滑行，它繞著關節轉**。
    /// 現在的鏈是三節：
    ///
    ///     肩（畫面外的虛擬支點）→ 腕（Palm 的 pivot）→ 拇指（Thumb 的 pivot）
    ///
    /// 【「優美」實際上是四個東西，不是一條曲線】
    ///   1. **弧線而不是直線**。手繞畫面外的肩點轉進來，同時沿著手臂軸伸出 ——
    ///      兩個動作疊起來就是「手臂從外面推進來」，而不是貼紙在滑。
    ///      肩點是算出來的虛擬點，**不需要在 Hierarchy 裡真的放一個物件**。
    ///   2. **遞延**（overlapping action）。腕比肩晚 `delay` 秒，拇指又比腕晚。
    ///      動作從近端往遠端傳，像鞭子。整隻手同時到位 = 機械手臂。
    ///   3. **阻尼回彈**（<see cref="Settle"/>）而不是單次 EaseOutBack。
    ///      每一節會越過定位再盪回來，盪個一兩下遞減 —— 這是「有重量」的來源。
    ///   4. **到位之後每一節各自呼吸**，週期與相位都不同（4.3 / 3.1 / 2.6 秒）。
    ///      共用同一個上下飄會變成整隻手在抖；錯開才會像在微微起伏。
    ///
    /// 【為什麼不是 Unity 的 2D 骨骼（Sprite Skinning / IK）】試過路線，兩個擋點：
    ///   · Skinning Editor 只吃 SpriteRenderer，**UI 的 Image 不支援**。整個祭壇
    ///     在 Canvas 上（要跟 YES / NO、眼睛同層排版），搬到世界空間會把排版全部重來。
    ///   · 真正的骨骼變形（手指能彎）需要**美術先把圖拆成分層 PSB 或在 Skinning Editor
    ///     刷權重**。現在只有「手.png」「拇指.png」兩張平面圖，骨頭綁上去也只能整塊轉，
    ///     跟這裡的關節鏈結果一模一樣，卻多背一套 runtime。
    ///   要手指真的能彎，得請美術交**分層 PSB**（每根手指一層）—— 那時候再換不遲，
    ///   這支的對外介面（ReachIn / ReachToGive / Withdraw / SetHovered / PlayGrant）可以原封不動。
    ///
    /// 【左右手共用同一組參數】（2026-10-13 改）美術把左右手**分開畫了**，
    /// 所以不再用 `localScale.x = -1` 鏡像 —— 右手用自己的圖、scale 是 +1。
    /// 鏡像改由 <see cref="mirrored"/> 決定：打開時**所有動作的角度與水平偏移都取負**
    /// （手臂擺動、肩點 x、各關節的進場／hover／遞牌／呼吸角度），
    /// 兩隻手在 Inspector 上填一模一樣的動作參數就好。
    /// 例外是**姿勢本身**（rest 是 transform 上擺好的、give 是 Inspector 上填的）——
    /// 那是量出來的絕對值，每隻手各填各的。
    ///
    /// 【兩個姿勢】（2026-10-13）
    ///   · rest：一開始伸進來、擺在畫面兩側等你回答的姿勢（transform 上擺的）
    ///   · give：選完之後**拿著東西**再伸出來的姿勢（<see cref="givePosition"/> 那組）
    /// 兩個姿勢共用同一套弧線／遞延／回彈，差別只在終點。
    ///
    /// 【手不能點了】改版後是 YES／NO 兩個選項，hover 選項時對應的手會動
    /// （<see cref="SetHovered"/>），手本身不收點擊。
    /// </summary>
    public class AltarHand : MonoBehaviour
    {
        /// <summary>
        /// 關節鏈上的一節。每一節都只做一件事：**繞自己的 pivot 轉**。
        /// 位移完全由上一節帶，這就是為什麼 Palm 的 pivot 一定要在手腕上。
        /// </summary>
        [Serializable]
        public class Joint
        {
            [Tooltip("要轉的 RectTransform。pivot 必須在關節的位置上")]
            public RectTransform target;

            [Tooltip("伸出來之前比定位多轉幾度。會在過程中收斂到 0")]
            public float enterAngle = -16f;

            [Tooltip("比上一節晚幾秒才開始。**這一拍是關節感的來源**，全部填 0 的話整隻手會像一塊板子")]
            [Min(0f)] public float delay = 0.12f;

            [Tooltip("從開始到完全停住要幾秒")]
            [Min(0.05f)] public float settleSeconds = 0.9f;

            [Tooltip("回彈幾次。0.5 = 越過一次就停，1.2 = 盪兩下。超過 2 會像彈簧玩具")]
            [Min(0f)] public float wobbles = 0.62f;

            [Tooltip("回彈衰減得多快。小 = 盪得明顯，大 = 很快收住。2~5 之間最像肉")]
            [Min(0.5f)] public float damping = 3.2f;

            [Header("到位之後的呼吸")]
            [Tooltip("來回幾度。0 = 這一節不呼吸")]
            [Min(0f)] public float idleDegrees = 1.4f;

            [Tooltip("一次來回幾秒。**每一節要給不同的值**，一樣的話會變成整隻手在抖")]
            [Min(0.1f)] public float idleSeconds = 3.1f;

            [Tooltip("呼吸的起始相位（0~1）。錯開才不會所有關節同時到最高點")]
            [Range(0f, 1f)] public float idlePhase = 0f;

            [Header("互動時的偏移")]
            [Tooltip("滑鼠移上去時多轉幾度")]
            public float hoverAngle = 4f;

            [Tooltip("遞牌時多轉幾度")]
            public float grantAngle = 10f;

            // 從 transform 讀回來的定位角度。所有偏移都是**相對於它**的增量，
            // 所以量好的靜止姿勢永遠不會被動畫改掉
            [NonSerialized] public float restAngle;

            [NonSerialized] public float progress;   // 0 = 還在畫面外，1 = 定位（回彈時會短暫超過 1）
        }

        [Header("元件")]
        [Tooltip("手掌那張圖。pivot 要設在**手腕**上，旋轉才會像從手腕轉")]
        public RectTransform palm;

        [Tooltip("拇指那張圖。pivot 設在拇指根部")]
        public RectTransform thumb;

        [Tooltip("（已停用）舊版點手用的透明點擊區。改版後手不收點擊，留著只為了不弄壞 prefab 引用")]
        public Graphic hitArea;

        [Tooltip("送禮時拿著的東西掛在這裡（神牌）。排在 Palm 與 Thumb 之間 —— 牌在手掌前面、拇指壓在牌上")]
        public RectTransform giftAnchor;

        [Tooltip("之後美術要加的神牌特效掛在這裡。現在是空的")]
        public RectTransform fxAnchor;

        [Header("手臂（肩點是算出來的，Hierarchy 裡沒有這個物件）")]
        [Tooltip("從手腕指到肩膀的向量，父層座標。\n"
                 + "方向要**順著前臂**（跟手指反向），長度就是手臂長。\n"
                 + "越長 = 弧線越平緩。太短的話擺動幾乎看不出來")]
        public Vector2 shoulderOffset = new Vector2(-1366f, 1725f);

        [Tooltip("伸出來之前沿著手臂軸往後縮多遠。要大到整隻手都在畫面外")]
        [Min(0f)] public float retractDistance = 1600f;

        [Tooltip("伸出來之前整條手臂繞肩點多轉幾度。**這個值就是弧線的彎度**，0 = 退化成直線")]
        public float swingDegrees = -20f;

        [Tooltip("往前伸（繞肩）與沿軸推出，分開兩條進度。伸出比擺動晚這麼多秒，手會先轉向再推進去")]
        [Min(0f)] public float extendDelay = 0.1f;

        [Min(0.05f)] public float swingSeconds = 1f;
        [Min(0f)] public float swingWobbles = 0.55f;
        [Min(0.5f)] public float swingDamping = 3f;

        [Tooltip("手臂整體的呼吸（繞肩點，所以整隻手會沿弧線飄）。度")]
        [Min(0f)] public float armIdleDegrees = 0.5f;
        [Min(0.1f)] public float armIdleSeconds = 4.3f;
        [Range(0f, 1f)] public float armIdlePhase = 0f;

        [Header("關節")]
        [Tooltip("手腕 = Palm。pivot 已經在手腕上，所以直接轉它")]
        public Joint wrist = new Joint
        {
            enterAngle = -16f, delay = 0.12f, settleSeconds = 0.95f,
            wobbles = 0.62f, damping = 3.2f,
            idleDegrees = 1.4f, idleSeconds = 3.1f, idlePhase = 0.33f,
            hoverAngle = 4f, grantAngle = 10f
        };

        [Tooltip("拇指。鏈上最遠的一節，所以 delay 最大、回彈最明顯")]
        public Joint thumbJoint = new Joint
        {
            enterAngle = -22f, delay = 0.26f, settleSeconds = 0.88f,
            wobbles = 0.8f, damping = 2.6f,
            idleDegrees = 2.2f, idleSeconds = 2.6f, idlePhase = 0.67f,
            hoverAngle = 7f, grantAngle = 18f
        };

        [Header("滑鼠移上去（要先可選才有反應）")]
        [Tooltip("整條手臂繞肩點往前轉幾度")]
        public float hoverSwing = 1.6f;

        [Tooltip("沿手臂軸再往外推多少 —— 像把手遞出去一點")]
        public float hoverExtend = 34f;

        [Min(0.01f)] public float hoverSeconds = 0.22f;

        [Header("授予神牌")]
        public float grantSwing = 5f;
        public float grantExtend = 130f;
        [Min(0.05f)] public float grantSeconds = 0.9f;

        [Header("送禮姿勢（選完之後拿著東西伸出來的終點）")]
        [Tooltip("手腕的位置（父層座標）。左手的值是從美術稿《給牌》剪影比對出來的")]
        public Vector2 givePosition = new Vector2(-540f, 528f);

        [Tooltip("整隻手的角度")]
        public float giveAngle = 13.5f;

        [Tooltip("整隻手放大多少。稿子上送禮的手比伸進來時大一點（靠近鏡頭）")]
        [Min(0.1f)] public float giveScale = 1.07f;

        [Header("鏡像")]
        [Tooltip("右手打勾。所有**動作**的角度與水平偏移會取負，左右手的動作參數就能填一樣的值。\n"
                 + "姿勢（rest 的 transform、give 的那三格）是絕對值，不受影響")]
        public bool mirrored = false;

        /// <summary>整隻手完全到位要幾秒 —— 鏈上最慢那一節說了算。外面等待時間用這個。</summary>
        public float ReachSeconds
        {
            get
            {
                float t = Mathf.Max(swingSeconds, extendDelay + swingSeconds);
                t = Mathf.Max(t, wrist.delay + wrist.settleSeconds);
                t = Mathf.Max(t, thumbJoint.delay + thumbJoint.settleSeconds);
                return t;
            }
        }

        private RectTransform self;
        private Vector2 restPos;
        private float restAngle;

        // 這一次伸手的終點。rest 或 give —— 只在手收在畫面外時切換
        private Vector2 targetPos;
        private float targetAngle;
        private float targetScale = 1f;

        private float swingProgress;     // 0 = 擺到畫面外，1 = 定位
        private float extendProgress;    // 0 = 沿軸縮到最後，1 = 定位
        private float hoverBlend;
        private float grantBlend;

        private bool hovered;
        private bool settled;            // 到位了才開始呼吸
        private Coroutine motion;

        private void Awake()
        {
            self = (RectTransform)transform;
            restPos = self.anchoredPosition;
            restAngle = Wrap180(self.localEulerAngles.z);
            UseRestPose();

            wrist.target = wrist.target != null ? wrist.target : palm;
            thumbJoint.target = thumbJoint.target != null ? thumbJoint.target : thumb;

            CaptureRest(wrist);
            CaptureRest(thumbJoint);

            PrepareHidden();
        }

        private static void CaptureRest(Joint j)
        {
            if (j != null && j.target != null) j.restAngle = Wrap180(j.target.localEulerAngles.z);
        }

        /// <summary>擺回畫面外、不可點。進場前叫一次。</summary>
        public void PrepareHidden()
        {
            StopMotion();
            swingProgress = 0f;
            extendProgress = 0f;
            hoverBlend = 0f;
            grantBlend = 0f;
            wrist.progress = 0f;
            thumbJoint.progress = 0f;
            hovered = false;
            settled = false;
            SetHitEnabled(false);
            Apply();
        }

        /// <summary>從畫面外伸進來，停在 rest 姿勢。<paramref name="delay"/> 讓兩隻手錯開。</summary>
        public Coroutine ReachIn(float delay)
        {
            StopMotion();
            UseRestPose();
            motion = StartCoroutine(ReachRoutine(delay));
            return motion;
        }

        /// <summary>
        /// 從畫面外**拿著東西**伸出來，停在送禮姿勢。要先 <see cref="Withdraw"/> 收回去再叫 ——
        /// 終點是在手收在畫面外時切換的，手還在畫面上時切會瞬移。
        /// </summary>
        public Coroutine ReachToGive(float delay)
        {
            StopMotion();
            swingProgress = extendProgress = wrist.progress = thumbJoint.progress = 0f;
            hoverBlend = grantBlend = 0f;
            settled = false;
            targetPos = givePosition;
            targetAngle = giveAngle;
            targetScale = giveScale;
            Apply();
            motion = StartCoroutine(ReachRoutine(delay));
            return motion;
        }

        /// <summary>
        /// 由外部（YES／NO 選項的 hover）驅動「這隻手被注意到了」的動作。
        /// 改版前是滑鼠直接移到手上，現在手不收點擊。
        /// </summary>
        public void SetHovered(bool on)
        {
            hovered = on;
        }

        private void UseRestPose()
        {
            targetPos = restPos;
            targetAngle = restAngle;
            targetScale = 1f;
        }

        /// <summary>縮回畫面外。玩家選「否」或拿完牌之後用。</summary>
        public Coroutine Withdraw()
        {
            StopMotion();
            SetHitEnabled(false);
            hovered = false;
            settled = false;
            motion = StartCoroutine(WithdrawRoutine());
            return motion;
        }

        /// <summary>被選中：把牌遞出來的那一下。</summary>
        public Coroutine PlayGrant()
        {
            StopMotion();
            SetHitEnabled(false);
            motion = StartCoroutine(GrantRoutine());
            return motion;
        }

        // ==========================================
        private IEnumerator ReachRoutine(float delay)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

            float total = ReachSeconds;
            float t = 0f;

            while (t < total)
            {
                t += Time.unscaledDeltaTime;
                Advance(t);
                Apply();
                yield return null;
            }

            Advance(total);
            settled = true;
            Apply();
            motion = null;
        }

        /// <summary>照時間把鏈上每一節往前推。每一節的曲線互相獨立，差別就在 delay。</summary>
        private void Advance(float t)
        {
            swingProgress = Settle(t / swingSeconds, swingWobbles, swingDamping);
            extendProgress = Settle((t - extendDelay) / swingSeconds, swingWobbles, swingDamping);
            wrist.progress = Settle((t - wrist.delay) / wrist.settleSeconds, wrist.wobbles, wrist.damping);
            thumbJoint.progress = Settle((t - thumbJoint.delay) / thumbJoint.settleSeconds,
                                         thumbJoint.wobbles, thumbJoint.damping);
        }

        /// <summary>
        /// 收回去。**遞延的順序反過來** —— 拇指先鬆、再來手腕、手臂最後才抽走。
        /// 遠端先動看起來像「不情願地放手」；整隻一起退會變成倒帶。
        /// </summary>
        private IEnumerator WithdrawRoutine()
        {
            float fromSwing = swingProgress, fromExtend = extendProgress;
            float fromWrist = wrist.progress, fromThumb = thumbJoint.progress;

            float dur = ReachSeconds * 0.75f;
            float t = 0f;

            while (t < dur)
            {
                t += Time.unscaledDeltaTime;

                thumbJoint.progress = Mathf.Lerp(fromThumb, 0f, EaseInCubic(Span(t, 0f, dur * 0.7f)));
                wrist.progress = Mathf.Lerp(fromWrist, 0f, EaseInCubic(Span(t, dur * 0.1f, dur * 0.8f)));
                extendProgress = Mathf.Lerp(fromExtend, 0f, EaseInCubic(Span(t, dur * 0.2f, dur)));
                swingProgress = Mathf.Lerp(fromSwing, 0f, EaseInCubic(Span(t, dur * 0.25f, dur)));

                Apply();
                yield return null;
            }

            swingProgress = extendProgress = wrist.progress = thumbJoint.progress = 0f;
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
                grantBlend = EaseOutBack(Mathf.Clamp01(u / 0.6f));

                Apply();
                yield return null;
            }
            grantBlend = 1f;
            Apply();
            motion = null;
        }

        private void Update()
        {
            float wantHover = hovered ? 1f : 0f;
            bool changed = false;

            if (Mathf.Abs(hoverBlend - wantHover) > 0.0005f)
            {
                hoverBlend = Mathf.Lerp(hoverBlend, wantHover,
                                        1f - Mathf.Exp(-Time.unscaledDeltaTime / hoverSeconds));
                changed = true;
            }

            // 呼吸只在到位之後跑。還在路上的時候疊上去會干擾回彈的讀法
            if (settled) changed = true;

            if (changed) Apply();
        }

        // ==========================================
        /// <summary>
        /// 把整條鏈算成 transform。**每一幀重算全部**，不累加 ——
        /// 累加的寫法只要有一幀被打斷就會漂走，而且 hover / 呼吸 / 遞牌是同時疊的。
        /// </summary>
        private void Apply()
        {
            float mirror = Mirror();
            float alive = Mathf.Clamp01(swingProgress);   // 呼吸隨著伸出來的進度淡入

            // ── 手臂：繞畫面外的肩點轉，再沿著手臂軸推出去
            float theta = mirror * (
                swingDegrees * (1f - swingProgress)
                + hoverSwing * hoverBlend
                + grantSwing * grantBlend
                + Idle(armIdleDegrees, armIdleSeconds, armIdlePhase) * alive);

            // 肩點跟著姿勢轉、跟著大小縮放 —— 送禮姿勢的手比 rest 多轉了幾度，
            // 手臂軸也要跟著轉，伸出來的方向才會貼著那隻手的前臂
            Vector2 offset = Rotate(new Vector2(shoulderOffset.x * mirror, shoulderOffset.y), targetAngle - restAngle)
                             * targetScale;
            Vector2 shoulder = targetPos + offset;

            Vector2 pos = shoulder + Rotate(targetPos - shoulder, theta);

            // 手臂軸（肩 → 腕），跟著 theta 一起轉，推出的方向才會貼著手臂
            Vector2 axis = offset.sqrMagnitude > 0.0001f
                ? Rotate((-offset).normalized, theta)
                : Rotate(Vector2.right, theta);

            float along = retractDistance * (1f - extendProgress)
                          - hoverExtend * hoverBlend
                          - grantExtend * grantBlend;

            self.anchoredPosition = pos - axis * along;
            self.localEulerAngles = new Vector3(0f, 0f, targetAngle + theta);
            self.localScale = new Vector3(targetScale, targetScale, 1f);

            // ── 遠端關節：純旋轉，位移由手臂帶。
            //    以前右手是 scale.x = -1，本地角度會被自動翻；現在兩隻手 scale 都是 +1，
            //    所以動作角度要自己乘上 mirror
            ApplyJoint(wrist, alive, mirror);
            ApplyJoint(thumbJoint, alive, mirror);
        }

        private void ApplyJoint(Joint j, float alive, float mirror)
        {
            if (j == null || j.target == null) return;

            float a = j.restAngle + mirror * (
                        j.enterAngle * (1f - j.progress)
                      + j.hoverAngle * hoverBlend
                      + j.grantAngle * grantBlend
                      + Idle(j.idleDegrees, j.idleSeconds, j.idlePhase) * alive);

            j.target.localEulerAngles = new Vector3(0f, 0f, a);
        }

        private static float Idle(float degrees, float seconds, float phase)
        {
            if (degrees <= 0f) return 0f;
            return Mathf.Sin((Time.unscaledTime / seconds + phase) * Mathf.PI * 2f) * degrees;
        }

        private float Mirror()
        {
            return mirrored ? -1f : 1f;
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
        /// <summary>
        /// 阻尼回彈：越過定位再盪回來，振幅遞減。
        ///
        /// 原始式子 `1 - e^(-dx)·cos(2πwx)` 在 x=1 時不會剛好等於 1（差幾 %），
        /// 直接截斷的話最後一幀會跳一下。所以補一條線性修正，
        /// 讓它 **f(0)=0、f(1)=1 剛好成立**，而中段的回彈形狀原封不動。
        /// </summary>
        private static float Settle(float x, float wobbles, float damping)
        {
            if (x <= 0f) return 0f;
            if (x >= 1f) return 1f;

            float raw = 1f - Mathf.Exp(-damping * x) * Mathf.Cos(wobbles * Mathf.PI * 2f * x);
            float end = 1f - Mathf.Exp(-damping) * Mathf.Cos(wobbles * Mathf.PI * 2f);
            return raw + (1f - end) * x;
        }

        /// <summary>t 落在 [from, to] 的哪裡（0~1）。收回時每一節各自的時間窗用這個切。</summary>
        private static float Span(float t, float from, float to)
        {
            if (to <= from) return t >= to ? 1f : 0f;
            return Mathf.Clamp01((t - from) / (to - from));
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        private static float Wrap180(float a)
        {
            a %= 360f;
            if (a > 180f) a -= 360f;
            if (a < -180f) a += 360f;
            return a;
        }

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
