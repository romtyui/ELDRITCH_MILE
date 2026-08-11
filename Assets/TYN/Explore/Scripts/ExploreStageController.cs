using System.Collections;
using UnityEngine;

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
        [Tooltip("ExitTag 物件。掛 BookmarkHover(第一段) + TwoStageConfirm(第二段)")]
        public TwoStageConfirm exitTag;

        [Tooltip("房間清空後詢問是否繼續探索的面板。\n" +
                 "若上面掛了 FadePanel 就會用淡入（與 MapBanner 外觀一致），否則直接 SetActive")]
        public GameObject continueAskPanel;

        [Header("打牌環節 (C18)")]
        public DialogueEncounterController encounter;

        private GameObject currentRoom;
        private RoomController room;
        private RunContext run;
        private FadePanel continueAskFade;

        /// 統一走這裡開關詢問面板，有 FadePanel 就淡入，沒有就硬切
        private void SetContinueAskVisible(bool visible)
        {
            if (continueAskPanel == null) return;

            if (continueAskFade == null)
            {
                continueAskFade = continueAskPanel.GetComponent<FadePanel>();
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

            SpawnRoom(run.pendingNode);

            SetContinueAskVisible(false);

            if (exitTag != null)
            {
                exitTag.onConfirmed.RemoveListener(RequestExit);
                exitTag.onConfirmed.AddListener(RequestExit);
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
            if (exitTag != null) exitTag.onConfirmed.RemoveListener(RequestExit);

            if (room != null) room.OnRoomCleared -= HandleRoomCleared;

            PopupService.Instance?.CloseAll();

            if (currentRoom != null)
            {
                Destroy(currentRoom);
                currentRoom = null;
                room = null;
            }

            yield break;
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
            if (PopupService.Instance != null && PopupService.Instance.IsAnyOpen)
            {
                // 等玩家把當前彈窗關掉再問，避免蓋在一起
                PopupService.Instance.OnAllClosed += AskContinueOnce;
            }
            else
            {
                ShowContinueAsk();
            }
        }

        private void AskContinueOnce()
        {
            if (PopupService.Instance != null) PopupService.Instance.OnAllClosed -= AskContinueOnce;
            ShowContinueAsk();
        }

        private void ShowContinueAsk()
        {
            SetContinueAskVisible(true);
        }

        /// 「要探索其他的東西嗎？」→ YES。留在房間，玩家自己找還沒點過的東西。
        public void OnContinueExploring()
        {
            SetContinueAskVisible(false);
        }

        /// 「要探索其他的東西嗎？」→ NO。走離開流程。
        public void OnChooseLeave()
        {
            SetContinueAskVisible(false);
            RequestExit();
        }

        // ==========================================
        // C14：離開（兩段式確認後才會走到這）
        // ==========================================
        public void RequestExit()
        {
            // C2：Stage 結束是自動回報，接著地圖會自己下拉
            ReportComplete(StageResult.Completed);
        }
    }
}
