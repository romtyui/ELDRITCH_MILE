using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace EldritchMile.Core
{
    /// <summary>
    /// 對話框裡的一個選項。**它同時是一個判定目標**（C18）。
    ///
    /// 【這是 C18 的核心形狀】企劃草圖上那三列「A 50 / B 50 / C 50」講的就是這個：
    /// hover 一張手牌，每個選項各自顯示「這張牌打在我身上會有多少成功率」。
    /// 所以選項不是按鈕，是**目標** —— 跟探索房間裡的寶箱是同一種東西，
    /// 只是長得像一行對白而不是一個箱子。
    ///
    /// 【兩種模式】
    ///   · `ProbabilityTarget` —— 要用機率卡打通。對話用這個
    ///   · `PlainChoice` —— 點了就選，不判定。商店挑商品、特殊事件挑牌用這個
    /// 兩種共用同一組 UI 物件（`answer_1/2/3`），因為它們在畫面上就是同一個位置。
    ///
    /// 【衰減狀態在自己身上】C18④：同一個選項被反覆嘗試會越來越難，
    /// 換一個選項則各自獨立計算 —— 所以 `CurrentDecayMultiplier` 存在這裡，
    /// 而不是存在對話控制器上。
    /// </summary>
    public class DialogueOptionUI : MonoBehaviour, IProbabilityTarget,
        IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public enum Mode
        {
            /// 要用機率卡打通（對話選項）
            ProbabilityTarget = 0,

            /// 點了就選，不判定（商店商品、可挑的牌）
            PlainChoice = 1,
        }

        [Header("元件")]
        [Tooltip("選項內文。C18③ 的「判定結果反映在選項內文」就是改這個")]
        public TextMeshProUGUI labelText;

        [Tooltip("機率預覽。hover 手牌時顯示這張牌打在本選項上的成功率")]
        public TextMeshProUGUI previewLabel;

        // 【移除的欄位】原本有個 `button` 指向子物件 `Option Button`。
        // 那是當初拉預設 Button 留下的空殼 —— sprite 是 Unity 內建的 UISprite、
        // alpha 0（看不見）、onClick 零個監聽，唯一的作用是**擋住點擊**讓選項點不到。
        // 選項的點擊靠本類別的 IPointerClickHandler，不需要 Button。

        [Header("預覽樣式")]
        public Color normalColor = Color.white;
        public Color immuneColor = new Color(0.5f, 0.5f, 0.5f);

        [Tooltip("屬性完全不合時顯示什麼。**不要用「0%」** —— 要讓玩家看得出是屬性不合而非運氣差")]
        public string immuneText = "X";

        [Header("選定狀態 (C18①)")]
        [Tooltip("被選為主要目標時的底色。用顏色而不是縮放 —— 縮放會讓三個選項的版面跳動")]
        public Color selectedTint = new Color(1f, 0.92f, 0.65f);
        public Color unselectedTint = Color.white;

        /// 點了這個選項。由 `DialogueOptionsPanel` 訂閱後轉給 Stage。
        public event Action<DialogueOptionUI> OnClicked;

        /// 判定結束。bool = 成功與否。
        public event Action<DialogueOptionUI, bool, float> OnResolved;

        public Mode mode = Mode.ProbabilityTarget;

        /// 這是第幾個選項（0-based）。Stage 靠它把 UI 對回自己的資料。
        public int Index { get; private set; }

        /// 未著色的原文。判定結果要接在它後面（C18③）
        private string baseText = "";

        /// 已經把關鍵字包上顏色語法的版本。**畫面上顯示的是這個**
        private string displayText = "";

        /// 這個選項的關鍵字（會用屬性色顯示）
        private string keyword = "";

        private ExploreAttribute attribute = ExploreAttribute.None;
        private Image background;

        /// <summary>屬性的顏色與相剋規則都住在同一份資產裡。</summary>
        private static AttributeChartData Chart =>
            ProbabilityCheck.Instance != null ? ProbabilityCheck.Instance.chart : null;

        private void Awake()
        {
            background = GetComponent<Image>();
        }

        public void Bind(int index, string text, ExploreAttribute attr, Mode optionMode)
        {
            Bind(index, text, attr, optionMode, null);
        }

        /// <summary>
        /// 綁一個選項。<paramref name="keywordText"/> 是要用**屬性色**顯示的關鍵字。
        ///
        /// 【為什麼是「關鍵字」而不是讓文案自己打顏色標籤】
        /// 讓文案在內文裡手打 `&lt;color=#...&gt;` 的話，顏色會散在幾十句台詞裡，
        /// 之後美術調色就得全部重找。這裡只填「哪幾個字」，顏色由屬性決定 ——
        /// 換色只要改 `AttributeChart` 一個地方。
        /// </summary>
        public void Bind(int index, string text, ExploreAttribute attr, Mode optionMode, string keywordText)
        {
            Index = index;
            baseText = text ?? "";
            keyword = keywordText ?? "";
            attribute = attr;
            mode = optionMode;

            displayText = BuildDisplayText();

            CurrentDecayMultiplier = 1f;
            SetSpent(false);

            if (labelText != null) labelText.text = displayText;

            SetSelected(false);
            HidePreview();

            gameObject.SetActive(true);
        }

        public void Dismiss()
        {
            HidePreview();
            gameObject.SetActive(false);
        }

        /// <summary>C18①：被選為主要目標時的視覺。用底色，不用縮放。</summary>
        public void SetSelected(bool selected)
        {
            if (background != null)
            {
                background.color = selected ? selectedTint : unselectedTint;
            }
        }

        // ==========================================
        // 點擊
        // ==========================================
        /// <summary>
        /// ⚠️ 點擊有兩種意思，順序不能顛倒：
        ///   1. 手上有選取的卡 → 這是「兩段式出牌的第二段」，把卡打在這個選項上
        ///   2. 沒有選取的卡   → 這是「選定主要目標」（C18①）或「挑選」（PlainChoice）
        ///
        /// 判斷交給 `DialogueOptionsPanel`／Stage，本類別只負責回報「我被點了」。
        /// 這跟 `InteractableBase.OnPointerClick` 是同一個模式。
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging) return;   // 拖曳放開時 Unity 也會送 click

            OnClicked?.Invoke(this);
        }

        // ==========================================
        // IProbabilityTarget
        // ==========================================
        public string DisplayName => string.IsNullOrEmpty(baseText) ? name : baseText;

        ExploreAttribute IProbabilityTarget.Attribute => attribute;

        public float CurrentDecayMultiplier { get; private set; } = 1f;

        public void ApplyDecay(float step)
        {
            CurrentDecayMultiplier = Mathf.Max(0f, CurrentDecayMultiplier - step);
        }

        public void ResetDecay()
        {
            CurrentDecayMultiplier = 1f;
        }

        public void OnCheckResult(bool success, float usedRate)
        {
            OnResolved?.Invoke(this, success, usedRate);
        }

        /// <summary>
        /// 把關鍵字包上屬性色。找不到關鍵字就原樣回傳並提醒 —— 多半是打錯字。
        /// </summary>
        private string BuildDisplayText()
        {
            if (string.IsNullOrEmpty(keyword) || string.IsNullOrEmpty(baseText)) return baseText;

            AttributeChartData chart = Chart;
            if (chart == null) return baseText;

            int at = baseText.IndexOf(keyword, System.StringComparison.Ordinal);
            if (at < 0)
            {
                Debug.LogWarning(
                    $"[選項] 關鍵字「{keyword}」不在內文裡，這一句不會有顏色。內文：{baseText}", this);
                return baseText;
            }

            // 只換第一個出現的位置。整句都換的話，重複出現的字會整段爆色
            return baseText.Substring(0, at)
                 + chart.Colorize(keyword, attribute)
                 + baseText.Substring(at + keyword.Length);
        }

        /// <summary>
        /// C18③：把判定結果反映在**選項內文**上。
        /// 由 Stage 呼叫 —— 要接什麼字是內容，不是 UI 該決定的。
        /// </summary>
        public void AppendResultText(string suffix)
        {
            if (labelText == null || string.IsNullOrEmpty(suffix)) return;

            displayText = BuildDisplayText() + suffix;
            labelText.text = displayText;
        }

        // ==========================================
        // 判定結果的動效：霧凝聚 → 散去
        // ==========================================
        [Header("判定結果動效")]
        [Tooltip("凝聚（底色壓暗、換成結果文字）要多久")]
        [Min(0.05f)] public float flashInSeconds = 0.28f;

        [Tooltip("結果停留多久")]
        [Min(0f)] public float flashHoldSeconds = 0.7f;

        [Tooltip("散去（底色回原色、文字換回內文）要多久")]
        [Min(0.05f)] public float flashOutSeconds = 0.34f;

        [Tooltip("凝聚時底色變成什麼。預設接近黑")]
        public Color flashColor = new Color(0.06f, 0.05f, 0.06f, 1f);

        [Tooltip("結果文字的排版。\n" +
                 "{0} = 成功／失敗（**已經上好屬性色**）、{1} = 關鍵字（原色）。\n" +
                 "預設只顯示成功／失敗；要連關鍵字一起秀就填「{1}　{0}」")]
        public string resultFormat = "{0}";

        public string successWord = "成功";
        public string failureWord = "失敗";

        private Coroutine flashRoutine;

        /// <summary>整段動效要跑多久。Stage 要等它演完才收尾時看這個，不要自己抄秒數。</summary>
        public float TotalFlashSeconds => flashInSeconds + flashHoldSeconds + flashOutSeconds;

        [Header("結案（成功之後）")]
        [Tooltip("判定成功之後整格的透明度。留著看得見「→ 成功」，但明顯是已經處理過的")]
        [Range(0.1f, 1f)] public float spentAlpha = 0.45f;

        /// <summary>
        /// 這個選項已經成功過了，不再接受出牌。
        ///
        /// 【為什麼成功要結案】對話選項的語意是「問過就問過了」——
        /// 已經成功問出「你到底是什麼東西」，再問一次沒有意義。
        /// 而且不結案的話同一個選項可以一直打，獎勵得另外防重複（那是治標）。
        ///
        /// ⚠️ **失敗不結案** —— 失敗還能再試，那正是逐次衰減存在的理由（C18④）。
        /// </summary>
        public bool IsSpent { get; private set; }

        private CanvasGroup group;

        /// <summary>
        /// 結案。**用 CanvasGroup 擋掉滑鼠，不動 IProbabilityTarget 介面**。
        ///
        /// 「不再接受出牌」在這裡等於「不再收到滑鼠事件」：
        /// 拖曳的 RaycastAll 掃不到它、點擊也點不到，兩條出牌路徑一次擋掉。
        /// 若改成在 Core 的規則引擎裡加一個「可不可以打」的判斷，
        /// 介面與所有實作者都要跟著改，而寶箱那邊根本沒有這個概念。
        /// </summary>
        public void SetSpent(bool spent)
        {
            IsSpent = spent;

            if (group == null) group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();

            group.alpha = spent ? spentAlpha : 1f;
            group.blocksRaycasts = !spent;

            if (spent) HidePreview();
        }

        /// <summary>
        /// 判定完播一次「霧凝聚又散去」。
        ///
        /// 【為什麼是換同一顆 Text 的字，而不是另外開一個結果用的 Text】
        /// 多一個文字物件就要多維護一份位置、字級、換行與對齊，
        /// 而且兩份很容易在改版面時走鐘。換字串只會讓 TMP 重建一次網格，
        /// 一次動效期間發生兩次 —— 那個成本在這個規模下等於沒有。
        ///
        /// 文字用**淡出→換字→淡入**的方式接，硬切會看起來像閃爍。
        /// </summary>
        public void PlayResultFlash(bool success)
        {
            if (!isActiveAndEnabled) return;

            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine(success));
        }

        private System.Collections.IEnumerator FlashRoutine(bool success)
        {
            AttributeChartData chart = Chart;

            // 上色的是「成功／失敗」那個詞，不是關鍵字 ——
            // 關鍵字的顏色已經在選項內文裡出現過了，結果字再上一次只是重複；
            // 玩家這一刻要讀的是「成了沒有」，顏色是用來提醒「是靠哪一種思路成的」
            string word = success ? successWord : failureWord;
            string coloredWord = chart != null ? chart.Colorize(word, attribute) : word;

            string resultText = string.Format(resultFormat, coloredWord, keyword);

            // 底色的基準是**當下的顏色**而不是 unselectedTint ——
            // 這個選項可能正被選取或被瞄準著，動效結束要回到那個狀態，不是回到預設
            Color from = background != null ? background.color : Color.white;

            yield return Phase(flashInSeconds, from, flashColor, resultText);

            if (flashHoldSeconds > 0f) yield return new WaitForSecondsRealtime(flashHoldSeconds);

            yield return Phase(flashOutSeconds, flashColor, from, displayText);

            // ⚠️ 結案放在動效**之後**。先變暗的話，那一下閃光是打在已經半透明的格子上，
            //    整個效果會弱掉 —— 玩家會覺得「好像有東西閃過去」而不是「這一句成了」
            if (success) SetSpent(true);

            flashRoutine = null;
        }

        /// <summary>底色從 a 漸變到 b，文字在中點淡出→換成 newText→淡入。</summary>
        private System.Collections.IEnumerator Phase(float seconds, Color a, Color b, string newText)
        {
            float t = 0f;
            bool swapped = false;

            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / seconds;
                float k = Mathf.Clamp01(t);

                if (background != null) background.color = Color.Lerp(a, b, k);

                if (labelText != null)
                {
                    // 0→0.5 淡出、0.5→1 淡入，中點換字
                    float alpha = k < 0.5f ? 1f - k * 2f : (k - 0.5f) * 2f;
                    labelText.alpha = alpha;

                    if (!swapped && k >= 0.5f)
                    {
                        labelText.text = newText;
                        swapped = true;
                    }
                }

                yield return null;
            }

            if (background != null) background.color = b;
            if (labelText != null)
            {
                labelText.text = newText;
                labelText.alpha = 1f;
            }
        }

        public void ShowPreview(float rate, Effectiveness eff)
        {
            // PlainChoice 不判定，顯示機率只會誤導；已結案的也不該再報機率
            if (mode != Mode.ProbabilityTarget || previewLabel == null || IsSpent) return;

            previewLabel.gameObject.SetActive(true);

            if (eff == Effectiveness.None)
            {
                previewLabel.text = immuneText;
                previewLabel.color = immuneColor;
            }
            else
            {
                previewLabel.text = Mathf.RoundToInt(rate * 100f).ToString();
                previewLabel.color = normalColor;
            }
        }

        public void HidePreview()
        {
            if (previewLabel != null) previewLabel.gameObject.SetActive(false);
        }

        public void OnAttemptsExhausted()
        {
            // 選項沒有「結案」的概念 —— 手牌用盡由對話 Stage 統一收尾
        }

        [Header("被瞄準時")]
        [Tooltip("卡片瞄準這個選項時底色乘上的顏色。預設稍微變暗。\n" +
                 "不要太搶眼 —— 選項上還有內文與機率數字要讀")]
        public Color targetedTint = new Color(0.78f, 0.78f, 0.78f, 1f);

        public void SetTargeted(bool targeted)
        {
            if (background == null || IsSpent) return;

            // 底色的基準是 unselectedTint（Bind 時套用的那個），不是當下的顏色 ——
            // 否則連續瞄準／取消會一次比一次暗，越疊越黑
            Color b = unselectedTint;

            background.color = targeted
                ? new Color(b.r * targetedTint.r, b.g * targetedTint.g, b.b * targetedTint.b, b.a)
                : b;
        }

        // ==========================================
        // 兩段式出牌：手上有卡時滑過來也要有回饋
        // ==========================================
        public void OnPointerEnter(PointerEventData eventData)
        {
            DialogueEncounterController e = DialogueEncounterController.Instance;
            if (e != null && e.IsActive && e.HasArmedCard) e.SetAimed(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            DialogueEncounterController e = DialogueEncounterController.Instance;
            if (e != null && ReferenceEquals(e.AimedTarget, this)) e.ClearAimed();
        }
    }
}
