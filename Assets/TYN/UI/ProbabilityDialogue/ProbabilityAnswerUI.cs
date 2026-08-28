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

        [Tooltip("被同色卡指到時（規格 §3.1 Highlight）")]
        public Color highlightTint = new Color(1f, 0.94f, 0.72f);

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
            if (!interactable) return;
            ApplyTint(on ? highlightTint : normalTint);
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
