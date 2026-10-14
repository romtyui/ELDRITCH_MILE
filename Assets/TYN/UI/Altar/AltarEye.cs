using UnityEngine;
using UnityEngine.InputSystem;

namespace EldritchMile.UI
{
    /// <summary>
    /// 祭壇中央那隻眼睛 —— 眼球會一直看著滑鼠。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【它在看玩家，不是在被玩家操作】所以**不是**把眼球放到游標位置，
    /// 而是朝游標的方向偏移一點點，偏移量限制在眼眶內的一個橢圓裡。
    /// 眼白是扁的（509x178），所以水平能跑的範圍比垂直大得多 ——
    /// 用圓形去夾的話，眼球往上下看時會凸出眼眶外。
    ///
    /// 【為什麼要阻尼】死跟著游標會像貼紙，而且游標一停就完全靜止，看起來是壞的。
    /// 指數平滑讓它「追過去」而不是「黏過去」，而且任何幀率下軌跡都一樣
    /// （`Lerp(cur, goal, dt * speed)` 那種寫法在高幀率會快很多，跟 SlideOutTab 同一個坑）。
    /// </summary>
    public class AltarEye : MonoBehaviour
    {
        [Header("元件")]
        [Tooltip("會動的眼球。眼白本身不動")]
        public RectTransform pupil;

        [Tooltip("算滑鼠座標要用的畫布。留空會往上找")]
        public Canvas canvas;

        [Header("可動範圍（以眼球的原位為中心的橢圓，畫布單位）")]
        [Tooltip("左右能跑多遠。眼白是扁的，這個值會比垂直大很多")]
        [Min(0f)] public float radiusX = 95f;

        [Tooltip("上下能跑多遠")]
        [Min(0f)] public float radiusY = 26f;

        [Header("不能超出眼球（2026-10-13 美術回報：虹膜會跑出那個圓）")]
        [Tooltip("眼球那個圓（其實是橢圓）的圓心，跟虹膜同一個座標系（Eye 底下）。\n"
                 + "從《眼睛.png》填色量出來的：內部區域與橢圓面積差 0.2%")]
        public Vector2 eyeballCenter = new Vector2(-15.8f, 7.3f);

        [Tooltip("眼球的半軸（左右、上下）")]
        public Vector2 eyeballRadius = new Vector2(82.6f, 64.1f);

        [Tooltip("虹膜自己的半徑。虹膜中心可以走的範圍 = 眼球半軸 − 這個值，整顆才會留在圓裡")]
        [Min(0f)] public float irisRadius = 35f;

        [Header("手感")]
        [Tooltip("游標離眼睛多遠就算「看到底」。再遠不會更偏，只是方向改變")]
        [Min(1f)] public float fullDeflectionDistance = 700f;

        [Tooltip("大約多久追到定位（秒）。0.1~0.25 之間最像活的")]
        [Min(0.01f)] public float smoothSeconds = 0.18f;

        [Header("眨眼（可留 0 關掉）")]
        [Tooltip("兩次眨眼之間最短幾秒")]
        [Min(0f)] public float blinkEveryMin = 3.5f;

        [Tooltip("兩次眨眼之間最長幾秒。設 0 = 不眨")]
        [Min(0f)] public float blinkEveryMax = 7f;

        [Tooltip("眨一次多久（秒）")]
        [Min(0.02f)] public float blinkSeconds = 0.12f;

        private RectTransform self;
        private Vector2 home;
        private Vector2 goal;
        private float nextBlink;
        private float blinkUntil;
        private Vector3 pupilScale;

        private void Awake()
        {
            self = (RectTransform)transform;
            if (canvas == null) canvas = GetComponentInParent<Canvas>();

            if (pupil == null)
            {
                Debug.LogWarning($"[祭壇眼睛]「{name}」沒有指定眼球，不會有反應", this);
                enabled = false;
                return;
            }

            home = pupil.anchoredPosition;
            pupilScale = pupil.localScale;
            ScheduleBlink();
        }

        private void OnEnable()
        {
            if (pupil != null)
            {
                pupil.anchoredPosition = home;
                pupil.localScale = pupilScale;
            }
            goal = Vector2.zero;
            ScheduleBlink();
        }

        private void Update()
        {
            TrackMouse();
            TickBlink();
        }

        private void TrackMouse()
        {
            if (canvas == null) return;

            var canvasRect = canvas.transform as RectTransform;
            if (canvasRect == null) return;

            // Overlay 畫布要傳 null 當相機 —— 傳了相機反而會算錯
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            // ⚠️ 一定要走 Input System 的 Mouse.current。
            //    這個專案的 activeInputHandler 是「新 Input System」，
            //    舊的 UnityEngine.Input.mousePosition 執行時會直接丟 InvalidOperationException。
            //    沒有滑鼠（手把、觸控）時 Mouse.current 是 null，那不是錯誤，眼球維持不動就好。
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 mouseScreen = mouse.position.ReadValue();

            Vector2 mouseLocal;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, mouseScreen, cam, out mouseLocal)) return;

            // 眼睛自己在畫布上的位置
            Vector2 eyeLocal;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, RectTransformUtility.WorldToScreenPoint(cam, self.position), cam, out eyeLocal);

            Vector2 d = mouseLocal - eyeLocal;
            float dist = d.magnitude;

            if (dist > 0.001f)
            {
                // 方向保留，但強度隨距離飽和 —— 游標貼在眼睛上時不該整顆彈到邊緣
                float t = Mathf.Clamp01(dist / fullDeflectionDistance);
                Vector2 dir = d / dist;
                goal = new Vector2(dir.x * radiusX, dir.y * radiusY) * t;
            }

            // 目標點夾進眼球裡。橢圓是凸的，所以兩個圈內的點之間平滑過去也一定在圈內
            goal = ClampInsideEyeball(home + goal) - home;

            float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime / smoothSeconds);
            Vector2 next = Vector2.Lerp(pupil.anchoredPosition - home, goal, k);
            pupil.anchoredPosition = home + next;
        }

        /// <summary>把虹膜中心夾在「眼球半軸 − 虹膜半徑」的橢圓裡。超出時沿著圓心方向拉回邊上。</summary>
        private Vector2 ClampInsideEyeball(Vector2 p)
        {
            float ax = Mathf.Max(0.01f, eyeballRadius.x - irisRadius);
            float ay = Mathf.Max(0.01f, eyeballRadius.y - irisRadius);

            Vector2 d = p - eyeballCenter;
            float e = (d.x * d.x) / (ax * ax) + (d.y * d.y) / (ay * ay);
            if (e <= 1f) return p;

            return eyeballCenter + d / Mathf.Sqrt(e);
        }

        private void TickBlink()
        {
            if (blinkEveryMax <= 0f) return;

            float now = Time.unscaledTime;

            if (blinkUntil > 0f)
            {
                if (now >= blinkUntil)
                {
                    blinkUntil = 0f;
                    pupil.localScale = pupilScale;
                    ScheduleBlink();
                }
                return;
            }

            if (now < nextBlink) return;

            // 沒有眨眼的圖，所以用「眼球壓扁一下」代替 —— 比整顆消失自然
            blinkUntil = now + blinkSeconds;
            pupil.localScale = new Vector3(pupilScale.x, pupilScale.y * 0.12f, pupilScale.z);
        }

        private void ScheduleBlink()
        {
            if (blinkEveryMax <= 0f) return;
            float lo = Mathf.Min(blinkEveryMin, blinkEveryMax);
            nextBlink = Time.unscaledTime + Random.Range(lo, blinkEveryMax);
        }
    }
}
