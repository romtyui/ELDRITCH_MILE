using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EldritchMile.Explore
{
    using EldritchMile.Core;

    /// <summary>
    /// 探索 Stage。取代封存的 ExplorationManager。
    ///
    /// 【拿掉了什麼】
    ///   · 自己的黑幕（fadeCanvasGroup）→ 統一由 ScreenFader 負責
    ///   · 反查 PerspectiveMapGenerator.Instance 拿節點資料 → 改由 OnStageEnter 收 RunContext
    ///   · SceneManager.MoveGameObjectToScene → 只有一個場景，不需要了
    ///   · fieldOfView 進場 zoom → C10 改正交後那段本來就會靜默失效，直接丟棄
    ///
    /// 【C13 的迴圈】房間清空**不會**自動離開，而是問玩家「要探索其他的東西嗎？」，
    /// 選 NO 才走離開流程；離開本身又是兩段式確認（C14）。
    /// </summary>
    public class ExploreStageController : StageController
    {
        public override StageType Stage => StageType.Explore;

        [Header("房間")]
        public Transform roomRoot;
        public RoomLibrary roomLibrary;

        [Header("離開 (C13/C14)")]
        [Tooltip("ExitTag 上的 Button。\n" +
                 "兩段式確認由「hover 滑下提示 → 點擊跳出確認面板」構成，" +
                 "所以標籤本身只需要單擊，不必再做二次點擊")]
        public Button exitTag;

        [Tooltip("房間清空後詢問是否繼續探索的面板。\n" +
                 "若上面掛了 FadePanel 就會用淡入（與 MapBanner 外觀一致），否則直接 SetActive")]
        public GameObject continueAskPanel;

        [Header("打牌環節 (C18)")]
        [Tooltip("手牌來源。牌組內容從 RunContext.exploreDeck 同步過來")]
        public ExplorationDeck explorationDeck;

        [Tooltip("每次開始打牌環節抽幾張。C18⑤：這個數字同時也是可嘗試的次數上限")]
        [Min(1)] public int cardsPerEncounter = 5;

        // 打牌環節的 UI 與規則引擎都常駐在 EventScene（手牌與對話框是同一組構圖），
        // 而 prefab 無法在 Inspector 引用場景物件，所以執行時才解析。
        private DialogueEncounterController Encounter => DialogueEncounterController.Instance;
        private ExploreHandUI HandUI => ExploreHandUI.Instance;

        private GameObject currentRoom;
        private RoomController room;
        private RunContext run;
        private FadePanel continueAskFade;
        private UIPanel continueAskUI;
        private bool uiRefsResolved;
        private bool continueAskShown;

        // 中途結束時保留的手牌，防止重抽（見 BeginEncounter 的說明）
        private IProbabilityTarget suspendedTarget;
        private readonly List<CardInstanceExplore> suspendedHand = new List<CardInstanceExplore>();

        // 延後播報的開箱結果
        private string pendingLootName;
        private readonly List<string> pendingLoot = new List<string>();

        /// 本次遭遇針對的目標（世界物件，非對話框裡的化身）
        private IProbabilityTarget currentEncounterTarget;

        /// <summary>
        /// 統一走這裡開關詢問面板。三段 fallback：
        ///   1. 面板掛了 UIPanel(Dialog) → 交給 UIDirector 的堆疊管理，
        ///      這樣換環節時 CloseAllDialogs() 會自動收掉，不必靠這裡記得關
        ///   2. 只掛了 FadePanel → 自己淡入淡出
        ///   3. 都沒有 → SetActive
        /// </summary>
        private void SetContinueAskVisible(bool visible)
        {
            if (continueAskPanel == null) return;

            if (!uiRefsResolved)
            {
                uiRefsResolved = true;
                continueAskFade = continueAskPanel.GetComponent<FadePanel>();
                continueAskUI = continueAskPanel.GetComponent<UIPanel>();
            }

            if (continueAskUI != null && continueAskUI.kind == UIKind.Dialog && UIDirector.Instance != null)
            {
                if (visible) UIDirector.Instance.PushDialog(continueAskUI);
                else UIDirector.Instance.CloseDialog(continueAskUI);
                return;
            }

            if (continueAskFade != null)
            {
                if (visible) continueAskFade.Show();
                else continueAskFade.Hide();
            }
            else
            {
                continueAskPanel.SetActive(visible);
            }
        }

        // ==========================================
        // Stage 生命週期
        // ==========================================
        public override void OnStageEnter(RunContext ctx)
        {
            run = ctx;

            if (run == null)
            {
                Debug.LogWarning("[探索] 沒有 RunContext，無法生成房間");
                return;
            }

            SyncDeckFromRun();
            SpawnRoom(run.pendingNode);

            HandUI?.Hide();

            continueAskShown = false;
            SetContinueAskVisible(false);

            if (exitTag != null)
            {
                exitTag.onClick.RemoveListener(ShowContinueAsk);
                exitTag.onClick.AddListener(ShowContinueAsk);
            }

        }

        public override IEnumerator OnStageReady()
        {
            if (room != null && !string.IsNullOrEmpty(room.entryText))
            {
                PopupService.Instance?.QueueText(room.entryText);
            }
            yield break;
        }

        public override IEnumerator OnStageExit()
        {
            if (exitTag != null) exitTag.onClick.RemoveListener(ShowContinueAsk);

            if (room != null) room.OnRoomCleared -= HandleRoomCleared;

            // 環節還開著就換環節（例如中途被流程強制切走）時，
            // HandleEncounterEnded 不會跑到，這裡補上同樣的清理
            if (Encounter != null)
            {
                Encounter.OnEncounterEnded -= HandleEncounterEnded;
                Encounter.HandExhaustedInterceptor = null;
            }

            PopupService.Instance?.CloseAll();

            if (currentRoom != null)
            {
                Destroy(currentRoom);
                currentRoom = null;
                room = null;
            }

            yield break;
        }

        /// <summary>
        /// 把整場 run 的探索牌組灌進場上的 ExplorationDeck。
        ///
        /// 【為什麼要這一步】牌組的真相在 `RunContext.exploreDeck`（跨房間保存），
        /// 而 `ExplorationDeck` 是每個 Stage prefab 自帶的執行期物件，會隨 Stage 生滅。
        /// 不同步的話，玩家在前一個房間獲得的卡進不了下一個房間。
        /// </summary>
        private void SyncDeckFromRun()
        {
            if (explorationDeck == null || run == null) return;

            if (run.exploreDeck.Count == 0)
            {
                // run 還沒有牌組（第一次進探索）→ 用 prefab 上設定的起始牌組當種子
                run.exploreDeck.AddRange(explorationDeck.startingDeck);
            }
            else
            {
                explorationDeck.startingDeck.Clear();
                explorationDeck.startingDeck.AddRange(run.exploreDeck);
            }

            explorationDeck.InitializeDeck();
            Debug.Log($"[探索] 牌組同步完成：{explorationDeck.startingDeck.Count} 張");
        }

        // ==========================================
        // 房間生成
        // ==========================================
        private void SpawnRoom(RunNodeData node)
        {
            if (roomLibrary == null)
            {
                Debug.LogWarning("[探索] 沒有指定 RoomLibrary");
                return;
            }

            // 用 run seed + nodeId 當種子：同一場 run 進同一個節點擺設一致，
            // 不同節點之間又不會長得一樣
            int seed = run.runSeed ^ (node != null ? node.nodeId.GetHashCode() : 0);
            var rng = new System.Random(seed);

            GameObject prefab = roomLibrary.Pick(node, rng);
            if (prefab == null) return;

            Transform parent = roomRoot != null ? roomRoot : transform;
            currentRoom = Instantiate(prefab, parent, false);
            currentRoom.name = $"Room_{node?.kind}";

            room = currentRoom.GetComponent<RoomController>();
            if (room != null)
            {
                room.Populate(seed);
                room.OnRoomCleared += HandleRoomCleared;
            }
        }

        // ==========================================
        // C13：清空後詢問是否繼續探索
        // ==========================================
        private void HandleRoomCleared()
        {
            // 等待邏輯統一在 ShowContinueAsk 裡，這裡直接呼叫即可
            ShowContinueAsk();
        }

        private void AskContinueOnce()
        {
            if (PopupService.Instance != null) PopupService.Instance.OnAllClosed -= AskContinueOnce;
            ShowContinueAsk();
        }

        /// <summary>
        /// 顯示「要探索其他的東西嗎？」。
        ///
        /// 【單一出口】兩條路都走這裡：
        ///   · 房間所有東西都互動完（C13）
        ///   · 玩家主動點 ExitTag（C14 的兩段式確認之後）
        ///
        /// 好處是玩家永遠只會看到同一個確認介面，不會「有時候直接走掉、有時候跳窗」；
        /// 也讓「不小心點到離開」有一次挽回機會。
        /// </summary>
        public void ShowContinueAsk()
        {
            if (continueAskShown) return;   // 避免清空與點擊同時觸發跳兩次

            // ⚠️ 不可以疊在對話框上面。
            // 打牌結束後還有結果與獲得道具要播，這時跳確認面板會蓋住玩家正在讀的內容。
            // 兩個入口（房間清空、點 ExitTag）都要等 —— 之前只有前者有等，
            // 所以點 ExitTag 會直接疊上去。
            bool busy = (PopupService.Instance != null && PopupService.Instance.IsAnyOpen)
                     || (Encounter != null && Encounter.IsActive);

            if (busy)
            {
                if (PopupService.Instance != null)
                {
                    PopupService.Instance.OnAllClosed -= AskContinueOnce;
                    PopupService.Instance.OnAllClosed += AskContinueOnce;
                }
                return;
            }

            continueAskShown = true;
            SetContinueAskVisible(true);
        }

        /// 「要探索其他的東西嗎？」→ YES。留在房間，玩家自己找還沒點過的東西。
        public void OnContinueExploring()
        {
            continueAskShown = false;
            SetContinueAskVisible(false);
        }

        /// 「要探索其他的東西嗎？」→ NO。真正離開。
        public void OnChooseLeave()
        {
            continueAskShown = false;
            SetContinueAskVisible(false);
            RequestExit();
        }

        // ==========================================
        // C18：打牌環節
        // ==========================================

        /// <summary>
        /// 開始一次打牌環節，對象是玩家點的那個目標。
        ///
        /// 【C18②⑤】從探索牌組抽 `cardsPerEncounter` 張。手牌數量同時就是
        /// 「可以嘗試幾次」的上限，不需要另外設次數限制。
        /// </summary>
        public void BeginEncounter(IProbabilityTarget target, string promptText = null, Sprite closeUp = null)
        {
            if (Encounter == null)
            {
                Debug.LogWarning(
                    "[探索] 場上找不到 DialogueEncounterController。\n" +
                    "它應該常駐在 EventScene（與對話框、手牌區同一組），不是放在 Stage prefab 裡。"
                );
                return;
            }

            if (Encounter.IsActive) return;

            if (!string.IsNullOrEmpty(promptText))
            {
                PopupService.Instance?.ShowText(promptText);
            }

            // 在對話框裡生成對象化身：卡片打在它身上、機率顯示在它頭上。
            // 世界裡的寶箱維持狀態真相，但不再是拖曳目標 —— 它可能被遮住或位置不佳。
            IProbabilityTarget encounterTarget = target;

            DialogueBoxUI box = PopupService.Instance != null ? PopupService.Instance.dialogueBox : null;
            if (box != null)
            {
                box.HoldOpen = true;   // 打牌期間點擊只推進文字，不關閉對話框

                EncounterTargetView view = box.SpawnTargetView(target, closeUp);
                if (view != null)
                {
                    encounterTarget = view;

                    // 兩段式出牌的第二段：點大圖 = 把選取的卡打在它身上。
                    // 世界裡的寶箱現在被壓黑層擋著，點不到也不該被點。
                    view.OnClicked += HandleTargetViewClicked;
                }
            }

            // ⚠️ 防止「點結束再點一次目標」來重抽手牌。
            //
            // 衰減本身已經限制了總嘗試次數（衰減記在目標上，不會因為重進而重置），
            // 但重抽會讓玩家能一直換一手更好的屬性組合去試 —— 那等於繞過了
            // 「這次遭遇你只有這幾張牌」的設計。
            //
            // 所以中途結束時手牌會被保留，同一個目標再進來就接著用剩下的。
            var hand = new List<CardInstanceExplore>();

            if (suspendedTarget == target && suspendedHand.Count > 0)
            {
                hand.AddRange(suspendedHand);
                Debug.Log($"[打牌] 接續上次中斷的手牌：{hand.Count} 張");
            }
            else if (explorationDeck != null)
            {
                explorationDeck.DrawCards(cardsPerEncounter);
                hand.AddRange(explorationDeck.Hand);
            }

            suspendedTarget = null;
            suspendedHand.Clear();
            currentEncounterTarget = target;

            if (hand.Count == 0)
            {
                Debug.LogWarning("[探索] 牌組抽不到任何卡，打牌環節不會開始");
                return;
            }

            // 先開手牌區再 Begin —— 手牌區在 Start 才訂閱事件，
            // 順序反了就會漏掉第一次的 OnHandChanged，卡片畫不出來。
            HandUI?.Show();

            Encounter.OnEncounterEnded -= HandleEncounterEnded;
            Encounter.OnEncounterEnded += HandleEncounterEnded;

            // 手牌用盡時先問「要不要付代價重來」，而不是直接結束
            Encounter.HandExhaustedInterceptor = TryOfferRetry;

            // 目前一次只針對一個目標。Phase 6 的多選項對話會傳入整組選項。
            Encounter.Begin(hand, new List<IProbabilityTarget> { encounterTarget });
        }

        /// <summary>
        /// 兩段式出牌的第二段。互動物件被點擊時先問這裡 ——
        /// 回傳 true 代表這次點擊已經當成出牌用掉了。
        /// </summary>
        /// <summary>
        /// 由互動物件回報「這次開箱拿到什麼」，延後到打牌結束才播報。
        /// 道具本身在成功當下就已經入袋，這裡只管播報時機。
        /// </summary>
        public void DeferLootReport(string containerName, List<string> items)
        {
            pendingLootName = containerName;
            pendingLoot.Clear();
            if (items != null) pendingLoot.AddRange(items);
        }

        private void HandleTargetViewClicked(EncounterTargetView view)
        {
            TryPlaySelectedCardOn(view);
        }

        // ==========================================
        // 手牌用盡 → 付出代價重來
        // ==========================================
        /// <summary>
        /// 由 `DialogueEncounterController` 在手牌用盡、即將自動結束前呼叫。
        ///
        /// 回傳 true = 我接手了，環節先別結束（詢問面板已經跳出來，等玩家決定）。
        /// 回傳 false = 沒有重試機會，照原本流程結束環節。
        ///
        /// ⚠️ 回傳 true 之後**一定**要有人收尾 —— 兩個回呼各自負責
        /// RefillHand() 或 EndEncounter()，漏掉環節會永遠卡著。
        /// </summary>
        private bool TryOfferRetry()
        {
            if (currentEncounterTarget is not IRetryableTarget retryable) return false;
            if (!retryable.CanOfferRetry) return false;

            EncounterUIController ui = EncounterUIController.Instance;
            if (ui == null) return false;

            // 沒有詢問面板可用時 ShowRetryOffer 會回 false —— 那就當作不能重試，
            // 照原流程結案，不要卡在一個問不出來的詢問上
            return ui.ShowRetryOffer(
                retryable.BuildRetryPrompt(),
                () => ConfirmRetry(retryable),
                DeclineRetry
            );
        }

        /// 玩家答應付代價重來。
        private void ConfirmRetry(IRetryableTarget retryable)
        {
            if (Encounter == null || !Encounter.IsActive) return;

            // 付款可能失敗（道具在詢問期間被別的流程用掉了之類）。
            // 失敗就當作玩家選了「不要」，環節照常結束 —— 不能白白放行。
            if (!retryable.TryPayForRetry())
            {
                DeclineRetry();
                return;
            }

            retryable.ResetForRetry();

            // 重抽一手。走 DrawCards 而不是沿用舊手牌 —— 舊的已經出完了。
            var hand = new List<CardInstanceExplore>();
            if (explorationDeck != null)
            {
                explorationDeck.DiscardHand();
                explorationDeck.DrawCards(cardsPerEncounter);
                hand.AddRange(explorationDeck.Hand);
            }

            if (hand.Count == 0)
            {
                Debug.LogWarning("[探索] 重試時抽不到任何卡，環節結束");
                DeclineRetry();
                return;
            }

            // ⚠️ 用 RefillHand 而不是 Begin() —— Begin 會把 cardsPlayed 歸零，
            //    次數提示就會從「第 1 次」重來，但玩家其實已經被收了第 N 次的錢。
            Encounter.RefillHand(hand);
        }

        /// 玩家拒絕，或付不起。結束環節，讓目標走結案流程。
        private void DeclineRetry()
        {
            if (Encounter != null && Encounter.IsActive) Encounter.EndEncounter();
        }

        public bool TryPlaySelectedCardOn(IProbabilityTarget target)
        {
            if (HandUI == null || Encounter == null || !Encounter.IsActive) return false;
            return HandUI.TryPlaySelectedOn(target);
        }

        /// <summary>
        /// C18⑥：玩家按下「結束」。
        /// ⚠️ 只有玩家主動按、或手牌用盡才會結束 —— 判定成功**不會**自動結束，
        /// 因為蓄意失敗是合法策略（C18⑦）。
        /// </summary>
        public void EndEncounter()
        {
            if (Encounter != null && Encounter.IsActive) Encounter.EndEncounter();
        }

        private void HandleEncounterEnded()
        {
            if (Encounter == null) return;

            Encounter.OnEncounterEnded -= HandleEncounterEnded;

            // ⚠️ 一定要清掉。DialogueEncounterController 常駐場景，而本控制器住在
            //    Stage prefab 裡、換環節就被銷毀 —— 留著的話下一個環節觸發手牌用盡時，
            //    會呼叫到已銷毀物件上的方法。
            Encounter.HandExhaustedInterceptor = null;

            // 還有手牌沒出完 = 玩家按了結束中途離開 → 把剩下的存起來。
            // 同一個目標再進來時接續使用，避免用「結束再點一次」重抽一手更好的牌。
            // 手牌用盡才離開的話不留 —— 那次遭遇已經用完了。
            if (Encounter.HasCardsLeft)
            {
                suspendedTarget = Encounter.PrimaryTarget ?? currentEncounterTarget;
                suspendedHand.Clear();
                suspendedHand.AddRange(Encounter.Hand);
                Debug.Log($"[打牌] 中途結束，保留 {suspendedHand.Count} 張手牌");
            }
            else
            {
                suspendedTarget = null;
                suspendedHand.Clear();

                // 手牌用盡 = 這次遭遇的機會用完了，通知目標結案。
                // ⚠️ 只有這一條路要通知 —— 中途按結束（上面那個分支）是暫停不是用盡，
                //    那時手牌被保留著，玩家回來還能接著打。
                //
                // 不通知的話：衰減已歸零（保證 0%）但物件還可以點、還會重抽手牌，
                // 而且永遠不回報房間 → C13 的房間清空永遠不觸發。
                currentEncounterTarget?.OnAttemptsExhausted();
            }

            currentEncounterTarget = null;
            HandUI?.Hide();

            // 打牌期間壓下來的開箱結果，現在才播報
            if (pendingLoot.Count > 0 || !string.IsNullOrEmpty(pendingLootName))
            {
                PopupService.Instance?.ShowLoot(pendingLootName, pendingLoot);
                pendingLootName = null;
                pendingLoot.Clear();
            }

            // 解除 HoldOpen，對話框才收得掉；對象大圖也一併移除
            DialogueBoxUI box = PopupService.Instance != null ? PopupService.Instance.dialogueBox : null;
            if (box != null)
            {
                box.HoldOpen = false;
                box.ClearTargetViews();
            }

            // 結束打牌就收掉對話框 —— 但等排隊中的訊息（成功/失敗的後續）播完再收，
            // 不會把玩家正在讀的最後一句截斷。內容是空的就立刻收。
            PopupService.Instance?.CloseWhenDrained();

            // 【關鍵】按「結束」本身就相當於在對話框上點一下：
            //   · 成功 → 目前顯示「鎖開了」，推進一次就接到「獲得了…」
            //   · 失敗 → 目前顯示「沒能撬開」，後面沒東西了，推進一次就關掉
            // 沒有這一下的話，玩家按完結束還要再手動點一次才有反應，很像卡住。
            // 用 AdvanceImmediate：文字還在打的時候按結束，也要一次做完，
            // 不能只是「把字補完」然後又停在那裡等下一次點擊。
            //
            // ⚠️ 但**只有玩家主動結束**才成立。手牌用盡是自動結束，玩家什麼都沒按 ——
            //    這時候推進會把「最後一張牌的判定結果」在同一個呼叫堆疊裡直接跳掉
            //    （SkipTyping → Hide → Drain 到下一則），玩家根本看不到成功或失敗，
            //    畫面直接變成物品結算。那一則要留著等玩家自己點。
            if (Encounter.EndedByPlayer) box?.AdvanceImmediate();

            // Q13 暫行做法：環節結束就把剩下的手牌棄掉，下次重抽
            if (explorationDeck != null) explorationDeck.DiscardHand();
        }

        // ==========================================
        // 離開
        // ==========================================
        /// <summary>
        /// 真正離開房間。只有確認面板的「離開」會走到這裡。
        /// C2：Stage 結束是自動回報，接著地圖會自己下拉。
        /// </summary>
        public void RequestExit()
        {
            ReportComplete(StageResult.Completed);
        }
    }
}
