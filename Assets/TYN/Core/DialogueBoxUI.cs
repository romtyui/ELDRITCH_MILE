using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EldritchMile.Core
{
    /// <summary>
    /// 全專案共用的對話框。同時負責兩種內容：
    ///
    ///   1. **角色說話** ── 有名字、立繪
    ///   2. **系統提示** ── 沒有說話者，用統一的公版格式（獲得道具、探索完成、判定結果…）
    ///
    /// 【為什麼要共用】原本探索有自己的 Popup_Panel / Loot_Panel，對話又有另一套 UI。
    /// 同一款遊戲裡兩種外觀的文字視窗會很割裂，而且每多一個 Stage 就要再做一份面板。
    /// 收斂成一個對話框之後，「獲得道具」只是系統提示的一種格式。
    /// </summary>
    public class DialogueBoxUI : MonoBehaviour
    {
        [Header("元件")]
        [Tooltip("整個對話框的開關對象（通常是 dialogbox）")]
        public GameObject root;

        [Tooltip("正文（text_box 裡的 TMP）")]
        public TextMeshProUGUI bodyText;

        [Tooltip("名字框物件。系統提示時會隱藏")]
        public GameObject nameBox;

        [Tooltip("名字文字")]
        public TextMeshProUGUI nameText;

        [Tooltip("立繪。系統提示時會隱藏。可留空")]
        public GameObject portraitRoot;
        public Image portraitImage;

        [Tooltip("背景壓黑。可留空")]
        public GameObject dimmer;

        [Tooltip("點擊推進的按鈕。建議是蓋住整個對話框的透明 Button")]
        public Button advanceButton;

        [Header("打字機")]
        [Tooltip("每秒顯示幾個字。設 0 = 不用打字機，直接全部顯示")]
        public float charsPerSecond = 40f;

        [Header("系統提示公版")]
        [Tooltip("系統提示時名字框顯示什麼。留空則隱藏名字框")]
        public string systemSpeakerName = "";

        [Tooltip("系統提示的文字顏色")]
        public Color systemTextColor = new Color(0.85f, 0.85f, 0.75f);

        [Tooltip("角色說話的文字顏色")]
        public Color speechTextColor = Color.white;

        [Header("公版格式（{0} 會被替換）")]
        [Tooltip("獲得單一道具")]
        public string itemGainedFormat = "獲得了 {0}。";

        [Tooltip("獲得多個道具時的開頭")]
        public string itemsGainedHeader = "獲得了：";

        [Tooltip("多個道具時每一行的格式")]
        public string itemLineFormat = "　· {0}";

        [Tooltip("容器是空的")]
        public string emptyContainerFormat = "{0} 裡面空空如也。";

        [Tooltip("開啟容器但有東西時的開頭")]
        public string containerOpenedFormat = "打開了 {0}。";

        public bool IsShowing { get; private set; }
        public bool IsTyping { get; private set; }

        /// 文字播完、玩家點擊推進時觸發
        public event System.Action OnAdvanced;

        private Coroutine typing;

        private void Awake()
        {
            if (advanceButton != null) advanceButton.onClick.AddListener(Advance);
            HideImmediate();
        }

        // ==========================================
        // 對外 API
        // ==========================================

        /// <summary>系統提示：沒有說話者，用公版樣式。</summary>
        public void ShowSystem(string message)
        {
            Open();

            bool hasName = !string.IsNullOrEmpty(systemSpeakerName);
            if (nameBox != null) nameBox.SetActive(hasName);
            if (hasName && nameText != null) nameText.text = systemSpeakerName;

            if (portraitRoot != null) portraitRoot.SetActive(false);

            if (bodyText != null) bodyText.color = systemTextColor;
            SetBody(message);
        }

        /// <summary>角色說話：有名字與立繪。</summary>
        public void ShowSpeech(string speaker, string message, Sprite portrait = null)
        {
            Open();

            if (nameBox != null) nameBox.SetActive(!string.IsNullOrEmpty(speaker));
            if (nameText != null) nameText.text = speaker;

            if (portraitRoot != null) portraitRoot.SetActive(portrait != null);
            if (portraitImage != null && portrait != null) portraitImage.sprite = portrait;

            if (bodyText != null) bodyText.color = speechTextColor;
            SetBody(message);
        }

        // ==========================================
        // 系統提示公版
        // ==========================================

        /// <summary>獲得道具。單一與多個用不同格式。</summary>
        public void ShowItemsGained(IReadOnlyList<string> items)
        {
            if (items == null || items.Count == 0) return;

            if (items.Count == 1)
            {
                ShowSystem(string.Format(itemGainedFormat, items[0]));
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(itemsGainedHeader);
            for (int i = 0; i < items.Count; i++)
            {
                sb.AppendLine(string.Format(itemLineFormat, items[i]));
            }
            ShowSystem(sb.ToString().TrimEnd());
        }

        /// <summary>開啟容器。空的與有東西用不同格式。</summary>
        public void ShowContainerOpened(string containerName, IReadOnlyList<string> items)
        {
            if (items == null || items.Count == 0)
            {
                ShowSystem(string.Format(emptyContainerFormat, containerName));
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(string.Format(containerOpenedFormat, containerName));
            for (int i = 0; i < items.Count; i++)
            {
                sb.AppendLine(string.Format(itemLineFormat, items[i]));
            }
            ShowSystem(sb.ToString().TrimEnd());
        }

        // ==========================================
        // 顯示控制
        // ==========================================

        public void Hide()
        {
            StopTyping();
            IsShowing = false;

            if (root != null) root.SetActive(false);
            if (dimmer != null) dimmer.SetActive(false);
        }

        public void HideImmediate() => Hide();

        /// <summary>點擊推進：文字還在跑就跳完，跑完了就關閉並通知。</summary>
        public void Advance()
        {
            if (!IsShowing) return;

            if (IsTyping)
            {
                SkipTyping();
                return;
            }

            Hide();
            OnAdvanced?.Invoke();
        }

        private void Open()
        {
            IsShowing = true;
            if (root != null) root.SetActive(true);
            if (dimmer != null) dimmer.SetActive(true);
        }

        private void SetBody(string message)
        {
            if (bodyText == null)
            {
                Debug.LogWarning($"[對話框] 沒有指定 Body Text，內容只能印出來：{message}");
                return;
            }

            StopTyping();

            if (charsPerSecond <= 0f)
            {
                bodyText.text = message;
                bodyText.maxVisibleCharacters = int.MaxValue;
                IsTyping = false;
                return;
            }

            typing = StartCoroutine(TypeRoutine(message));
        }

        private IEnumerator TypeRoutine(string message)
        {
            IsTyping = true;

            bodyText.text = message;
            bodyText.ForceMeshUpdate();

            int total = bodyText.textInfo.characterCount;
            bodyText.maxVisibleCharacters = 0;

            float shown = 0f;
            while (shown < total)
            {
                shown += charsPerSecond * Time.unscaledDeltaTime;
                bodyText.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(shown));
                yield return null;
            }

            bodyText.maxVisibleCharacters = total;
            IsTyping = false;
            typing = null;
        }

        private void SkipTyping()
        {
            StopTyping();
            if (bodyText != null) bodyText.maxVisibleCharacters = int.MaxValue;
            IsTyping = false;
        }

        private void StopTyping()
        {
            if (typing != null)
            {
                StopCoroutine(typing);
                typing = null;
            }
            IsTyping = false;
        }
    }
}
