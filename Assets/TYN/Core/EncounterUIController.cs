using System.Collections;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 打牌環節的附屬 UI 總管。本輪只負責 C12「在試一次？」。
    ///
    /// 【C12 在解什麼】在這之前，判定失敗後畫面只換一句「沒能撬開」，然後什麼都不發生 ——
    /// 要不要再試，靠玩家自己想到「我可以再拖一張」。那是隱性設計：
    /// 失敗這個結果沒有出口，玩家不知道自己還有選擇，也不知道再試要付什麼代價。
    /// 改成明確詢問之後，失敗變成一個分岔點，重試是玩家**選**的，不是預設繼續。
    ///
    /// 【為什麼成功不問】C18⑦ 蓄意失敗是合法策略，成功之後玩家可能還想繼續打
    /// （例如刻意把某個目標打壞）。成功時跳詢問等於暗示「該收手了」，會壓掉那個玩法。
    /// 手牌用盡時也不問 —— 那時候沒得選，問了只是多一下點擊。
    ///
    /// 【為什麼放在 Core 而不是 Explore】Phase 6 的對話與商店同樣會用打牌環節，
    /// 這個詢問對它們是一樣的。所以它跟 DialogueEncounterController 一起住在 Core，
    /// 而且**刻意不引用 ExploreHandUI** —— Core 不該反過來認識 Explore。
    /// 擋輸入改用通用的 CanvasGroup 欄位（見 Hand Interaction Group）。
    ///
    /// 【擺放位置】常駐 EventScene，與 DialogueEncounterController、手牌區同一組。
    /// Stage prefab 無法在 Inspector 引用場景物件，所以一樣走單例。
    /// </summary>
    public class EncounterUIController : MonoBehaviour
    {
        public static EncounterUIController Instance { get; private set; }

        /// <summary>
        /// 什麼時候跳「在試一次？」。
        ///
        /// 【為什麼預設 Never】每次失敗都跳確認，實測下來是錯的設計：
        ///   · 它傳達的資訊（「你還可以再試」）只有第一次是新的，之後就是純摩擦
        ///   · 出口一直都在 —— 「結束」按鈕整個環節都在畫面上，不是沒有出路
        ///   · 「再試要付什麼代價」hover 的預覽數字早就答了（機率會下降）
        ///   · C18 的核心是連續嘗試，5 張手牌失敗 4 次等於被切成四段
        ///
        /// 改用 <see cref="DialogueEncounterController.attemptSuffixFormat"/>：
        /// 回合感寫進判定結果文字（「這是你嘗試的第 3 次。」），零打斷。
        ///
        /// OnFailure 留著是為了能在 Play Mode 直接比較兩種手感，不是建議值。
        /// </summary>
        public enum AskMode
        {
            /// 不跳確認。回合感交給判定結果文字（建議）
            Never,

            /// 每次判定失敗且還有手牌時跳確認
            OnFailure,
        }

        [Header("繫結")]
        [Tooltip("留空會自動抓 DialogueEncounterController.Instance")]
        public DialogueEncounterController encounter;

        [Header("C12「在試一次？」")]
        [Tooltip("什麼時候跳確認。\n\n" +
                 "Never（建議）＝ 不跳，回合感改由判定結果文字表現\n" +
                 "（DialogueEncounterController 的 Attempt Suffix Format）。\n\n" +
                 "OnFailure ＝ 每次失敗都跳。留著是為了能直接比較兩種手感。\n\n" +
                 "【還沒有的第三種】OnHandExhausted（手牌用盡時問要不要逆轉）——\n" +
                 "尚未實作，因為它現在會是個假選擇：Decay Scaled To Hand Size 勾選時，\n" +
                 "出完最後一張的衰減倍率正好是 0，ProbabilityCheck 會把它短路成必定失敗。\n" +
                 "要做這個模式，得先決定「逆轉」如何處理衰減，以及它要付什麼代價。")]
        public AskMode askMode = AskMode.Never;
        [Tooltip("判定失敗後跳出的詢問面板。\n" +
                 "掛 UIPanel(Dialog) 就交給 UIDirector 堆疊管理；只掛 FadePanel 則淡入；" +
                 "都沒有就直接 SetActive。\n" +
                 "留空 = 不啟用 C12，維持舊行為（玩家自己決定要不要再出牌）")]
        public GameObject retryAskPanel;

        [Tooltip("詢問面板上的文字。重試詢問會把代價寫進去（「花費 5 點…」），所以需要能改內容。\n" +
                 "留空則面板文字維持你在 Inspector 打好的固定內容")]
        public TMPro.TextMeshProUGUI retryAskLabel;

        [Tooltip("詢問期間要停掉互動的 CanvasGroup —— 拖這個場景裡的 EncounterUI（手牌區的根）。\n\n" +
                 "⚠️ 這不是裝飾。詢問跳出來時手牌若還能拖，玩家可以一邊被問「要再試嗎」" +
                 "一邊把牌打出去，那張牌等於繞過了整個詢問。\n\n" +
                 "用 CanvasGroup 而不是關掉 Image 或 SetActive：停用的 Graphic 收不到 raycast " +
                 "且不會報錯，是這個專案踩過的坑；SetActive 則會讓手牌整排消失，玩家看不到自己還剩幾張。")]
        public CanvasGroup handInteractionGroup;

        [Tooltip("失敗後隔多久才跳詢問（秒）。\n" +
                 "會先等對話框的打字機把失敗那句打完，再等這段時間 —— " +
                 "字還在跑就跳確認的話，玩家根本沒讀到自己為什麼失敗。\n" +
                 "設 0 則打完就立刻跳")]
        [Min(0f)] public float askDelay = 0.35f;

        /// 詢問面板正開著。此時手牌互動是停掉的。
        public bool IsAsking { get; private set; }

        private readonly PanelToggle retryToggle = new PanelToggle();
        private Coroutine askRoutine;
        private bool warnedNoPanel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // 訂閱放 Start：本元件通常掛在 [SYSTEM] 上（一直是 active），OnEnable 也會過，
            // 但專案裡其他 UI 是因為「預設 inactive 的物件不執行 OnEnable」才統一改用 Start，
            // 這裡跟著同一個慣例，之後把它移進預設隱藏的 EncounterUI 底下也不會突然失效。
            Subscribe();

            // 面板一開始必須是關的。上一次 Play 停在詢問狀態、或有人在編輯器裡把它留成開啟，
            // 都會讓玩家一進場就看到一個沒有上下文的確認框。
            HideAskImmediate();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (Instance == this) Instance = null;
        }

        private void Subscribe()
        {
            if (encounter == null) encounter = DialogueEncounterController.Instance;
            if (encounter == null) return;

            encounter.OnCardResolved -= HandleCardResolved;
            encounter.OnEncounterEnded -= HandleEncounterEnded;

            encounter.OnCardResolved += HandleCardResolved;
            encounter.OnEncounterEnded += HandleEncounterEnded;
        }

        private void Unsubscribe()
        {
            if (encounter == null) return;
            encounter.OnCardResolved -= HandleCardResolved;
            encounter.OnEncounterEnded -= HandleEncounterEnded;
        }

        // ==========================================
        // C12：失敗 → 詢問
        // ==========================================
        private void HandleCardResolved(
            CardInstanceExplore card, IProbabilityTarget target, bool success, float usedRate)
        {
            if (askMode != AskMode.OnFailure) return;   // 預設 Never，見 AskMode 的說明
            if (success) return;                        // 成功不問（C18⑦）
            if (encounter == null) return;

            // 手牌用盡不問 —— PlayCard 會在這之後自己呼叫 EndEncounter()，
            // 這時候跳詢問，玩家按哪個都一樣，只是多一下點擊。
            if (!encounter.HasCardsLeft) return;

            if (retryAskPanel == null)
            {
                if (!warnedNoPanel)
                {
                    warnedNoPanel = true;
                    Debug.LogWarning(
                        "[打牌] EncounterUIController 沒有指定 Retry Ask Panel —— " +
                        "C12「在試一次？」不會出現，失敗後維持舊行為（玩家自己決定要不要再出牌）。",
                        this
                    );
                }
                return;
            }

            // 失敗的當下就把手牌鎖住，不等延遲跑完 ——
            // 否則這段等待正好是個「還能再打一張」的空窗，等於漏掉了要問的那件事。
            SetHandInteractable(false);
            IsAsking = true;

            if (askRoutine != null) StopCoroutine(askRoutine);
            askRoutine = StartCoroutine(ShowAskAfterText());
        }

        private IEnumerator ShowAskAfterText()
        {
            DialogueBoxUI box = PopupService.Instance != null ? PopupService.Instance.dialogueBox : null;

            // 先讓「沒能撬開」那句打完。用 while 而不是固定秒數，因為文字長度是企劃在調的，
            // 寫死秒數的話換一句長一點的文案就會被打斷。
            while (box != null && box.IsTyping) yield return null;

            if (askDelay > 0f) yield return new WaitForSecondsRealtime(askDelay);

            askRoutine = null;

            // 等待期間環節可能已經被別的路徑結束（例如玩家按了結束鈕）。
            // 那樣就不該再跳詢問 —— 問一個已經結束的環節要不要重試沒有意義。
            if (encounter == null || !encounter.IsActive)
            {
                HideAskImmediate();
                yield break;
            }

            retryToggle.Set(retryAskPanel, true);
        }

        // ==========================================
        // 手牌用盡 → 付出代價重來（C12 / 全域資源制）
        // ==========================================
        private System.Action pendingYes;
        private System.Action pendingNo;

        /// <summary>
        /// 顯示「付出代價重來」的詢問。由 Stage 在手牌用盡且目標可重試時呼叫。
        ///
        /// 【為什麼用回呼參數而不是事件】付款與重抽的邏輯住在 Explore，
        /// 本類別住在 Core —— Core 不該反過來認識 Explore。由呼叫端把要做的事傳進來，
        /// 依賴方向就只有一個方向。
        ///
        /// 回傳 false 代表沒有面板可用，呼叫端要自己收尾（結束環節）。
        /// </summary>
        public bool ShowRetryOffer(string prompt, System.Action onYes, System.Action onNo)
        {
            if (retryAskPanel == null)
            {
                if (!warnedNoPanel)
                {
                    warnedNoPanel = true;
                    Debug.LogWarning(
                        "[打牌] EncounterUIController 沒有指定 Retry Ask Panel —— " +
                        "手牌用盡時無法詢問要不要付代價重來，一律直接結案。",
                        this
                    );
                }
                return false;
            }

            pendingYes = onYes;
            pendingNo = onNo;

            if (retryAskLabel != null && !string.IsNullOrEmpty(prompt))
            {
                retryAskLabel.text = prompt;
            }

            SetHandInteractable(false);
            IsAsking = true;

            // ⚠️ 不可以立刻彈出。這一刻對話框正在打「沒能撬開…這是你嘗試的第 5 次。」，
            //    面板蓋上去玩家就沒讀到自己為什麼失敗 —— 然後要他決定付不付錢重來，
            //    那是個沒有依據的決定。等字打完再問。
            if (askRoutine != null) StopCoroutine(askRoutine);
            askRoutine = StartCoroutine(ShowRetryOfferAfterText());

            return true;   // 已接手 —— 環節不會結束，等玩家從面板上做決定
        }

        private IEnumerator ShowRetryOfferAfterText()
        {
            DialogueBoxUI box = PopupService.Instance != null ? PopupService.Instance.dialogueBox : null;

            while (box != null && box.IsTyping) yield return null;

            if (askDelay > 0f) yield return new WaitForSecondsRealtime(askDelay);

            askRoutine = null;

            // 等待期間環節可能已經被別的路徑收掉了。此時彈出詢問會問一個
            // 已經結束的環節要不要重試 —— 而且 pendingYes 會對著失效的狀態執行。
            if (encounter == null || !encounter.IsActive)
            {
                HideAskImmediate();
                yield break;
            }

            retryToggle.Set(retryAskPanel, true);
        }

        /// <summary>「在試一次？」→ YES。</summary>
        public void OnRetryYes()
        {
            // 先取出再 HideAsk —— HideAsk 會清掉 pending，順序反了就叫不到
            System.Action yes = pendingYes;
            pendingYes = null;
            pendingNo = null;

            HideAsk();

            // 有回呼 = 這是「付代價重來」的詢問，交給 Stage 處理付款與重抽。
            // 沒有回呼 = 這是 OnFailure 模式的詢問，關掉面板就是繼續出牌，不必做別的。
            yes?.Invoke();
        }

        /// <summary>
        /// 「在試一次？」→ NO。結束打牌環節。
        ///
        /// 效果與按「結束」完全相同 —— 走同一條 EndEncounter()，
        /// 所以剩下的手牌一樣會被保留（防止「結束再點一次」重抽），
        /// 開箱結果一樣會補播。**不會**直接離開房間：
        /// 離開另有 C13/C14 的兩段式確認，這裡只是收掉打牌。
        /// </summary>
        public void OnRetryNo()
        {
            System.Action no = pendingNo;
            pendingYes = null;
            pendingNo = null;

            HideAsk();

            if (no != null)
            {
                // 「付代價重來」的詢問：由 Stage 決定怎麼收尾（結案 + 結束環節）
                no.Invoke();
                return;
            }

            if (encounter == null) encounter = DialogueEncounterController.Instance;
            if (encounter != null && encounter.IsActive) encounter.EndEncounter();
        }

        // ==========================================
        // 面板開關
        // ==========================================
        private void HideAsk()
        {
            IsAsking = false;
            pendingYes = null;
            pendingNo = null;

            if (askRoutine != null)
            {
                StopCoroutine(askRoutine);
                askRoutine = null;
            }

            if (retryAskPanel != null) retryToggle.Set(retryAskPanel, false);

            // ⚠️ 一定要還原。手牌區是常駐場景的同一個物件，
            // 這裡漏掉的話下一次遭遇的手牌會整排點不動，而且不會有任何錯誤訊息。
            SetHandInteractable(true);
        }

        /// <summary>不走淡出，用於初始化與環節結束的收尾。</summary>
        private void HideAskImmediate()
        {
            IsAsking = false;
            pendingYes = null;
            pendingNo = null;

            if (askRoutine != null)
            {
                StopCoroutine(askRoutine);
                askRoutine = null;
            }

            if (retryAskPanel != null)
            {
                // ⚠️ 順序不能反。先走正規關閉，UIDirector 的 Dialog 堆疊才會正確退一層 ——
                // 直接 HideImmediate 的話面板是不見了，但它還留在堆疊裡，
                // 下一次 PopDialog 會關到不相干的東西。
                retryToggle.Set(retryAskPanel, false);

                // 再把淡出硬收掉。SetActive(false) 會一併停掉 FadePanel 正在跑的淡出協程。
                var fade = retryAskPanel.GetComponent<FadePanel>();
                if (fade != null) fade.HideImmediate();
            }

            SetHandInteractable(true);
        }

        private void SetHandInteractable(bool value)
        {
            if (handInteractionGroup == null) return;

            handInteractionGroup.interactable = value;
            handInteractionGroup.blocksRaycasts = value;
        }

        private void HandleEncounterEnded()
        {
            // 環節結束一律收掉詢問。走 Immediate 是因為此時對話框正在播結算，
            // 一個還在淡出的確認框疊在上面很雜。
            HideAskImmediate();
        }
    }
}
