using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EldritchMile.UI
{
    /// <summary>
    /// 祭壇的氣氛層 —— 把背景壓暗，而且**隨著事件走到哪裡即時變化**。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【為什麼需要】（2026-10-12）美術稿的背景是整片壓到接近全黑的暖褐
    /// （四角 #100B06、中央 #423833），場上現在跑的是房間原本的亮度 ——
    /// 兩隻慘白的手直接疊在亮背景上，手搶不過背景。
    ///
    /// 【兩層，不是一層】
    ///   · **Dim**：整片平塗。負責「整體暗下來」
    ///   · **Vignette**：程式生成的放射漸層，中央透明、邊緣不透明。
    ///     負責「把視線收到中間」。稿子上的四角比中央暗三倍，靠平塗做不出來
    ///
    /// 【為什麼漸層是程式畫的】只為了一張放射漸層去要素材、進版控、設匯入參數
    /// 不划算，而且參數（內外半徑）之後一定會調。
    /// 這裡用 <see cref="BuildVignette"/> 當場畫一張 192x192，幾乎不花時間，
    /// 美術改 inner / outer 就直接重生成。要換成手繪的也行 —— 填
    /// <see cref="vignetteSprite"/> 就會優先用它。
    ///
    /// 【為什麼兩層都是 runtime 生出來的】祭壇的 prefab 已經夠滿了，
    /// 而且這兩層不需要美術在 Hierarchy 上調位置（它們永遠是滿版）。
    /// 生出來之後 `SetAsFirstSibling`，所以保證在手、眼睛、YES/NO **後面**。
    ///
    /// 【它壓不到 HUD，這是故意的】這一層在 AltarUI（sortingOrder 95）裡面，
    /// 壓得到世界空間的場景美術，壓不到 Canvas_Stage(100) 以上。
    /// 左上角齒輪、資源列這些**本來就該保持看得清楚**。
    ///
    /// 【之後接神牌特效】<see cref="SetAccent"/> 把整層的色相往某個神的顏色推。
    /// 深淵給藍、豐饒給暖黃 —— 到時候在 <see cref="AltarEventView"/> 選到手的那一刻呼叫。
    /// </summary>
    [DisallowMultipleComponent]
    public class AltarMood : MonoBehaviour
    {
        public enum Phase
        {
            /// <summary>還沒開始。完全不壓暗</summary>
            Hidden,
            /// <summary>兩隻手正在伸進來</summary>
            Reaching,
            /// <summary>「你願意相信嗎?」亮著</summary>
            Asking,
            /// <summary>說了相信，正在挑手</summary>
            Picking,
            /// <summary>某隻手在遞牌</summary>
            Granting,
            /// <summary>收場，亮回去</summary>
            Leaving
        }

        /// <summary>一個階段長什麼樣。美術直接在 Inspector 上調這張表。</summary>
        [Serializable]
        public class Step
        {
            public Phase phase;

            [Tooltip("平塗層的濃度。0 = 不壓暗，1 = 全黑")]
            [Range(0f, 1f)] public float dim = 0.4f;

            [Tooltip("暗角的濃度")]
            [Range(0f, 1f)] public float vignette = 0.6f;

            [Tooltip("暗角的大小。小於 1 = 收緊（像管狀視野），大於 1 = 放鬆")]
            [Range(0.6f, 1.6f)] public float vignetteScale = 1f;

            [Tooltip("幾秒走到這個狀態。這是指數趨近的時間常數，不是硬時間")]
            [Min(0.01f)] public float seconds = 0.8f;
        }

        [Header("底色（取自美術稿：四角 #100B06、中央 #423833 的暖褐黑）")]
        [Tooltip("壓暗用的顏色。**不要用純黑** —— 稿子是偏暖的褐黑，純黑會讓場景看起來髒")]
        public Color baseTint = new Color32(0x14, 0x0D, 0x08, 0xFF);

        [Header("各階段")]
        public List<Step> steps = new List<Step>
        {
            new Step { phase = Phase.Hidden,   dim = 0f,    vignette = 0f,    vignetteScale = 1.3f, seconds = 0.6f },
            new Step { phase = Phase.Reaching, dim = 0.36f, vignette = 0.55f, vignetteScale = 1.15f, seconds = 1.1f },
            new Step { phase = Phase.Asking,   dim = 0.5f,  vignette = 0.7f,  vignetteScale = 1f,    seconds = 0.9f },
            new Step { phase = Phase.Picking,  dim = 0.46f, vignette = 0.8f,  vignetteScale = 0.86f, seconds = 1.2f },
            new Step { phase = Phase.Granting, dim = 0.62f, vignette = 0.88f, vignetteScale = 0.78f, seconds = 0.5f },
            new Step { phase = Phase.Leaving,  dim = 0f,    vignette = 0f,    vignetteScale = 1.3f, seconds = 0.7f },
        };

        [Header("活著的感覺")]
        [Tooltip("濃度上下起伏多少。0 = 完全靜止（會像一張死掉的半透明黑板）")]
        [Range(0f, 0.1f)] public float pulseAmount = 0.028f;

        [Tooltip("一次起伏幾秒。慢一點比較像呼吸")]
        [Min(0.5f)] public float pulseSeconds = 5.5f;

        [Header("暗角形狀")]
        [Tooltip("中央留多乾淨。0.22 大約是眼睛那一圈完全不受影響")]
        [Range(0f, 0.8f)] public float vignetteInner = 0.22f;

        [Tooltip("到哪裡全黑。1 大約是畫面四角")]
        [Range(0.3f, 1.6f)] public float vignetteOuter = 0.88f;

        [Tooltip("兩層鋪多大（相對於畫布）。要大於 1，縮小時才不會露出邊")]
        [Range(1.2f, 2.5f)] public float oversize = 1.7f;

        [Header("可選：改用手繪的暗角")]
        [Tooltip("留空 = 程式生成。填了就用這張（要中央透明、邊緣不透明）")]
        public Sprite vignetteSprite;

        [Header("可選：自己指定兩層（留空會自己生）")]
        public Image dimLayer;
        public Image vignetteLayer;

        private Step current;          // 目前趨近的目標
        private float dimNow, vigNow, scaleNow;
        private Color accent = Color.white;
        private RectTransform host;
        private Texture2D generated;

        private void Awake()
        {
            host = transform as RectTransform;

            EnsureLayers();

            current = Find(Phase.Hidden);
            dimNow = current.dim;
            vigNow = current.vignette;
            scaleNow = current.vignetteScale;
            Push();
        }

        private void OnDestroy()
        {
            // 程式畫出來的貼圖不會被 GC 收，要自己丟
            if (generated != null) Destroy(generated);
        }

        /// <summary>切到某個階段。不會瞬間跳過去，會用那個階段自己的 seconds 趨近。</summary>
        public void SetPhase(Phase phase)
        {
            current = Find(phase);
        }

        /// <summary>
        /// 把整層的顏色往某個神的色調推。1 = 完全用 <see cref="baseTint"/>。
        /// 之後神牌特效進來時，在選到手的那一刻呼叫。
        /// </summary>
        public void SetAccent(Color tint)
        {
            accent = tint;
            Push();
        }

        private void Update()
        {
            float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime / Mathf.Max(0.01f, current.seconds));

            dimNow = Mathf.Lerp(dimNow, current.dim, k);
            vigNow = Mathf.Lerp(vigNow, current.vignette, k);
            scaleNow = Mathf.Lerp(scaleNow, current.vignetteScale, k);

            Push();
        }

        private void Push()
        {
            // 脈動疊在濃度上，但**完全透明的時候不脈動** —— 不然關掉的時候會隱隱閃
            float pulse = pulseAmount > 0f
                ? Mathf.Sin(Time.unscaledTime / pulseSeconds * Mathf.PI * 2f) * pulseAmount
                : 0f;

            Color c = baseTint * accent;

            if (dimLayer != null)
            {
                float a = Mathf.Clamp01(dimNow + pulse * Mathf.Clamp01(dimNow * 4f));
                dimLayer.color = new Color(c.r, c.g, c.b, a);
                dimLayer.enabled = a > 0.002f;
            }

            if (vignetteLayer != null)
            {
                float a = Mathf.Clamp01(vigNow - pulse * Mathf.Clamp01(vigNow * 4f));
                vignetteLayer.color = new Color(c.r, c.g, c.b, a);
                vignetteLayer.enabled = a > 0.002f;
                vignetteLayer.rectTransform.localScale = new Vector3(scaleNow, scaleNow, 1f);
            }
        }

        private Step Find(Phase phase)
        {
            for (int i = 0; i < steps.Count; i++)
                if (steps[i] != null && steps[i].phase == phase) return steps[i];

            Debug.LogWarning($"[祭壇氣氛] 表上沒有「{phase}」這個階段，先用不壓暗頂著", this);
            return new Step { phase = phase, dim = 0f, vignette = 0f, vignetteScale = 1f, seconds = 0.5f };
        }

        // ==========================================
        private void EnsureLayers()
        {
            if (dimLayer == null) dimLayer = MakeLayer("_MoodDim", null);
            if (vignetteLayer == null)
                vignetteLayer = MakeLayer("_MoodVignette", vignetteSprite != null ? vignetteSprite : BuildVignette());

            // 一定要在最底下，否則會蓋住手跟 YES / NO。Dim 在 Vignette 下面
            if (vignetteLayer != null) vignetteLayer.rectTransform.SetAsFirstSibling();
            if (dimLayer != null) dimLayer.rectTransform.SetAsFirstSibling();
        }

        private Image MakeLayer(string layerName, Sprite sprite)
        {
            var go = new GameObject(layerName, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(host, false);

            // 鋪得比畫布大，縮小的時候才不會露出邊
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = ReferenceSize() * oversize;

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;      // ⚠️ 不關的話整片會吃掉點手與 YES / NO 的點擊
            img.color = new Color(baseTint.r, baseTint.g, baseTint.b, 0f);

            return img;
        }

        /// <summary>
        /// 畫布的參考尺寸。CanvasScaler 是 ScaleWithScreenSize，所以版面單位
        /// 永遠是 referenceResolution（1920x1080），不是實際像素。
        /// </summary>
        private Vector2 ReferenceSize()
        {
            var scaler = GetComponentInParent<CanvasScaler>();
            if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                return scaler.referenceResolution;

            if (host != null && host.rect.width > 1f) return host.rect.size;
            return new Vector2(1920f, 1080f);
        }

        /// <summary>
        /// 當場畫一張放射漸層：中央透明、邊緣不透明。顏色全白，實際色調由 Image.color 決定。
        ///
        /// 半徑是**正規化到貼圖半寬**算的，而貼圖會被拉成畫面比例 ——
        /// 等於漸層自動變成橢圓，跟著畫面長寬比走。暗角本來就該這樣。
        /// </summary>
        private Sprite BuildVignette()
        {
            const int size = 192;

            generated = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "_AltarVignette",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            float half = size * 0.5f;
            float inner = Mathf.Min(vignetteInner, vignetteOuter - 0.02f);
            var px = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                float dy = (y + 0.5f - half) / half;
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - half) / half;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);

                    float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((r - inner) / (vignetteOuter - inner)));
                    px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }

            generated.SetPixels32(px);
            generated.Apply(false, false);

            return Sprite.Create(generated, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
                                 0, SpriteMeshType.FullRect);
        }
    }
}
