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

        [Tooltip("選項框（option_box）。顯示純文字訊息時會自動隱藏 —— " +
                 "系統提示與一般對白都沒有選項，留著會擋住畫面也會誤導玩家")]
        public GameObject optionBox;

        [Tooltip("背景壓黑。可留空")]
        public GameObject dimmer;

        [Tooltip("點擊推進的按鈕。建議是蓋住整個對話框的透明 Button")]
        public Button advanceButton;

        [Header("打牌環節的對象大圖")]
        [Tooltip("生成在 Portrait Root 底下的對象化身 prefab。\n" +
                 "需含 Image（勾 Preserve Aspect）與機率標籤，掛 EncounterTargetView")]
        public EncounterTargetView targetViewPrefab;

        /// <summary>
        /// 打牌期間為 true —— 點擊只推進文字，**不會關閉對話框**。
        /// 因為對象大圖就在框裡，關掉的話玩家就沒有東西可以出牌了。
        /// </summary>
        public bool HoldOpen { get; set; }

        /// <summary>
        /// 選項正在顯示中 —— `Open()` 不可以把選項框關掉。
        ///
        /// 【為什麼需要】`Open()` 預設每次都關閉選項框，好讓純文字訊息不會殘留上一則的選項。
        /// 但**打牌時每出一張牌都會即時替換正文**（C18③），那也會走 `Open()` ——
        /// 結果選項在玩家出第一張牌的瞬間就整排消失，但環節還在等他選。
        ///
        /// 由 `DialogueOptionsPanel` 在顯示／收起選項時設定。
        /// </summary>
        public bool HoldOptions { get; set; }

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
        private Coroutine autoAdvance;

        /// <summary>
        /// 排定自動推進：文字打完後等 seconds 秒自動 Advance()。
        /// seconds <= 0 表示不自動，維持等玩家點擊。
        ///
        /// 用於「結束打牌 → 結算 → 獲得道具」這種連續播報 ——
        /// 玩家已經按了結束，不該還要再點好幾下才看得完後續。
        /// </summary>
        public void ScheduleAutoAdvance(float seconds)
        {
            CancelAutoAdvance();
            if (seconds <= 0f || !IsShowing) return;

            autoAdvance = StartCoroutine(AutoAdvanceRoutine(seconds));
        }

        private IEnumerator AutoAdvanceRoutine(float seconds)
        {
            while (IsTyping) yield return null;              // 先讓字打完
            yield return new WaitForSecondsRealtime(seconds); // 再給讀的時間

            autoAdvance = null;
            Advance();
        }

        private void CancelAutoAdvance()
        {
            if (autoAdvance != null)
            {
                StopCoroutine(autoAdvance);
                autoAdvance = null;
            }
        }

        private void Awake()
        {
            if (advanceButton != null) advanceButton.onClick.AddListener(Advance);
            HideImmediate();
        }

        // ==========================================
        // 對外 API
        // ==========================================

        /// <summary>
        /// 系統提示：沒有說話者，用公版樣式。
        ///
        /// closeUp 可選 —— 借立繪位置顯示互動對象的特寫圖（開箱、檢查物件時）。
        /// 這是冒險遊戲的慣例：把注意力從整個場景拉到「你正在處理的這個東西」。
        /// </summary>
        public void ShowSystem(string message, Sprite closeUp = null)
        {
            Open();

            bool hasName = !string.IsNullOrEmpty(systemSpeakerName);
            if (nameBox != null) nameBox.SetActive(hasName);
            if (hasName && nameText != null) nameText.text = systemSpeakerName;

            // 打牌期間立繪位置放的是對象大圖（SpawnTargetView 生成的），
            // 後續訊息不可以把它關掉，否則玩家就沒有東西可以出牌了。
            if (spawnedTargets.Count == 0)
            {
                if (portraitRoot != null) portraitRoot.SetActive(closeUp != null);
                if (portraitImage != null && closeUp != null) portraitImage.sprite = closeUp;
            }

            if (bodyText != null) bodyText.color = systemTextColor;
            SetBody(message);
        }

        /// <summary>角色說話：有名字與立繪。</summary>
        public void ShowSpeech(string speaker, string message, Sprite portrait = null)
        {
            Open();

            if (nameBox != null) nameBox.SetActive(!string.IsNullOrEmpty(speaker));
            if (nameText != null) nameText.text = speaker;

            if (spawnedTargets.Count == 0)
            {
                if (portraitRoot != null) portraitRoot.SetActive(portrait != null);
                if (portraitImage != null && portrait != null) portraitImage.sprite = portrait;
            }

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
            CancelAutoAdvance();
            IsShowing = false;
            HoldOpen = false;
            ClearTargetViews();

            if (root != null) root.SetActive(false);
            if (dimmer != null) dimmer.SetActive(false);
        }

        public void HideImmediate() => Hide();

        /// <summary>
        /// 點擊推進：文字還在跑就跳完，跑完了就通知（由 PopupService 決定播下一則或收掉）。
        ///
        /// HoldOpen 期間不關閉 —— 打牌時對象大圖就在框裡，關掉玩家就沒東西可打了。
        /// </summary>
        public void Advance()
        {
            if (!IsShowing) return;

            if (IsTyping)
            {
                SkipTyping();
                return;
            }

            if (!HoldOpen) Hide();
            OnAdvanced?.Invoke();
        }

        /// <summary>
        /// 強制推進一格：文字還在跑就先補完，然後直接進到下一句（或關閉）。
        ///
        /// 與 Advance() 的差別：Advance() 在打字中只會「跳完文字」，
        /// 得再點一次才會推進 —— 那是給玩家點擊用的正確行為。
        /// 但「按結束」是一個明確的指令，玩家不會預期還要再點一下，
        /// 所以這裡一次做完。
        /// </summary>
        public void AdvanceImmediate()
        {
            if (!IsShowing) return;

            if (IsTyping) SkipTyping();
            Advance();
        }

        // ==========================================
        // 打牌環節的對象大圖
        // ==========================================

        /// <summary>
        /// 在立繪位置生成一個對象化身，回傳它。卡片會打在這上面。
        ///
        /// 【為什麼用生成而不是直接換 character 的圖】立繪 Image 的尺寸與比例是為人物
        /// 調好的，塞寶箱之類的圖會被拉伸。每次生成一個帶 Preserve Aspect 的新 Image，
        /// 什麼比例的圖都不會變形。
        /// </summary>
        public EncounterTargetView SpawnTargetView(IProbabilityTarget source, Sprite closeUp)
        {
            if (targetViewPrefab == null || portraitRoot == null)
            {
                Debug.LogWarning("[對話框] 沒有指定 Target View Prefab 或 Portrait Root，無法顯示對象大圖");
                return null;
            }

            ClearTargetViews();

            portraitRoot.SetActive(true);

            EncounterTargetView view = Instantiate(targetViewPrefab, portraitRoot.transform);
            view.Bind(source, closeUp);
            spawnedTargets.Add(view);

            return view;
        }

        public void ClearTargetViews()
        {
            for (int i = 0; i < spawnedTargets.Count; i++)
            {
                if (spawnedTargets[i] != null) Destroy(spawnedTargets[i].gameObject);
            }
            spawnedTargets.Clear();
        }

        private readonly List<EncounterTargetView> spawnedTargets = new List<EncounterTargetView>();

        /// <summary>
        /// 開啟對話框並回到「純文字」狀態。
        ///
        /// 選項框預設關閉 —— 只有明確要顯示選項時才由 Phase 4c 的打牌 UI 打開。
        /// 這樣不管上一則訊息留下什麼狀態，每次開啟都是乾淨的。
        /// </summary>
        private void Open()
        {
            IsShowing = true;
            if (root != null) root.SetActive(true);
            if (dimmer != null) dimmer.SetActive(true);

            // 選項顯示中就不能關 —— 打牌時每出一張牌都會即時替換正文並走到這裡，
            // 無條件關閉的話選項會在玩家出第一張牌的瞬間整排消失
            if (optionBox != null && !HoldOptions) optionBox.SetActive(false);
        }

        /// <summary>Phase 4c：要顯示選項時由打牌 UI 呼叫。</summary>
        public void SetOptionsVisible(bool visible)
        {
            if (optionBox != null) optionBox.SetActive(visible);
        }

        private void SetBody(string message)
        {
            // 換內容就取消上一則排定的自動推進，否則新句子會被舊計時器提早跳掉
            CancelAutoAdvance();

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
