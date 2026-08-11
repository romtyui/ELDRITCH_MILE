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

        [Header("啟動")]
        [Tooltip("遊戲啟動後要進入的第一個 Stage")]
        public StageType bootStage = StageType.Menu;

        [Header("狀態 (唯讀)")]
        [SerializeField] private StageType currentStage = StageType.None;

        public StageType CurrentStage => currentStage;
        public bool IsMapOpen => mapOverlay != null && mapOverlay.IsOpen;
        public bool IsTransitioning { get; private set; }

        /// 單場 run。死亡或通關時整個丟棄重建。
        public RunContext Run { get; private set; }

        /// 跨輪迴保存。遺產機制的載體，死亡不會清空。
        public MetaProgressData Meta { get; private set; }

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

                case MapNodeKind.Event:
                default:
                    return StageType.Explore;
            }
        }
    }
}
