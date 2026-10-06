using System;
using UnityEngine;
using UnityEngine.UI;
using EldritchMile.Core;

namespace EldritchMile.UI
{
    /// <summary>
    /// 右下角的 EXIT。**探索、商店、對話節點共用這一顆**，住在 `Canvas_HUD`。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【為什麼改成共用】（2026-09-15 EXIT 改版）
    ///   · 三個環節要同一個設計、同一個位置（右下角）
    ///   · 它要跟 HP／SAN 顯示**輪流出現** —— 那一塊也在右下角、也在場景常駐的 HUD 上。
    ///     各 Stage 自己一顆的話，每一顆都要各自知道 HUD 的狀態
    ///   · 它要知道對話框開著沒有（開著就往上推），而對話框也是場景常駐的
    ///
    /// 【Stage 怎麼用】進場 `Show(按下去要做的事)`，離場 `Hide()`。就這樣。
    ///
    /// 【它自己管的三件事】
    ///   1. 淡入淡出（CanvasGroup），Stage 沒要求時完全透明、點不到
    ///   2. 對話框開著 → 推到 `aboveDialogueY`；關著 → 回到 `bottomY`
    ///   3. HP／SAN 顯示要出來時先淡出，等那邊完全消失才淡回來（見 VitalsHudUI）
    ///
    /// 外觀（兩張圖替換、滑出）沿用商店那一套：SlideOutTab ＋ HoverCrossfadeImage。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class SharedExitUI : MonoBehaviour
    {
        public static SharedExitUI Instance { get; private set; }

        [Header("元件")]
        [Tooltip("真正被點的那顆 Button（商店那一套的 Visual）")]
        public Button button;

        [Tooltip("滑出元件。點下去時會強制收回 —— 確認面板蓋上來之後收不到 OnPointerExit")]
        public SlideOutTab slideTab;

        [Header("位置（本物件的 anchoredPosition.y，錨在右下角）")]
        [Tooltip("對話框沒開時的高度")]
        public float bottomY = 40f;

        [Tooltip("對話框開著時的高度。要**高過對話框的上緣**，整顆才看得完整")]
        public float aboveDialogueY = 400f;

        [Tooltip("上下移動大約多久到位（秒）")]
        [Min(0.01f)] public float moveSmoothSeconds = 0.15f;

        [Header("淡入淡出")]
        [Tooltip("完整淡入或淡出要多久（秒）")]
        [Min(0.01f)] public float fadeSeconds = 0.25f;

        [Header("確認面板（「確定要離開嗎？」）")]
        [Tooltip("整塊面板（含擋住後面點擊的全螢幕底）。\n\n" +
                 "⚠️ **要跟 EXIT 在同一個畫布（Canvas_HUD），而且是它的兄弟、不是子物件**：\n" +
                 "　· 放在 Stage 的畫布（Canvas_Stage，100）裡會被對話框（101）、寶箱大圖蓋住 ——\n" +
                 "　　2026-09-15 回報「和寶箱打牌時點 EXIT，詢問畫面顯示不出來」就是這個\n" +
                 "　· 放在 EXIT 底下的話，EXIT 淡出時會把面板一起淡掉、一起關掉點擊")]
        public GameObject confirmPanel;

        public TMPro.TMP_Text confirmQuestion;
        public Button confirmYes;
        public Button confirmNo;

        [Tooltip("沒指定問句時用這一句")]
        public string defaultQuestion = "確定要離開嗎？";

        /// <summary>確認面板開著</summary>
        public bool IsConfirming { get { return confirmPanel != null && confirmPanel.activeSelf; } }

        private Action onConfirm;

        /// <summary>目前有 Stage 要它出現</summary>
        public bool IsRequested { get; private set; }

        /// <summary>完全透明了。HP／SAN 顯示等這個才淡入</summary>
        public bool IsFullyHidden { get { return group == null || group.alpha <= 0.01f; } }

        private CanvasGroup group;
        private RectTransform rt;
        private Action onClick;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[EXIT] 場上有兩顆 SharedExitUI，這一顆不作用", this);
                enabled = false;
                return;
            }
            Instance = this;

            group = GetComponent<CanvasGroup>();
            rt = (RectTransform)transform;

            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
                button.onClick.AddListener(HandleClick);
            }

            if (confirmYes != null) { confirmYes.onClick.RemoveListener(HandleYes); confirmYes.onClick.AddListener(HandleYes); }
            if (confirmNo != null) { confirmNo.onClick.RemoveListener(HandleNo); confirmNo.onClick.AddListener(HandleNo); }
            if (confirmPanel != null) confirmPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
            if (confirmYes != null) confirmYes.onClick.RemoveListener(HandleYes);
            if (confirmNo != null) confirmNo.onClick.RemoveListener(HandleNo);
            if (Instance == this) Instance = null;
        }

        // ==========================================
        // Stage 用的
        // ==========================================

        /// <summary>進場時呼叫。按下去會執行 <paramref name="click"/>。</summary>
        public void Show(Action click)
        {
            // 從完全隱藏開始出現時，直接跳到正確高度 ——
            // 不然會看到它從上一站的位置飄過來
            if (IsFullyHidden) SnapY();

            onClick = click;
            IsRequested = true;
        }

        /// <summary>
        /// 進場時呼叫。點 EXIT 先跳「確定要離開嗎？」，按「是」才執行 <paramref name="onYes"/>。
        /// 探索、商店、對話節點都走這一支 —— 誤觸 EXIT 就直接離開的代價太大。
        /// </summary>
        public void ShowWithConfirm(Action onYes, string question = null)
        {
            Show(delegate { Confirm(onYes, question); });
        }

        /// <summary>離場時呼叫。</summary>
        public void Hide()
        {
            onClick = null;
            IsRequested = false;
            Retract();
            CloseConfirm();
        }

        /// <summary>跳出確認面板。場上沒有面板的話直接執行（少一個欄位比讓玩家出不去好）。</summary>
        public void Confirm(Action onYes, string question = null)
        {
            if (confirmPanel == null)
            {
                if (onYes != null) onYes();
                return;
            }

            onConfirm = onYes;
            if (confirmQuestion != null) confirmQuestion.text = string.IsNullOrEmpty(question) ? defaultQuestion : question;

            confirmPanel.SetActive(true);
            confirmPanel.transform.SetAsLastSibling();   // 蓋在 HUD 其他東西（含 EXIT）之上
            Retract();
        }

        public void CloseConfirm()
        {
            onConfirm = null;
            if (confirmPanel != null) confirmPanel.SetActive(false);
        }

        private void HandleYes()
        {
            Action a = onConfirm;
            CloseConfirm();
            if (a != null) a();
        }

        private void HandleNo()
        {
            CloseConfirm();
        }

        /// <summary>把滑出的標籤收回去（不影響顯示）。確認面板跳出來時用。</summary>
        public void Retract()
        {
            if (slideTab != null) slideTab.SetShown(false);
        }

        // ==========================================
        private void HandleClick()
        {
            // 淡出中（例如 HP／SAN 正在顯示）不收點擊 —— 半透明的鈕被點到會很突然
            if (!IsRequested || group.alpha < 0.5f) return;

            Retract();
            Action a = onClick;
            if (a != null) a();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            // ── 淡入淡出 ──
            bool hudBusy = VitalsHudUI.Active != null && VitalsHudUI.Active.WantsScreen;
            float want = IsRequested && !hudBusy ? 1f : 0f;
            group.alpha = Mathf.MoveTowards(group.alpha, want, dt / fadeSeconds);

            // 確認面板開著時 EXIT 不收點擊 —— 面板本來就蓋著，這是保險
            bool clickable = want > 0.5f && group.alpha > 0.5f && !IsConfirming;
            group.blocksRaycasts = clickable;
            group.interactable = clickable;

            // 淡出時標籤還伸在外面的話收回來 —— 淡回來時才不會是伸出去的樣子
            if (!clickable && slideTab != null && slideTab.IsShown) slideTab.SetShown(false);

            // ── 高度 ──
            float targetY = TargetY();
            Vector2 p = rt.anchoredPosition;
            if (Mathf.Abs(p.y - targetY) < 0.5f) return;

            float k = 1f - Mathf.Exp(-dt / moveSmoothSeconds);
            p.y = Mathf.Lerp(p.y, targetY, k);
            rt.anchoredPosition = p;
        }

        private float TargetY()
        {
            bool boxOpen = PopupService.Instance != null && PopupService.Instance.IsAnyOpen;
            return boxOpen ? aboveDialogueY : bottomY;
        }

        private void SnapY()
        {
            if (rt == null) rt = (RectTransform)transform;
            Vector2 p = rt.anchoredPosition;
            p.y = TargetY();
            rt.anchoredPosition = p;
        }
    }
}
