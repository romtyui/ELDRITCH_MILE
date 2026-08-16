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

        [Header("狀態 (唯讀)")]
        [SerializeField] private StageType currentStage = StageType.None;

        public StageType CurrentStage => currentStage;
        public bool IsMapOpen => mapOverlay != null && mapOverlay.IsOpen;
        public bool IsTransitioning { get; private set; }

        /// 單場 run。死亡或通關時整個丟棄重建。
        public RunContext Run { get; private set; }

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

            // 地圖在此一次生成，之後整場 run 都用同一份 —— 地圖 UI 反覆下拉收起不會重生成
            Run.mapData = MapGenerator.Generate(mapSettings, Run.runSeed);

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
            yield return SwitchStageInternal(StageTypeForNode(node));
            yield return FadeIn();

            IsTransitioning = false;
        }

        private IEnumerator StageCompleteRoutine(StageResult result)
        {
            IsTransitioning = true;

            Debug.Log($"[Flow] {currentStage} 完成：{result}");

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
                    return StageType.Dialogue;

                case MapNodeKind.Event:
                default:
                    return StageType.Explore;
            }
        }
    }
}
