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
            ApplyTint(interactable ? normalTint : disabledTint);

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

            foreach (EldritchMile.Core.ExploreAttribute attr in option.source.acceptedAttributes)
            {
                Image dot = Instantiate(colorDotPrefab, colorDotRoot);
                dot.color = colorLookup != null ? colorLookup(attr) : Color.white;
                dot.gameObject.SetActive(true);
                dots.Add(dot);
            }
        }

        // ==========================================
        public void SetProbabilityImmediate(int value)
        {
            if (countUp != null) { StopCoroutine(countUp); countUp = null; }
            if (probabilityText != null) probabilityText.text = string.Format(probabilityFormat, value);
        }

        /// <summary>機率變化的動畫（規格 §10.1 probabilityChangeEffect）。</summary>
        public void AnimateProbability(int before, int after)
        {
            if (probabilityText == null) return;

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
                yield return null;
            }
            probabilityText.text = string.Format(probabilityFormat, to);
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

        /// <summary>失敗後變暗且不可再點（規格 §6）。</summary>
        public void SetDisabled()
        {
            interactable = false;
            ApplyTint(disabledTint);
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
