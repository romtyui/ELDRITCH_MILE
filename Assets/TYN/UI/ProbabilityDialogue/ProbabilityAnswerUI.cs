using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EldritchMile.UI.ProbabilityDialogue
{
    using EldritchMile.Core.ProbabilityDialogue;

    /// <summary>
    /// 一個回答按鈕：文字 ＋ 目前機率 ＋ 顏色點。
    ///
    /// 規格書 §3.1：**顏色關係一定要明顯** ——
    /// 回答右上至少要有一個跟卡牌相同的色點；多色回答就顯示多個點。
    /// </summary>
    public class ProbabilityAnswerUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("元件")]
        public TextMeshProUGUI labelText;

        [Tooltip("目前成功機率。文字格式見 Probability Format")]
        public TextMeshProUGUI probabilityText;

        [Tooltip("圓弧進度條（Filled、Radial 360）。圖換成這個回答的屬性色")]
        public Image arcFill;

        [Tooltip("圓弧的預覽段：手上指著的牌打出去會到哪裡。同一張圖半透明，畫在 Arc Fill 底下")]
        public Image arcPreview;

        [Tooltip("數字旁邊的「%」加色點，圖換成這個回答的屬性色")]
        public Image markImage;

        [Serializable]
        public class AttributeArt
        {
            public EldritchMile.Core.ExploreAttribute attribute;
            public Sprite arc;
            public Sprite mark;
        }

        [Tooltip("各屬性用哪一組圖。美術把顏色畫在圖裡（紅綠藍各一張），所以這裡不染色。\n" +
                 "吃多種屬性的回答用清單裡第一個屬性的圖；找不到就用這張表的第一組")]
        public List<AttributeArt> attributeArt = new List<AttributeArt>();

        [Tooltip("機率 → Fill Amount 的對照表（0%、1%…100% 平均取樣）。\n" +
                 "圓弧繞著圖的中心轉，但各段離中心遠近不同，直接用機率當 Fill Amount 的話，弧長和機率對不上。\n" +
                 "換了弧的圖要重算：元件右上角選單 → 重算圓弧對照表")]
        public float[] arcFillTable = new float[0];

        [Tooltip("色點的容器。**色點會依這個回答吃的屬性動態生成**，\n" +
                 "所以這裡放一個空的 Layout Group 就好，不用預先擺點")]
        public RectTransform colorDotRoot;

        [Tooltip("單顆色點的 prefab。要有 Image 元件")]
        public Image colorDotPrefab;

        [Tooltip("整張的底圖。Highlight 與變暗都改它")]
        public Image background;

        [Header("外觀")]
        public string probabilityFormat = "{0}%";

        [Tooltip("一般狀態")]
        public Color normalTint = Color.white;

        [Tooltip("被同色卡指到時（規格 §3.1 Highlight）。\n\n" +
                 "**這是退路，不是主要的顏色** —— 打開下面那格之後，\n" +
                 "亮起來的顏色會照屬性算；這一格只在算不出顏色時才用到")]
        public Color highlightTint = new Color(1f, 0.94f, 0.72f);

        [Tooltip("亮起來的顏色**照屬性走**（本我紅／超我藍／自我綠）。\n\n" +
                 "【為什麼】色點、卡框、亮起來的回答本來就該是同一個顏色系統。\n" +
                 "統一用一個米黃色的話，玩家只知道「這張牌對這個回答有用」，\n" +
                 "但看不出**是哪一種屬性在起作用** —— 而雙屬性的回答正是要靠那個分辨。\n\n" +
                 "⚠️ 顏色是 View 算好傳進來的（`ColorOf`／混色），這裡不查表 ——\n" +
                 "在這裡再查一次就會有第二份對照表")]
        public bool tintHighlightByAttribute = true;

        [Tooltip("往屬性色偏多少。0 = 完全不變、1 = 整片變成屬性色。\n\n" +
                 "調太高底圖會變成一塊純色，回答的文字就看不清楚了；\n" +
                 "0.4~0.6 之間看得出是哪一色，字也還讀得動")]
        [Range(0f, 1f)] public float highlightBlend = 0.5f;

        [Tooltip("混完之後再整體提亮多少倍。**只動 RGB，不動 alpha** ——\n" +
                 "連 alpha 一起乘的話，半透明的底圖會在 hover 時整片浮出來")]
        [Range(1f, 2f)] public float highlightBoost = 1.12f;

        [Tooltip("失敗後不可再選（規格 §6：整體變暗）")]
        public Color disabledTint = new Color(0.42f, 0.42f, 0.45f);

        [Tooltip("失敗後整張的透明度。底圖是黑的，光靠染色看不出來")]
        [Range(0f, 1f)] public float disabledAlpha = 0.45f;

        [Tooltip("被牌指到時放大幾倍。底圖是黑的，光靠染色看不出來")]
        public float highlightScale = 1.04f;

        [Header("判定動效（成功／失敗那一下）")]
        [Tooltip("判定之後、變暗之前，先在這一格上閃一下並打出「成功／失敗」。\n\n" +
                 "【為什麼要有】原本是「按下去 → 選項直接變暗」——\n" +
                 "玩家看不到判定的那一刻，只看到結果，會覺得系統擅自決定了什麼。\n" +
                 "舊版的探索打牌（`DialogueOptionUI.PlayResultFlash`）本來就有這一下，\n" +
                 "機率對話換了新的 UI 之後掉了，這裡是補回來。\n\n" +
                 "取消勾選 = 回到直接變暗")]
        public bool playResultFlash = true;

        [Min(0.05f)] public float flashInSeconds = 0.28f;
        [Min(0f)] public float flashHoldSeconds = 0.7f;
        [Min(0.05f)] public float flashOutSeconds = 0.34f;

        [Tooltip("閃的時候底色壓到什麼顏色。深色才襯得出上面那兩個字")]
        public Color flashColor = new Color(0.06f, 0.05f, 0.06f, 1f);

        [Tooltip("成功那兩個字的顏色")]
        public Color successColor = new Color(0.55f, 0.90f, 0.60f);

        [Tooltip("失敗那兩個字的顏色")]
        public Color failureColor = new Color(0.92f, 0.45f, 0.42f);

        public string successWord = "成功";
        public string failureWord = "失敗";

        /// <summary>整段動效要跑多久。呼叫端要等它跑完再往下走。</summary>
        public float TotalFlashSeconds =>
            playResultFlash ? flashInSeconds + flashHoldSeconds + flashOutSeconds : 0f;

        [Header("機率變化")]
        [Tooltip("數字跑動的秒數。0 = 直接跳到新數字")]
        [Min(0f)] public float countUpSeconds = 0.35f;

        public event Action<ProbabilityAnswerUI> OnClicked;

        public ProbabilityDialogueSession.RuntimeOption Bound { get; private set; }

        private readonly List<Image> dots = new List<Image>();
        private Coroutine countUp;
        private bool interactable = true;

        // ==========================================
        public void Bind(ProbabilityDialogueSession.RuntimeOption option,
                         Func<EldritchMile.Core.ExploreAttribute, Color> colorLookup)
        {
            Bound = option;
            interactable = option != null && option.available;

            if (labelText != null) labelText.text = option?.source?.text ?? "";
            SetProbabilityImmediate(option != null ? option.currentProbability : 0);
            BuildDots(option, colorLookup);
            ApplyArt(option);
            ApplyTint(interactable ? normalTint : disabledTint);
            SetContentAlpha(interactable ? 1f : disabledAlpha);
            transform.localScale = Vector3.one;

            gameObject.SetActive(true);
        }

        /// <summary>
        /// 依這個回答吃的屬性生出色點。
        ///
        /// 【為什麼動態生成】回答可以吃一個或多個屬性（規格 §3.1），
        /// 預先擺固定數量的點就得處理「多的要藏起來」，而且改資料還要回頭改 prefab。
        ///
        /// 【顏色從哪來】`AttributeChartData` —— 卡框的顏色也是同一份來源，
        /// 所以**色點與卡框永遠對得上**，不會出現「這裡橘、那裡紅」。
        /// </summary>
        private void BuildDots(ProbabilityDialogueSession.RuntimeOption option,
                               Func<EldritchMile.Core.ExploreAttribute, Color> colorLookup)
        {
            for (int i = 0; i < dots.Count; i++) if (dots[i] != null) Destroy(dots[i].gameObject);
            dots.Clear();

            if (colorDotRoot == null || colorDotPrefab == null || option?.source?.acceptedAttributes == null) return;

            // 單一屬性的回答，圓弧和「%」的顏色已經講清楚了。色點只留給吃多種屬性的回答
            if (option.source.acceptedAttributes.Count < 2) return;

            foreach (EldritchMile.Core.ExploreAttribute attr in option.source.acceptedAttributes)
            {
                Image dot = Instantiate(colorDotPrefab, colorDotRoot);
                dot.color = colorLookup != null ? colorLookup(attr) : Color.white;
                dot.gameObject.SetActive(true);
                dots.Add(dot);
            }
        }

        /// <summary>換成這個回答的屬性色那一組圖。</summary>
        private void ApplyArt(ProbabilityDialogueSession.RuntimeOption option)
        {
            AttributeArt art = attributeArt.Count > 0 ? attributeArt[0] : null;
            List<EldritchMile.Core.ExploreAttribute> attrs = option?.source?.acceptedAttributes;

            if (attrs != null && attrs.Count > 0)
            {
                AttributeArt match = attributeArt.Find(a => a.attribute == attrs[0]);
                if (match != null) art = match;
            }

            if (art == null) return;

            if (arcFill != null) { arcFill.sprite = art.arc; arcFill.color = Color.white; }
            if (arcPreview != null) { arcPreview.sprite = art.arc; arcPreview.color = new Color(1f, 1f, 1f, 0.45f); }
            if (markImage != null) { markImage.sprite = art.mark; markImage.color = Color.white; }
        }

        /// <summary>機率畫到圓弧上。0% 一定全空，100% 一定全滿。</summary>
        private void SetArc(Image arc, int percent)
        {
            if (arc == null) return;

            if (percent <= 0) { arc.fillAmount = 0f; return; }
            if (arcFillTable == null || arcFillTable.Length < 2) { arc.fillAmount = percent / 100f; return; }

            float at = Mathf.Clamp01(percent / 100f) * (arcFillTable.Length - 1);
            int i = Mathf.Min(Mathf.FloorToInt(at), arcFillTable.Length - 2);
            arc.fillAmount = Mathf.Lerp(arcFillTable[i], arcFillTable[i + 1], at - i);
        }

        private CanvasGroup contentGroup;

        private void SetContentAlpha(float alpha)
        {
            if (contentGroup == null) contentGroup = GetComponent<CanvasGroup>();
            if (contentGroup == null) contentGroup = gameObject.AddComponent<CanvasGroup>();
            contentGroup.alpha = alpha;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 從弧的圖算出「機率 → Fill Amount」。每個不透明的像素算它落在幾度，累積起來就是弧長。
        /// Image 的 Radial 360 是在圖的 0~1 座標裡算角度，所以這裡也用 0~1 座標。
        /// </summary>
        [ContextMenu("重算圓弧對照表")]
        public void RebuildArcTable()
        {
            if (arcFill == null || attributeArt.Count == 0 || attributeArt[0].arc == null) return;

            string path = UnityEditor.AssetDatabase.GetAssetPath(attributeArt[0].arc.texture);
            var tex = new Texture2D(2, 2);
            ImageConversion.LoadImage(tex, System.IO.File.ReadAllBytes(path));
            Color32[] px = tex.GetPixels32();

            const int Bins = 1440;
            var hist = new int[Bins];
            int total = 0;

            // 起點與方向跟 Image 的 Fill Origin = Bottom、Clockwise = 否一致
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    if (px[y * tex.width + x].a < 128) continue;

                    float deg = Mathf.Atan2((y + 0.5f) / tex.height - 0.5f, (x + 0.5f) / tex.width - 0.5f) * Mathf.Rad2Deg + 90f;
                    if (deg < 0f) deg += 360f;

                    hist[Mathf.Min(Bins - 1, (int)(deg / 360f * Bins))]++;
                    total++;
                }
            }

            DestroyImmediate(tex);

            var table = new float[101];
            int sum = 0, bin = 0;

            for (int i = 0; i <= 100; i++)
            {
                int want = total * i / 100;
                while (bin < Bins - 1 && sum + hist[bin] < want) { sum += hist[bin]; bin++; }
                table[i] = (bin + 1f) / Bins;
            }

            arcFillTable = table;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        private int currentPercent;

        // ==========================================
        public void SetProbabilityImmediate(int value)
        {
            if (countUp != null) { StopCoroutine(countUp); countUp = null; }
            currentPercent = value;
            if (probabilityText != null) probabilityText.text = string.Format(probabilityFormat, value);
            SetArc(arcFill, value);
            SetArc(arcPreview, value);
        }

        /// <summary>
        /// 手上指著的牌打出去會到多少：圓弧的預覽段伸長、數字換成打出後的。null ＝ 收掉預覽。
        /// </summary>
        public void SetPreview(int? value)
        {
            int shown = value ?? currentPercent;
            SetArc(arcPreview, shown);
            if (probabilityText != null && countUp == null) probabilityText.text = string.Format(probabilityFormat, shown);
        }

        /// <summary>機率變化的動畫（規格 §10.1 probabilityChangeEffect）。</summary>
        public void AnimateProbability(int before, int after)
        {
            if (probabilityText == null) return;

            currentPercent = after;
            SetArc(arcPreview, after);   // 預覽段留在終點，填色追上去

            if (countUpSeconds <= 0f || before == after)
            {
                SetProbabilityImmediate(after);
                return;
            }

            if (countUp != null) StopCoroutine(countUp);
            countUp = StartCoroutine(CountUpRoutine(before, after));
        }

        private System.Collections.IEnumerator CountUpRoutine(int from, int to)
        {
            float t = 0f;
            while (t < countUpSeconds)
            {
                t += Time.unscaledDeltaTime;
                int v = Mathf.RoundToInt(Mathf.Lerp(from, to, Mathf.Clamp01(t / countUpSeconds)));
                probabilityText.text = string.Format(probabilityFormat, v);
                SetArc(arcFill, v);
                yield return null;
            }
            probabilityText.text = string.Format(probabilityFormat, to);
            SetArc(arcFill, to);
            countUp = null;
        }

        // ==========================================
        /// <summary>被同色卡指到時亮起來。已經不可用的回答**不亮**（規格 §3.1）。</summary>
        public void SetHighlighted(bool on)
        {
            SetHighlighted(on, null);
        }

        /// <summary>
        /// 亮起來，而且**用指定的屬性色亮**。
        ///
        /// 【顏色由誰決定】呼叫端（View）—— 它才知道玩家現在指著的是哪一張牌。
        /// 規則是「牌有屬性就用牌的顏色；黑白牌（`None`）對誰都有效，
        /// 那就退回**這個回答自己的顏色**，雙屬性的回答因此會是兩色的混色」。
        ///
        /// 【為什麼不在這裡算】這一格只認得自己綁的那個回答，
        /// 認不得玩家手上那張牌。把牌傳進來的話，這支 UI 就得認識卡牌型別 ——
        /// 那正是規格 §8「View 不記錄主要 State」要避開的方向。
        /// </summary>
        /// <param name="attributeColor">null ＝ 沒有屬性色可用，退回 `highlightTint`。</param>
        public void SetHighlighted(bool on, Color? attributeColor)
        {
            if (!interactable) return;

            transform.localScale = Vector3.one * (on ? highlightScale : 1f);

            if (!on) { ApplyTint(normalTint); return; }

            ApplyTint(tintHighlightByAttribute && attributeColor.HasValue
                ? HighlightFor(attributeColor.Value)
                : highlightTint);
        }

        /// <summary>
        /// 一般狀態 → 屬性色，混一段再提亮。
        ///
        /// ⚠️ **alpha 走 normalTint 的，不參與混色也不參與提亮。**
        /// 底圖若是半透明的，連 alpha 一起動的話 hover 會變成「整塊浮出來」，
        /// 那看起來像破圖而不是高亮。
        /// </summary>
        private Color HighlightFor(Color attribute)
        {
            Color mixed = Color.Lerp(normalTint, attribute, Mathf.Clamp01(highlightBlend));

            return new Color(
                Mathf.Clamp01(mixed.r * highlightBoost),
                Mathf.Clamp01(mixed.g * highlightBoost),
                Mathf.Clamp01(mixed.b * highlightBoost),
                normalTint.a);
        }

        /// <summary>
        /// 失敗後變暗且不可再點（規格 §6）。
        ///
        /// ⚠️ **動效還在跑的時候不會馬上變暗** —— 排到動效結束再變。
        /// Session 是「判定 → 立刻停用」同一幀連著發的，照做的話那一下閃光
        /// 會打在已經半透明的格子上，整個效果就弱掉了
        /// （舊版 `DialogueOptionUI.FlashRoutine` 的結尾也是同一個理由）。
        ///
        /// 判定的當下就鎖住點擊 —— 只有「看起來變暗」被延後，可不可以點沒有。
        /// </summary>
        public void SetDisabled()
        {
            interactable = false;
            transform.localScale = Vector3.one;
            SetArc(arcPreview, currentPercent);

            if (flashRoutine != null) { dimAfterFlash = true; return; }

            ApplyTint(disabledTint);
            SetContentAlpha(disabledAlpha);
        }

        /// <summary>
        /// 判定的那一下：底色壓暗、字換成「成功／失敗」、再淡回來。
        ///
        /// 【為什麼是換字不是另外疊一個標籤】疊標籤要處理位置、層級、
        /// 多行文字撐開的情況；換字則一定對得準，因為那本來就是這一格的文字。
        /// 舊版就是這樣做的。
        /// </summary>
        public void PlayResultFlash(bool success)
        {
            if (!playResultFlash || !isActiveAndEnabled) return;

            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine(success));
        }

        private Coroutine flashRoutine;
        private bool dimAfterFlash;

        private System.Collections.IEnumerator FlashRoutine(bool success)
        {
            string word = success ? successWord : failureWord;
            Color tone = success ? successColor : failureColor;
            string resultText = $"<color=#{ColorUtility.ToHtmlStringRGB(tone)}>{word}</color>";

            string back = labelText != null ? labelText.text : "";

            // 起點用**當下的顏色**，不是 normalTint ——
            // 這一格可能正被瞄準著（highlight），動效結束要回到那個狀態
            Color from = background != null ? background.color : Color.white;

            yield return FlashPhase(flashInSeconds, from, flashColor, resultText);

            if (flashHoldSeconds > 0f) yield return new WaitForSecondsRealtime(flashHoldSeconds);

            yield return FlashPhase(flashOutSeconds, flashColor, from, back);

            flashRoutine = null;

            // ⚠️ 變暗放在動效**之後**（見 SetDisabled 的說明）
            if (dimAfterFlash)
            {
                dimAfterFlash = false;
                ApplyTint(disabledTint);
                SetContentAlpha(disabledAlpha);
            }
        }

        /// <summary>底色 a → b，文字在中點淡出、換字、再淡入。</summary>
        private System.Collections.IEnumerator FlashPhase(float seconds, Color a, Color b, string newText)
        {
            float t = 0f;
            bool swapped = false;

            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / Mathf.Max(0.01f, seconds);
                float k = Mathf.Clamp01(t);

                if (background != null) background.color = Color.Lerp(a, b, k);

                if (labelText != null)
                {
                    labelText.alpha = k < 0.5f ? 1f - k * 2f : (k - 0.5f) * 2f;

                    if (!swapped && k >= 0.5f) { labelText.text = newText; swapped = true; }
                }

                yield return null;
            }

            if (background != null) background.color = b;
            if (labelText != null) { labelText.text = newText; labelText.alpha = 1f; }
        }

        private void ApplyTint(Color c)
        {
            if (background != null) background.color = c;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!interactable) return;
            OnClicked?.Invoke(this);
        }
    }
}
