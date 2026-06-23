using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BattleDebugHotkeys : MonoBehaviour
{
    [Header("References")]
    public BattleManager battleManager;
    public GameObject battleManagerObject;

    [Header("Hotkeys")]
    public Key killAllEnemiesKey = Key.K;
    public Key enableBattleManagerKey = Key.B;

    [Tooltip("開關狀態 Debug UI")]
    public Key toggleStatusDebugUIKey = Key.F1;

    [Header("Settings")]
    public int debugDamage = 9999;

    [Header("Debug")]
    public bool enableDebugHotkeys = true;

    [Header("Status Debug UI")]
    public bool showStatusDebugUI;
    public bool includeInactiveEnemies;
    public int statusAmount = 1;

    private Rect statusWindowRect = new Rect(30, 80, 360, 680);
    private Vector2 targetScrollPosition;

    private readonly List<BattleUnit> debugTargets = new();

    private int selectedTargetIndex;
    private int selectedStatusIndex;

    private StatusType[] statusTypes;

    private void Awake()
    {
        AutoFindRefs();
        statusTypes = (StatusType[])Enum.GetValues(typeof(StatusType));
        RefreshDebugTargets();
    }

    private void Update()
    {
        if (!enableDebugHotkeys)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (keyboard[killAllEnemiesKey] != null &&
            keyboard[killAllEnemiesKey].wasPressedThisFrame)
        {
            KillAllEnemies();
        }

        if (keyboard[enableBattleManagerKey] != null &&
            keyboard[enableBattleManagerKey].wasPressedThisFrame)
        {
            EnableBattleManagerObject();
        }

        if (keyboard[toggleStatusDebugUIKey] != null &&
            keyboard[toggleStatusDebugUIKey].wasPressedThisFrame)
        {
            ToggleStatusDebugUI();
        }
    }

    private void OnGUI()
    {
        if (!enableDebugHotkeys)
            return;

        if (!showStatusDebugUI)
            return;

        statusWindowRect = GUI.Window(
            8721,
            statusWindowRect,
            DrawStatusDebugWindow,
            "Status Debug UI"
        );
    }

    private void DrawStatusDebugWindow(int windowId)
    {
        GUILayout.Space(6);

        GUILayout.Label($"快捷鍵：{toggleStatusDebugUIKey} 開 / 關");

        GUILayout.Space(8);

        if (GUILayout.Button("重新抓取場景目標"))
        {
            RefreshDebugTargets();
        }

        includeInactiveEnemies = GUILayout.Toggle(
            includeInactiveEnemies,
            "包含未啟用怪物"
        );

        GUILayout.Space(8);

        DrawTargetSection();

        GUILayout.Space(8);

        DrawStatusSection();

        GUILayout.Space(8);

        DrawActionSection();

        GUILayout.Space(8);

        DrawSelectedTargetStatusPreview();

        GUI.DragWindow();
    }

    private void DrawTargetSection()
    {
        GUILayout.Label("選擇目標");

        if (debugTargets.Count == 0)
        {
            GUILayout.Label("目前沒有可用目標");
            return;
        }

        selectedTargetIndex = Mathf.Clamp(
            selectedTargetIndex,
            0,
            debugTargets.Count - 1
        );

        targetScrollPosition = GUILayout.BeginScrollView(
            targetScrollPosition,
            GUILayout.Height(150)
        );

        for (int i = 0; i < debugTargets.Count; i++)
        {
            BattleUnit target = debugTargets[i];

            if (target == null)
                continue;

            string label = BuildTargetLabel(target);

            bool isSelected = selectedTargetIndex == i;

            if (GUILayout.Toggle(isSelected, label, "Button"))
            {
                selectedTargetIndex = i;
            }
        }

        GUILayout.EndScrollView();
    }

    private void DrawStatusSection()
    {
        GUILayout.Label("選擇狀態");

        if (statusTypes == null || statusTypes.Length == 0)
        {
            GUILayout.Label("StatusType 沒有資料");
            return;
        }

        selectedStatusIndex = Mathf.Clamp(
            selectedStatusIndex,
            0,
            statusTypes.Length - 1
        );

        GUILayout.BeginVertical("box");

        for (int i = 0; i < statusTypes.Length; i++)
        {
            bool isSelected = selectedStatusIndex == i;

            if (GUILayout.Toggle(isSelected, statusTypes[i].ToString(), "Button"))
            {
                selectedStatusIndex = i;
            }
        }

        GUILayout.EndVertical();

        GUILayout.Space(6);

        GUILayout.BeginHorizontal();
        GUILayout.Label("層數", GUILayout.Width(50));

        string amountText = GUILayout.TextField(statusAmount.ToString(), GUILayout.Width(80));

        if (int.TryParse(amountText, out int parsedAmount))
            statusAmount = Mathf.Max(1, parsedAmount);

        if (GUILayout.Button("-"))
            statusAmount = Mathf.Max(1, statusAmount - 1);

        if (GUILayout.Button("+"))
            statusAmount++;

        GUILayout.EndHorizontal();
    }

    private void DrawActionSection()
    {
        BattleUnit target = GetSelectedTarget();

        GUI.enabled = target != null;

        if (GUILayout.Button("套用狀態到目標"))
        {
            ApplySelectedStatusToTarget();
        }

        if (GUILayout.Button("清除目標所有狀態"))
        {
            ClearSelectedTargetStatuses();
        }

        GUI.enabled = true;
    }

    private void DrawSelectedTargetStatusPreview()
    {
        BattleUnit target = GetSelectedTarget();

        GUILayout.Label("目標目前狀態");

        if (target == null)
        {
            GUILayout.Label("未選擇目標");
            return;
        }

        Dictionary<StatusType, int> statuses = target.GetAllStatuses();

        if (statuses == null || statuses.Count == 0)
        {
            GUILayout.Label("沒有狀態");
            return;
        }

        GUILayout.BeginVertical("box");

        foreach (var pair in statuses)
        {
            GUILayout.Label($"{pair.Key} x{pair.Value}");
        }

        GUILayout.EndVertical();
    }

    private void ToggleStatusDebugUI()
    {
        showStatusDebugUI = !showStatusDebugUI;

        if (showStatusDebugUI)
        {
            AutoFindRefs();
            RefreshDebugTargets();
        }
    }

    private void RefreshDebugTargets()
    {
        debugTargets.Clear();

        AutoFindRefs();

        if (battleManager != null && battleManager.playerUnit != null)
        {
            debugTargets.Add(battleManager.playerUnit);
        }
        else
        {
            BattleUnit[] allUnits = FindObjectsByType<BattleUnit>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            for (int i = 0; i < allUnits.Length; i++)
            {
                BattleUnit unit = allUnits[i];

                if (unit == null)
                    continue;

                if (unit is EnemyUnit)
                    continue;

                debugTargets.Add(unit);
                break;
            }
        }

        EnemyUnit[] enemies = FindObjectsByType<EnemyUnit>(
            includeInactiveEnemies ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyUnit enemy = enemies[i];

            if (enemy == null)
                continue;

            if (!includeInactiveEnemies && !enemy.gameObject.activeInHierarchy)
                continue;

            if (!debugTargets.Contains(enemy))
                debugTargets.Add(enemy);
        }

        selectedTargetIndex = Mathf.Clamp(
            selectedTargetIndex,
            0,
            Mathf.Max(0, debugTargets.Count - 1)
        );

        Debug.Log($"[BattleDebugHotkeys] Status Debug 重新抓取目標，數量 = {debugTargets.Count}");
    }

    private BattleUnit GetSelectedTarget()
    {
        if (debugTargets == null || debugTargets.Count == 0)
            return null;

        selectedTargetIndex = Mathf.Clamp(
            selectedTargetIndex,
            0,
            debugTargets.Count - 1
        );

        return debugTargets[selectedTargetIndex];
    }

    private StatusType GetSelectedStatusType()
    {
        if (statusTypes == null || statusTypes.Length == 0)
            return StatusType.Strength;

        selectedStatusIndex = Mathf.Clamp(
            selectedStatusIndex,
            0,
            statusTypes.Length - 1
        );

        return statusTypes[selectedStatusIndex];
    }

    private void ApplySelectedStatusToTarget()
    {
        BattleUnit target = GetSelectedTarget();

        if (target == null)
        {
            Debug.LogWarning("[BattleDebugHotkeys] 沒有選擇目標，無法套用狀態");
            return;
        }

        StatusType statusType = GetSelectedStatusType();

        target.ApplyStatus(statusType, statusAmount);

        Debug.Log($"[BattleDebugHotkeys] 對 {target.unitName} 套用狀態 {statusType} x{statusAmount}");

        RefreshBattleUI();
    }

    private void ClearSelectedTargetStatuses()
    {
        BattleUnit target = GetSelectedTarget();

        if (target == null)
        {
            Debug.LogWarning("[BattleDebugHotkeys] 沒有選擇目標，無法清除狀態");
            return;
        }

        target.ClearAllStatuses();

        Debug.Log($"[BattleDebugHotkeys] 清除 {target.unitName} 所有狀態");

        RefreshBattleUI();
    }

    private void RefreshBattleUI()
    {
        if (battleManager != null && battleManager.gameObject.activeInHierarchy)
        {
            battleManager.RefreshStatusUI();
        }

        EnemyUnit[] enemies = FindObjectsByType<EnemyUnit>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null)
                enemies[i].RefreshAllUI();
        }
    }

    private string BuildTargetLabel(BattleUnit target)
    {
        if (target == null)
            return "null";

        string typeLabel = target is EnemyUnit ? "敵人" : "玩家";
        string activeLabel = target.gameObject.activeInHierarchy ? "" : " / Inactive";

        return $"{typeLabel}：{target.unitName} HP {target.currentHp}/{target.maxHp}{activeLabel}";
    }

    private void AutoFindRefs()
    {
        if (battleManager == null)
            battleManager = FindFirstObjectByType<BattleManager>(FindObjectsInactive.Include);

        if (battleManagerObject == null && battleManager != null)
            battleManagerObject = battleManager.gameObject;
    }

    [ContextMenu("Debug Kill All Enemies")]
    public void KillAllEnemies()
    {
        AutoFindRefs();

        EnemyUnit[] enemies = FindObjectsByType<EnemyUnit>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        Debug.Log($"[BattleDebugHotkeys] 對場景所有怪物造成 {debugDamage} 傷害，數量 = {enemies.Length}");

        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyUnit enemy = enemies[i];

            if (enemy == null)
                continue;

            if (!enemy.gameObject.activeInHierarchy)
                continue;

            if (enemy.currentHp <= 0)
                continue;

            enemy.TakeDamage(debugDamage);
        }

        if (battleManager != null && battleManager.gameObject.activeInHierarchy)
        {
            battleManager.RequestCheckBattleEnd();
        }
    }

    [ContextMenu("Debug Enable BattleManager")]
    public void EnableBattleManagerObject()
    {
        AutoFindRefs();

        if (battleManagerObject == null)
        {
            Debug.LogWarning("[BattleDebugHotkeys] battleManagerObject 沒有指定");
            return;
        }

        if (battleManagerObject.activeSelf)
        {
            Debug.Log("[BattleDebugHotkeys] BattleManager 物件已經是開啟狀態");
            return;
        }

        Debug.Log("[BattleDebugHotkeys] 開啟 BattleManager 物件");

        battleManagerObject.SetActive(true);
    }
}