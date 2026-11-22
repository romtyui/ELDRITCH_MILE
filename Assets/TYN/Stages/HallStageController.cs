using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EldritchMile.Core;

/// <summary>
/// 大廳 Stage（2026-11-23 接進流程）。內容是 Romtyui `Assets/Romtyui_hall/test_Scene` 的快照，
/// 打包成 `Stage_Hall.prefab`，做法跟戰鬥一樣（他改了 test_Scene 就要重新打包）。
///
/// ────────────────────────────────────────────────────────
/// 【裡面有三個畫面，都是他做的，這一支不碰】
///   標題（MainMenuRoot）─開始→ MenuToHallTransition 鏡頭拉近轉場
///   → 大廳（Lobby/Hall）─點整備室的門→ RoomTransitionController
///   → 整備室（Lobby/Preparation Room）：選開局帶的食物 3 格、遺物 1 格
///
/// 【這一支只做膠水】
///   · 進場決定從標題開始，還是直接站在大廳（一輪結束回來時不用再看一次標題）
///   · 自帶透視相機 → 暫停宿主相機（<see cref="StageCameraHandover"/>）
///   · 整備室的出發鍵 → 把選好的食物／遺物交給 <see cref="GameFlowManager.StartNewRun(IList{string})"/>
///   · 標題的 Setting 鍵 → 開我們的系統選單（他那邊 settingsRoot 還沒做）
///   · 標題畫面藏起左上角齒輪（標題有自己的 Setting 鍵）
///
/// 大廳**不呼叫** ReportComplete —— 那是「這一站結束、地圖下拉」，這時候還沒有 run，也沒有地圖。
/// </summary>
public class HallStageController : StageController
{
    public override StageType Stage => StageType.Hall;

    /// <summary>
    /// 下一次進大廳要不要從標題畫面開始。由 GameFlowManager 在載入前設定，進場時讀掉並還原成 true。
    /// （跟 BattleStageController.PendingEnemyId 同一種做法：Stage 是每次新生成的，只能靠 static 傳話）
    /// </summary>
    public static bool OpenAtTitle = true;

    [Header("Romtyui 的物件（打包時自動填）")]
    [Tooltip("標題畫面總根物件（MainMenuRoot）")]
    public GameObject titleRoot;

    [Tooltip("大廳總根物件（Lobby），底下是 Hall 與 Preparation Room")]
    public GameObject lobbyRoot;

    public GameObject hallRoom;
    public GameObject preparationRoom;

    [Tooltip("他的透視相機")]
    public Camera stageCamera;

    [Tooltip("整備室的出發鍵（MainPanel/Button，他那邊還沒接任何事件）")]
    public Button departButton;

    [Tooltip("標題的 Setting 鍵")]
    public Button titleSettingButton;

    [Header("直接進大廳時的鏡頭（打包時從 MenuToHallTransition 抄過來）")]
    public Vector3 hallCameraPosition = new Vector3(0f, 1f, -10f);
    public Vector3 hallCameraEuler = Vector3.zero;
    public float hallCameraFov = 60f;

    [Header("除錯")]
    public bool verboseLog = true;

    private readonly StageCameraHandover handover = new StageCameraHandover();
    private bool gearHidden;
    private bool departing;

    public override void OnStageEnter(RunContext run)
    {
        bool atTitle = OpenAtTitle;
        OpenAtTitle = true;

        if (atTitle) ShowTitle();
        else ShowHallDirectly();

        if (handover.Take(transform) && verboseLog)
        {
            var names = new List<string>();
            foreach (Camera c in handover.Suspended) names.Add(c.name);
            Debug.Log($"[大廳] 自帶相機，暫停宿主相機：{string.Join("、", names)}", this);
        }

        if (departButton != null) departButton.onClick.AddListener(OnDepartClicked);
        else Debug.LogWarning("[大廳] 沒有出發鍵，選完食物遺物出不去", this);

        if (titleSettingButton != null)
        {
            // 他的 MainMenuUI.Awake 掛了 OnSettingClicked，settingsRoot 沒填會一直噴警告。
            // RemoveAllListeners 只拆程式掛的（他那一支），Inspector 裡拉的不受影響
            titleSettingButton.onClick.RemoveAllListeners();
            titleSettingButton.onClick.AddListener(OpenSettings);
        }

        if (verboseLog) Debug.Log($"[大廳] 進場（{(atTitle ? "從標題開始" : "直接站在大廳")}）", this);
    }

    private void ShowTitle()
    {
        if (titleRoot != null) titleRoot.SetActive(true);
        if (lobbyRoot != null) lobbyRoot.SetActive(false);
        SetGear(false);
    }

    private void ShowHallDirectly()
    {
        if (titleRoot != null) titleRoot.SetActive(false);

        if (stageCamera != null)
        {
            stageCamera.transform.SetPositionAndRotation(hallCameraPosition, Quaternion.Euler(hallCameraEuler));
            if (!stageCamera.orthographic) stageCamera.fieldOfView = hallCameraFov;
        }

        if (preparationRoom != null) preparationRoom.SetActive(false);
        if (hallRoom != null) hallRoom.SetActive(true);
        if (lobbyRoot != null) lobbyRoot.SetActive(true);
        SetGear(true);
    }

    private void Update()
    {
        // 標題那段轉場是他的協程，結束時沒有對外的事件可接 —— 看標題關掉了沒就好
        if (gearHidden && titleRoot != null && !titleRoot.activeSelf) SetGear(true);
    }

    // ==========================================
    // 按鈕
    // ==========================================

    public void OnDepartClicked()
    {
        if (departing) return;

        var flow = GameFlowManager.Instance;
        if (flow == null)
        {
            Debug.LogWarning("[大廳] 場上沒有 GameFlowManager，出發不了", this);
            return;
        }
        if (flow.IsTransitioning) return;

        var picked = new List<string>();
        if (StartingFoodLoadout.Instance != null) picked.AddRange(StartingFoodLoadout.Instance.CreateFoodIdSnapshot());
        if (StartingRelicLoadout.Instance != null) picked.AddRange(StartingRelicLoadout.Instance.CreateRelicIdSnapshot());
        picked.RemoveAll(string.IsNullOrEmpty);

        if (verboseLog) Debug.Log($"[大廳] 出發，帶著：{(picked.Count > 0 ? string.Join("、", picked) : "（什麼都沒帶）")}", this);

        departing = true;
        flow.StartNewRun(picked);
    }

    private void OpenSettings()
    {
        var menu = EldritchMile.UI.SystemMenuUI.Instance;
        if (menu == null || menu.option == null)
        {
            Debug.LogWarning("[大廳] 找不到系統選單，Setting 鍵沒有作用", this);
            return;
        }
        menu.option.OpenPauseMenu();
    }

    private void SetGear(bool visible)
    {
        var menu = EldritchMile.UI.SystemMenuUI.Instance;
        if (menu == null) return;
        menu.SetGearVisible(visible);
        gearHidden = !visible;
    }

    // ==========================================
    // 離場
    // ==========================================

    public override IEnumerator OnStageExit()
    {
        Cleanup();
        yield break;
    }

    /// <summary>沒走 OnStageExit 就被銷毀（停止播放）時一樣要還回去，不然下一站一片黑。可以重複呼叫</summary>
    private void OnDestroy()
    {
        Cleanup();
    }

    private void Cleanup()
    {
        handover.Restore();
        if (gearHidden) SetGear(true);
    }
}
