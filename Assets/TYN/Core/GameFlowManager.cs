using System;
using System.Collections;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 唯一的流程總管。
    ///
    /// 【兩個正交的軸】(設計文件 §4.1)
    ///   Stage      ── 互斥，同時只有一個（Menu / Explore / Battle / Shop…）
    ///   MapOverlay ── 常駐覆蓋層，只有「下拉 / 收起」兩態
    ///
    /// 整條流程恆為：
    ///   Stage 結束 → 地圖下拉(自動) → 玩家選節點 → 地圖收起 → 載入新 Stage
    ///
    /// 戰鬥線與事件線走的是**同一個** pattern，戰鬥只是 Stage 的一種。
    ///
    /// 【鐵則】
    ///   1. 任何 Stage 切換與地圖開合都必須經過本類別。Stage 內不得有 SceneManager。
    ///   2. Stage 結束是自動回報（NotifyStageComplete），不是玩家按返回鈕。
    /// </summary>
    public class GameFlowManager : MonoBehaviour
    {
        public static GameFlowManager Instance { get; private set; }

        [Header("組件")]
        public StageHost stageHost;
        public MapOverlayController mapOverlay;

        [Header("地圖生成")]
        [Tooltip("地圖生成參數。留空會產生一張最小地圖並警告")]
        public MapGenerationSettings mapSettings;

        [Header("道具")]
        [Tooltip("道具資料庫（id → 顯示名／圖示）。\n" +
                 "掛在這裡是因為背包本身（RunContext.inventory）就由本類別持有。\n" +
                 "留空不會壞，只是玩家會看到 id（例如「lockpick」）而不是「撬棍」")]
        public ItemDatabase itemDatabase;

        [Header("事件")]
        [Tooltip("隨機事件庫。留空則永遠不會觸發事件（不會壞，只是沒有事件）")]
        public EventLibrary eventLibrary;

        [Tooltip("這一區的戰鬥節點會遇到誰。留空 = 每一場都交給戰鬥組自己抽怪。\n\n" +
                 "在開新 run、地圖生成之後套用一次（見 EncounterPlanner）")]
        public EncounterPool encounterPool;

        [Header("角色")]
        [Tooltip("角色資料庫（id → 名字／立繪／寒暄）。\n" +
                 "留空不會壞，只是氣泡與對話框會顯示 id 而不是「時藏」")]
        public CharacterDatabase characterDatabase;

        [Header("啟動")]
        [Tooltip("遊戲啟動後要進入的第一個 Stage")]
        public StageType bootStage = StageType.Menu;

        [Tooltip("開局身上的錢。**佔位值** —— 經濟還沒設計，這只是為了讓商店能測。\n" +
                 "定案後這個值多半會改由遺產或難度決定，不會留在這裡")]
        [Min(0)] public int startingMoney = 0;

        [Tooltip("開局的戰鬥牌組。大綱裡是坎貝爾幫你準備的那一套。\n\n" +
                 "⚠️ **沒有這個，Starting Max Hp 填了也不會生效** ——\n" +
                 "空牌組進戰鬥是打不動的，所以 PlayerVitals 會擋住並退回舊行為。\n" +
                 "見 StartingDeckData 的說明")]
        public StartingDeckData startingDeck;

        [Tooltip("開局的 HP 上限。**0 = 不初始化**（維持舊行為：等第一場戰鬥打完才有值）。\n\n" +
                 "⚠️ 填了非 0 之後，戰鬥開始也會套用這條血條而不是戰鬥自己的預設值 ——\n" +
                 "那正是「run 開始就初始化」的用意，但要跟戰鬥組確認數值對得上")]
        [Min(0)] public int startingMaxHp = 0;

        [Tooltip("開局的 SAN 上限。**在戰鬥那邊叫 Energy**（見 PlayerVitals 的說明）。\n" +
                 "0 = 沿用戰鬥端原本的值")]
        [Min(0)] public int startingMaxSan = 0;

        [Header("對話節點")]
        [Tooltip("地圖上的**對話節點**要走哪一個 Stage。專案裡有兩套刻意並存的對話：\n\n" +
                 "· ProbabilityDialogue —— 資料驅動（一個事件一個資產），\n" +
                 "  出完牌才選回答、失敗就移掉那個回答。附件《對話》的兩段寫在這一套。\n" +
                 "· Dialogue —— 把牌打在選項上、逐次衰減、可以蓄意失敗。\n" +
                 "  內容寫死在 Stage_Dialogue prefab 上，一次只能有一段。\n\n" +
                 "⚠️ 改這一格就能整套換回去，不必改程式。")]
        public StageType dialogueNodeStage = StageType.ProbabilityDialogue;

        [Header("狀態 (唯讀)")]
        [SerializeField] private StageType currentStage = StageType.None;

        public StageType CurrentStage => currentStage;
        public bool IsMapOpen => mapOverlay != null && mapOverlay.IsOpen;
        public bool IsTransitioning { get; private set; }

        /// 單場 run。死亡或通關時整個丟棄重建。
        public RunContext Run { get; private set; }

        /// <summary>
        /// 正在插播的事件。**Event Stage 靠它知道自己該演哪一個。**
        ///
        /// 【為什麼要有這個中繼】事件不是節點，是「進節點之前插播的一段」。
        /// Stage prefab 沒辦法在 Inspector 指定「這次要演哪個事件」——
        /// 那是執行時才決定的，所以由總管放在這裡讓 Stage 來拿。
        /// </summary>
        public EventData PendingEvent { get; private set; }

        /// <summary>事件播完後要接著進的那個節點的 Stage。</summary>
        private StageType stageAfterEvent = StageType.None;

        /// <summary>
        /// 戰鬥打完後要接著進的 Stage。**只有事件把玩家拉進戰鬥時才會設**
        /// （見 <see cref="InsertBattleBeforeNextStage"/>）。
        ///
        /// 一般從地圖走到戰鬥節點時這裡是 None，打完照常回地圖。
        /// </summary>
        private StageType stageAfterBattle = StageType.None;

        /// 跨輪迴保存。遺產機制的載體，死亡不會清空。
        public MetaProgressData Meta { get; private set; }

        /// <summary>
        /// 道具 id → 玩家看得懂的名字。
        ///
        /// 【為什麼放這裡】背包（`Run.inventory`）由本類別持有，翻譯表放在旁邊最好找。
        /// 做成 static 是因為呼叫端多半只想要一個字串，不想每次都寫三層 null 檢查。
        ///
        /// 查不到（沒有資料庫、或資料庫裡沒這筆）就回傳 id 本身 ——
        /// 醜，但比空白好查。
        /// </summary>
        public static string ItemName(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";

            ItemDatabase db = Instance != null ? Instance.itemDatabase : null;
            return db != null ? db.DisplayNameOf(id) : id;
        }

        /// <summary>道具 id → 資料本體。查不到回 null。商店要圖示與價格時用這支。</summary>
        public static ItemData Item(string id)
        {
            ItemDatabase db = Instance != null ? Instance.itemDatabase : null;
            return db != null ? db.GetById(id) : null;
        }

        /// <summary>角色 id → 資料本體。查不到回 null。</summary>
        public static CharacterData Character(string id)
        {
            CharacterDatabase db = Instance != null ? Instance.characterDatabase : null;
            return db != null ? db.GetById(id) : null;
        }

        public event Action<StageType, StageType> OnStageChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 只有一個場景，不需要 DontDestroyOnLoad
            Meta = MetaProgressData.Load();
        }

        private void Start()
        {
            StartCoroutine(SwitchStage(bootStage));
        }

        // ==========================================
        // 流程入口 —— Stage prefab 只呼叫這些
        // ==========================================

        public void GoToMenu()
        {
            if (IsTransitioning) return;
            StartCoroutine(GoToMenuRoutine());
        }

        /// <summary>
        /// 開新的一場 run。
        /// 【遺產切分點】新 run 從 Meta 繼承什麼，由 RunContext.CreateNew 決定。
        /// </summary>
        public void StartNewRun()
        {
            if (IsTransitioning) return;
            StartCoroutine(StartNewRunRoutine());
        }

        /// <summary>
        /// 玩家在地圖上選了節點。收起地圖 → 依節點類型載入對應 Stage。
        /// </summary>
        public void EnterNode(RunNodeData node)
        {
            if (IsTransitioning || node == null) return;
            StartCoroutine(EnterNodeRoutine(node));
        }

        /// <summary>
        /// C2：Stage 完成的自動回報。由總管決定下一步（通常是地圖下拉）。
        /// </summary>
        public void NotifyStageComplete(StageResult result)
        {
            if (IsTransitioning) return;
            StartCoroutine(StageCompleteRoutine(result));
        }

        // ==========================================
        // 流程實作
        // ==========================================

        private IEnumerator GoToMenuRoutine()
        {
            IsTransitioning = true;

            yield return FadeOut();

            if (mapOverlay != null) mapOverlay.SetOpenImmediate(false);

            yield return SwitchStageInternal(StageType.Menu);

            yield return FadeIn();

            IsTransitioning = false;
        }

        private IEnumerator StartNewRunRoutine()
        {
            IsTransitioning = true;

            Run = RunContext.CreateNew(Meta);
            Run.AddMoney(startingMoney);

            // HP／SAN 不存在 RunContext 裡 —— 它們歸 RunStateManager（戰鬥端）持有，
            // 我方只負責「run 開始時把它設好」。見 PlayerVitals 的說明。
            if (startingMaxHp > 0)
            {
                PlayerVitals.EnsureInitialized(
                    startingMaxHp, startingMaxSan,
                    startingDeck != null ? startingDeck.Resolve() : null);
            }

            // 地圖在此一次生成，之後整場 run 都用同一份 —— 地圖 UI 反覆下拉收起不會重生成
            Run.mapData = MapGenerator.Generate(mapSettings, Run.runSeed);

            // 每個戰鬥節點要打誰，也在這裡一次安排好（保證項先佔位，剩下的抽）。
            // ⚠️ 種子跟地圖**錯開**（^ 1）—— 用同一個的話，敵人的抽選會跟
            //    地圖形狀綁在一起，兩張長得像的地圖會配到一樣的怪
            EncounterPlanner.AssignEnemies(
                Run.mapData, encounterPool, Run,
                new System.Random(Run.runSeed ^ 1), itemDatabase);

            Debug.Log(
                $"[Flow] 開始新的一場 run（seed {Run.runSeed}）：" +
                $"{Run.mapData.allNodes.Count} 個節點、{Run.mapData.MaxLayer + 1} 層"
            );

            yield return FadeOut();

            // C9：新手介紹由 Romtyui 製作。若尚未提供 prefab 就直接跳過。
            if (stageHost != null && stageHost.Has(StageType.Intro))
            {
                yield return SwitchStageInternal(StageType.Intro);
                yield return FadeIn();
                IsTransitioning = false;
                yield break;
            }

            yield return SwitchStageInternal(StageType.None);
            yield return FadeIn();

            IsTransitioning = false;

            yield return OpenMapRoutine();
        }

        private IEnumerator EnterNodeRoutine(RunNodeData node)
        {
            IsTransitioning = true;

            Run.mapData.MoveTo(node.nodeId);
            Run.pendingNode = node;

            // 收起地圖（此時畫面還沒黑，玩家看得到地圖收上去）
            if (mapOverlay != null) yield return mapOverlay.SlideUp();

            yield return FadeOut();

            // ── 事件的前置檢查（大綱：「每當前進到下一張地圖…就會有概率觸發事件」）──
            //
            // 只在**這一個點**檢查，不要散在各 Stage 裡。
            // 有事件就先演事件，演完再由 StageCompleteRoutine 接回原本的節點 ——
            // 前置，不覆蓋。玩家不會因為運氣好觸發了事件反而少玩到一間房。
            StageType nodeStage = StageTypeForNode(node);
            EventData ev = PickEventForNode();

            if (ev != null)
            {
                PendingEvent = ev;
                stageAfterEvent = nodeStage;
                yield return SwitchStageInternal(StageType.Event);
            }
            else
            {
                yield return SwitchStageInternal(nodeStage);
            }

            yield return FadeIn();

            IsTransitioning = false;
        }

        /// <summary>
        /// 把一場戰鬥**插在**事件與原本那個節點之間。由事件效果
        /// `EventEffect.Kind.StartBattle`（《好餓好餓的貪吃鬼》選項 B）呼叫。
        ///
        /// 【為什麼是「插入」不是「取代」】跟事件本身同一個原則 ——
        /// 「前置，不覆蓋。玩家不會因為運氣好觸發了事件反而少玩到一間房。」
        /// 直接把 stageAfterEvent 改成 Battle 的話，這一站的房間就被吃掉了。
        /// 所以先把原本要去的地方存進 stageAfterBattle，打完再接回去。
        ///
        /// ⚠️ 只有在事件流程中呼叫才有意義。不在事件裡呼叫會發警告並忽略 ——
        /// 沒有東西可以「接回去」的話，戰鬥打完玩家會被丟回地圖，
        /// 那跟呼叫方預期的「打完繼續」不一樣，而且不會有任何錯誤訊息。
        /// </summary>
        /// <param name="enemyId">要打誰。留空則交給戰鬥組自己抽怪。</param>
        public void InsertBattleBeforeNextStage(string enemyId = null)
        {
            if (currentStage != StageType.Event || stageAfterEvent == StageType.None)
            {
                Debug.LogWarning(
                    "[Flow] InsertBattleBeforeNextStage 只能在事件流程中呼叫 —— " +
                    $"現在是 {currentStage}、stageAfterEvent = {stageAfterEvent}。這次忽略。");
                return;
            }

            if (stageAfterEvent == StageType.Battle)
            {
                Debug.LogWarning("[Flow] 這個事件已經安排過一場戰鬥了，不重複插入");
                return;
            }

            stageAfterBattle = stageAfterEvent;
            stageAfterEvent = StageType.Battle;

            BattleStageController.PendingEnemyId = string.IsNullOrEmpty(enemyId) ? null : enemyId;

            Debug.Log(
                $"[Flow] 事件安排了一場戰鬥（對手：{(string.IsNullOrEmpty(enemyId) ? "交給戰鬥組抽" : enemyId)}），" +
                $"打完接回 {stageAfterBattle}");
        }

        /// <summary>
        /// **除錯用**：直接切到某個 Stage，跳過地圖與節點。
        ///
        /// 新做好的 Stage 常常還沒有地圖入口，那時要驗畫面就得先想辦法走到它。
        /// 這一支讓「看一眼」變成一秒的事。
        ///
        /// ⚠️ 跳過去的 Stage 結束後**照常回報完成、地圖照常下拉**，
        /// 所以不會把流程弄壞 —— 只是少了「從節點進來」那一段。
        ///
        /// `#if` 包起來：**正式包裡整個方法會消失**，不會變成可以被誤用的後門。
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public void DebugJumpToStage(StageType type)
        {
            if (IsTransitioning)
            {
                Debug.LogWarning("[Flow] 轉場中，這次跳轉忽略");
                return;
            }

            StartCoroutine(DebugJumpRoutine(type));
        }

        private IEnumerator DebugJumpRoutine(StageType type)
        {
            IsTransitioning = true;

            if (mapOverlay != null) yield return mapOverlay.SlideUp();
            yield return FadeOut();
            yield return SwitchStageInternal(type);
            yield return FadeIn();

            IsTransitioning = false;
        }

        /// <summary>
        /// 這一站要不要插播事件。抽不到就回 null。
        ///
        /// 亂數綁 run 種子 + 節點 id —— 同一場 run 的同一個節點，
        /// 重進不會重骰出不同的事件。
        /// </summary>
        private EventData PickEventForNode()
        {
            if (eventLibrary == null || Run == null) return null;

            RunNodeData node = Run.pendingNode;
            int seed = Run.runSeed ^ (node != null && !string.IsNullOrEmpty(node.nodeId)
                ? node.nodeId.GetHashCode() : 0);

            return eventLibrary.Pick(Run, new System.Random(seed), itemDatabase);
        }

        private IEnumerator StageCompleteRoutine(StageResult result)
        {
            IsTransitioning = true;

            Debug.Log($"[Flow] {currentStage} 完成：{result}");

            // ── 事件播完 → 接回原本那個節點，而不是收工回地圖 ──
            if (currentStage == StageType.Event && stageAfterEvent != StageType.None)
            {
                // 標記成觸發過**是在這裡**，不是選到的時候 ——
                // 玩家中途離開的話，那個事件應該還能再出現
                EventLibrary.MarkTriggered(PendingEvent, Run);
                PendingEvent = null;

                StageType next = stageAfterEvent;
                stageAfterEvent = StageType.None;

                yield return FadeOut();
                yield return SwitchStageInternal(next);
                yield return FadeIn();

                IsTransitioning = false;
                yield break;
            }

            // ── 事件安排的戰鬥打完 → 接回原本那個節點 ──
            //
            // ⚠️ 只在「玩家還活著」時接回去。戰死的話要走下面的遺產結算，
            //    硬接回房間會變成死了還能繼續探索。
            if (currentStage == StageType.Battle
                && stageAfterBattle != StageType.None
                && result == StageResult.Completed)
            {
                StageType next = stageAfterBattle;
                stageAfterBattle = StageType.None;

                Debug.Log($"[Flow] 事件安排的戰鬥結束，接回 {next}");

                yield return FadeOut();
                yield return SwitchStageInternal(next);
                yield return FadeIn();

                IsTransitioning = false;
                yield break;
            }

            // 走到這裡代表不會再接回去了（打輸、或本來就沒安排）——
            // 留著的話下一場從地圖進的戰鬥打完會被莫名其妙送去某個房間
            stageAfterBattle = StageType.None;

            // ── 遺產結算點 ──
            if (result == StageResult.PlayerDied || result == StageResult.RunFinished)
            {
                Run?.ContributeToMeta(Meta, result);

                yield return FadeOut();
                yield return SwitchStageInternal(StageType.Menu);

                Run = null;   // A4：死亡 → 這場 run 整個丟棄，下個輪迴重來

                yield return FadeIn();

                IsTransitioning = false;
                yield break;
            }

            // 打完 Boss 也視為 run 結束
            RunNodeData current = Run?.CurrentNode;
            if (current != null && Run.mapData.IsFinalLayer(current))
            {
                yield return StageCompleteRoutine(StageResult.RunFinished);
                yield break;
            }

            // 一般情況：卸掉 Stage，地圖自動下拉（C1/C2）
            yield return FadeOut();
            yield return SwitchStageInternal(StageType.None);
            yield return FadeIn();

            IsTransitioning = false;

            yield return OpenMapRoutine();
        }

        private IEnumerator OpenMapRoutine()
        {
            if (mapOverlay == null)
            {
                Debug.LogWarning("[Flow] 沒有指定 MapOverlay，無法下拉地圖");
                yield break;
            }

            mapOverlay.Refresh(Run);
            yield return mapOverlay.SlideDown();
            yield return mapOverlay.OnOpened();
        }

        // ==========================================
        // Stage 切換
        // ==========================================

        private IEnumerator SwitchStage(StageType next)
        {
            IsTransitioning = true;

            yield return SwitchStageInternal(next);
            yield return FadeIn();

            IsTransitioning = false;
        }

        /// 假設呼叫時畫面已經是黑的。不負責淡入淡出。
        private IEnumerator SwitchStageInternal(StageType next)
        {
            StageType previous = currentStage;

            // 換環節時把訊息佇列清乾淨。放在這裡而不是各 Stage 的 OnStageExit ——
            // 否則每新增一個 Stage 就要記得再寫一次，遲早會有人漏掉。
            PopupService.Instance?.CloseAll();

            // 手牌區（含「結束」鍵）同理。**它是場景常駐的，沒有人主動收就會一直留著。**
            //
            // 以前不需要這一行，是因為對話框的 root 指的是整塊 DialogueUI 畫布，
            // 關對話框等於把畫布底下所有東西連坐關掉 —— 手牌區剛好在裡面。
            // 那個連坐後來被拆掉了（它同時也把氣泡吞掉，見 DialogueBoxUI.Hide），
            // 於是「從主選單進地圖，背景浮著一顆結束鍵」就跑出來了。
            EldritchMile.Explore.ExploreHandUI.Instance?.Hide();

            // 氣泡也是同一批「畫布連坐關掉」的受害者，只是還沒有人回報 ——
            // 換環節時正好有一句話在播，它會被帶到下一個畫面上。
            // 用 Immediate：這一刻要卸載了，播收合動畫會來不及，殘影反而更糟。
            //
            // ⚠️ 這行在**新環節載入之前**，所以不會蓋掉新環節自己的招呼（例如商店的寒暄）
            EldritchMile.UI.SpeechBubbleUI.Instance?.HideImmediate();

            if (stageHost != null && stageHost.Current != null)
            {
                yield return stageHost.Current.OnStageExit();
                stageHost.Unload();
            }

            currentStage = next;

            if (next != StageType.None && stageHost != null)
            {
                StageController controller = stageHost.Load(next);

                if (controller != null)
                {
                    controller.OnStageEnter(Run);
                }
            }

            // 統一套用這個環節該有的 UI 狀態：該開的開、上個環節殘留的關掉。
            // 放在 Stage 載入之後，新 prefab 裡的面板才掃得到。
            UIDirector.Instance?.ApplyStage(next);

            OnStageChanged?.Invoke(previous, next);
        }

        /// Stage 進場動畫在黑幕淡出後才播
        private IEnumerator FadeIn()
        {
            if (ScreenFader.Instance != null)
            {
                yield return ScreenFader.Instance.FadeFromBlack();
            }

            if (stageHost != null && stageHost.Current != null)
            {
                yield return stageHost.Current.OnStageReady();
            }
        }

        private IEnumerator FadeOut()
        {
            if (ScreenFader.Instance != null)
            {
                yield return ScreenFader.Instance.FadeToBlack();
            }
        }

        private StageType StageTypeForNode(RunNodeData node)
        {
            switch (node.kind)
            {
                case MapNodeKind.Combat:
                case MapNodeKind.Boss:
                    return StageType.Battle;

                case MapNodeKind.Shop:
                    return StageType.Shop;

                case MapNodeKind.SpecialEvent:
                    return StageType.SpecialEvent;

                case MapNodeKind.Dialogue:
                    return dialogueNodeStage;

                case MapNodeKind.Event:
                default:
                    return StageType.Explore;
            }
        }
    }
}
