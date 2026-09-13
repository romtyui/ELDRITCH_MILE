using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EldritchMile.UI
{
    /// <summary>
    /// 一句會自己消失的提示。**不用點、不擋滑鼠、蓋在所有畫面之上**（住在 `Canvas_Tooltip`）。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【為什麼不用對話框】（2026-09-15 試玩回報）
    /// 快捷欄的提示（「現在不能使用道具」「吃了什麼」）以前走 `PopupService.ShowInstant`，
    /// 那是**對話框** —— 要點才會關，而且：
    ///   · 在地圖上：對話框的畫布（101）在地圖（300）底下，**看不到也點不到**
    ///   · 在打牌中：對話框是 HoldOpen，點了只會推進文字，**關不掉**
    /// 這種「附帶一提」的訊息本來就不該佔用玩家正在讀的那個框。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ToastUI : MonoBehaviour
    {
        public static ToastUI Instance { get; private set; }

        [Tooltip("顯示文字的元件")]
        public TMP_Text text;

        [Tooltip("淡入／淡出各多久（秒）")]
        [Min(0.01f)] public float fadeSeconds = 0.2f;

        [Tooltip("完全顯示之後停多久（秒）")]
        [Min(0f)] public float holdSeconds = 1.6f;

        [Tooltip("每多一個字多停幾秒 —— 長句要讀比較久")]
        [Min(0f)] public float perCharSeconds = 0.03f;

        private CanvasGroup group;
        private float hold;
        private float timer;
        private int phase;   // 0 隱藏、1 淡入、2 停留、3 淡出

        private void Awake()
        {
            if (Instance != null && Instance != this) { enabled = false; return; }
            Instance = this;

            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>有 Toast 就顯示並回傳 true；場上沒有的話回傳 false，由呼叫方退回別的做法。</summary>
        public static bool TryShow(string message)
        {
            if (Instance == null || !Instance.isActiveAndEnabled || string.IsNullOrEmpty(message)) return false;
            Instance.Show(message);
            return true;
        }

        public void Show(string message)
        {
            if (text != null) text.text = message;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);

            hold = holdSeconds + perCharSeconds * (message != null ? message.Length : 0);
            timer = 0f;

            // 正在顯示的話不必重新淡入，只換字、重新計時
            phase = group.alpha > 0.99f ? 2 : 1;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            switch (phase)
            {
                case 1:
                    group.alpha = Mathf.MoveTowards(group.alpha, 1f, dt / fadeSeconds);
                    if (group.alpha >= 1f) { phase = 2; timer = 0f; }
                    break;
                case 2:
                    timer += dt;
                    if (timer >= hold) phase = 3;
                    break;
                case 3:
                    group.alpha = Mathf.MoveTowards(group.alpha, 0f, dt / fadeSeconds);
                    if (group.alpha <= 0f) phase = 0;
                    break;
            }
        }
    }
}
