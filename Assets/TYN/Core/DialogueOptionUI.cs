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

        private string baseText = "";
        private ExploreAttribute attribute = ExploreAttribute.None;
        private Image background;

        private void Awake()
        {
            background = GetComponent<Image>();
        }

        public void Bind(int index, string text, ExploreAttribute attr, Mode optionMode)
        {
            Index = index;
            baseText = text ?? "";
            attribute = attr;
            mode = optionMode;

            CurrentDecayMultiplier = 1f;

            if (labelText != null) labelText.text = baseText;

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
        /// C18③：把判定結果反映在**選項內文**上。
        /// 由 Stage 呼叫 —— 要接什麼字是內容，不是 UI 該決定的。
        /// </summary>
        public void AppendResultText(string suffix)
        {
            if (labelText == null || string.IsNullOrEmpty(suffix)) return;
            labelText.text = baseText + suffix;
        }

        public void ShowPreview(float rate, Effectiveness eff)
        {
            // PlainChoice 不判定，顯示機率只會誤導
            if (mode != Mode.ProbabilityTarget || previewLabel == null) return;

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
            if (background == null) return;

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
