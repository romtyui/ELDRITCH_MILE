using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class BattleDebugHotkeys : MonoBehaviour
{
    [Header("References")]
    public BattleManager battleManager;
    public GameObject battleManagerObject;

    [Header("Hotkeys")]
    public Key killAllEnemiesKey = Key.K;
    public Key enableBattleManagerKey = Key.B;

    [Tooltip("開關 Debug UI")]
    public Key toggleStatusDebugUIKey = Key.F1;

    [Tooltip("保存目前紀錄並載入指定場景")]
    public Key loadSceneWithSaveKey = Key.F5;
    [Tooltip("不通關，保留目前怪物組並重新載入指定場景")]
    public Key reloadSceneWithoutCommitKey = Key.F6;

    [Header("Settings")]
    public int debugDamage = 9999;
    [Header("Scene Load Debug")]
    [Tooltip("按下指定按鍵後要載入的場景名稱。場景必須加入 Build Settings。")]
    public string debugSceneName = "BattleScene";

    [Tooltip("載入場景前是否先保存目前 HP / SAN / 牌組紀錄")]
    public bool saveRunStateBeforeLoadScene = true;

    [Header("Debug")]
    public bool enableDebugHotkeys = true;

    [Header("Status Debug UI")]
    public bool showStatusDebugUI;
    public bool includeInactiveEnemies;
    public int statusAmount = 1;

    [Header("Card Debug UI")]
    public AllCardDatabase allCardDatabase;

    [Tooltip("可以額外手動指定 CardData")]
    public List<CardData> debugAddableCards = new();

    public int addCardAmount = 1;

    [Tooltip("是否自動把 BattleDeck.startingDeck 裡的牌加入 Debug 清單")]
    //public bool includeStartingDeckCards = true;

    //public int addCardAmount = 1;

    private Rect statusWindowRect = new Rect(30, 80, 420, 680);

    private Vector2 targetScrollPosition;
    private Vector2 statusScrollPosition;
    private Vector2 cardScrollPosition;
    private Vector2 cardTabScrollPosition;

    private readonly List<BattleUnit> debugTargets = new();
    private readonly List<CardData> runtimeCardList = new();

    private int selectedTargetIndex;
    private int selectedStatusIndex;
    private int selectedCardIndex;

    private StatusType[] statusTypes;

    private int selectedTabIndex;
    private readonly string[] tabNames = new string[]
    {
        "狀態",
        "加牌"
    };

    private void Awake()
    {
        AutoFindRefs();

        statusTypes = (StatusType[])Enum.GetValues(typeof(StatusType));

        RefreshDebugTargets();
        RefreshDebugCardList();
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

        if (keyboard[loadSceneWithSaveKey] != null &&
    keyboard[loadSceneWithSaveKey].wasPressedThisFrame)
        {
            DebugCommitBattleAndLoadScene();
        }
        if (keyboard[reloadSceneWithoutCommitKey] != null &&
    keyboard[reloadSceneWithoutCommitKey].wasPressedThisFrame)
        {
            DebugReloadSceneFromBattleStartDeckSnapshot();
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
            DrawDebugWindow,
            "Battle Debug UI"
        );
    }

    private void DrawDebugWindow(int windowId)
    {
        GUILayout.Space(6);

        GUILayout.Label($"快捷鍵：{toggleStatusDebugUIKey} 開 / 關");

        GUILayout.Space(8);

        selectedTabIndex = GUILayout.Toolbar(selectedTabIndex, tabNames);

        GUILayout.Space(8);

        switch (selectedTabIndex)
        {
            case 0:
                DrawStatusDebugTab();
                break;

            case 1:
                DrawCardDebugTab();
                break;
        }

        GUI.DragWindow();
    }

    // =========================================================
    // Status Tab
    // =========================================================

    private void DrawStatusDebugTab()
    {
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
            GUILayout.Height(130)
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

        statusScrollPosition = GUILayout.BeginScrollView(
            statusScrollPosition,
            GUILayout.Height(130)
        );

        GUILayout.BeginVertical("box");

        for (int i = 0; i < statusTypes.Length; i++)
        {
            bool isSelected = selectedStatusIndex == i;
            string statusName = GetStatusDisplayName(statusTypes[i]);

            if (GUILayout.Toggle(isSelected, statusName, "Button"))
            {
                selectedStatusIndex = i;
            }
        }

        GUILayout.EndVertical();

        GUILayout.EndScrollView();

        GUILayout.Space(6);

        GUILayout.BeginHorizontal();

        GUILayout.Label("層數", GUILayout.Width(50));

        string amountText = GUILayout.TextField(
            statusAmount.ToString(),
            GUILayout.Width(80)
        );

        if (int.TryParse(amountText, out int parsedAmount))
            statusAmount = Mathf.Max(1, parsedAmount);

        if (GUILayout.Button("-", GUILayout.Width(40)))
            statusAmount = Mathf.Max(1, statusAmount - 1);

        if (GUILayout.Button("+", GUILayout.Width(40)))
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
            GUILayout.Label($"{GetStatusDisplayName(pair.Key)} x{pair.Value}");
        }

        GUILayout.EndVertical();
    }

    // =========================================================
    // Card Tab
    // =========================================================

    private void DrawCardDebugTab()
    {
        cardTabScrollPosition = GUILayout.BeginScrollView(
            cardTabScrollPosition,
            GUILayout.ExpandHeight(true)
        );

        GUILayout.Label("加牌到手牌");

        GUILayout.Space(4);

        if (GUILayout.Button("重新整理卡牌清單"))
        {
            RefreshDebugCardList();
        }

        if (allCardDatabase != null)
        {
            int count = allCardDatabase.cards != null ? allCardDatabase.cards.Count : 0;
            GUILayout.Label($"AllCardDatabase：{count} 張卡");
        }
        else
        {
            GUILayout.Label("AllCardDatabase：未指定");
        }

        GUILayout.Space(8);

        if (runtimeCardList.Count == 0)
        {
            GUILayout.Label("目前沒有可加入的卡牌。");
            GUILayout.Label("請指定 AllCardDatabase，或把 CardData 拖到 Debug Addable Cards。");

            GUILayout.EndScrollView();
            return;
        }

        selectedCardIndex = Mathf.Clamp(
            selectedCardIndex,
            0,
            runtimeCardList.Count - 1
        );

        GUILayout.Label("選擇卡牌");

        cardScrollPosition = GUILayout.BeginScrollView(
            cardScrollPosition,
            GUILayout.Height(240)
        );

        for (int i = 0; i < runtimeCardList.Count; i++)
        {
            CardData cardData = runtimeCardList[i];

            if (cardData == null)
                continue;

            bool isSelected = selectedCardIndex == i;

            string cardName = string.IsNullOrWhiteSpace(cardData.cardName)
                ? cardData.name
                : cardData.cardName;

            string label = $"{cardName} / Cost {cardData.baseCost} / {cardData.cardType}";

            if (GUILayout.Toggle(isSelected, label, "Button"))
            {
                selectedCardIndex = i;
            }
        }

        GUILayout.EndScrollView();

        GUILayout.Space(8);

        DrawAddCardAmountSection();

        GUILayout.Space(8);

        DrawSelectedCardPreview();

        GUILayout.Space(8);

        GUI.enabled = GetSelectedCardData() != null &&
                      battleManager != null &&
                      battleManager.gameObject.activeInHierarchy;

        if (GUILayout.Button("加入手牌", GUILayout.Height(32)))
        {
            AddSelectedCardToHand();
        }

        GUI.enabled = true;

        GUILayout.Space(20);

        GUILayout.EndScrollView();
    }
    private void DrawAddCardAmountSection()
    {
        GUILayout.BeginHorizontal();

        GUILayout.Label("數量", GUILayout.Width(50));

        string amountText = GUILayout.TextField(
            addCardAmount.ToString(),
            GUILayout.Width(80)
        );

        if (int.TryParse(amountText, out int parsedAmount))
            addCardAmount = Mathf.Max(1, parsedAmount);

        if (GUILayout.Button("-", GUILayout.Width(40)))
            addCardAmount = Mathf.Max(1, addCardAmount - 1);

        if (GUILayout.Button("+", GUILayout.Width(40)))
            addCardAmount++;

        GUILayout.EndHorizontal();
    }

    private void DrawSelectedCardPreview()
    {
        CardData cardData = GetSelectedCardData();

        GUILayout.Label("選中卡牌");

        if (cardData == null)
        {
            GUILayout.Label("未選擇卡牌");
            return;
        }

        GUILayout.BeginVertical("box");

        string cardName = string.IsNullOrWhiteSpace(cardData.cardName)
            ? cardData.name
            : cardData.cardName;

        GUILayout.Label($"名稱：{cardName}");
        GUILayout.Label($"費用：{cardData.baseCost}");
        GUILayout.Label($"類型：{cardData.cardType}");
        GUILayout.Label($"目標：{cardData.targetType}");

        if (!string.IsNullOrWhiteSpace(cardData.description))
            GUILayout.Label($"描述：{cardData.description}");

        GUILayout.EndVertical();
    }

    private void RefreshDebugCardList()
    {
        runtimeCardList.Clear();

        AutoFindRefs();

        if (allCardDatabase != null && allCardDatabase.cards != null)
        {
            for (int i = 0; i < allCardDatabase.cards.Count; i++)
            {
                AddCardToRuntimeList(allCardDatabase.cards[i]);
            }
        }

        if (debugAddableCards != null)
        {
            for (int i = 0; i < debugAddableCards.Count; i++)
            {
                AddCardToRuntimeList(debugAddableCards[i]);
            }
        }

        selectedCardIndex = Mathf.Clamp(
            selectedCardIndex,
            0,
            Mathf.Max(0, runtimeCardList.Count - 1)
        );

        Debug.Log($"[BattleDebugHotkeys] Card Debug 重新整理卡牌清單，數量 = {runtimeCardList.Count}");
    }

    private void AddCardToRuntimeList(CardData cardData)
    {
        if (cardData == null)
            return;

        if (runtimeCardList.Contains(cardData))
            return;

        runtimeCardList.Add(cardData);
    }

    private CardData GetSelectedCardData()
    {
        if (runtimeCardList == null || runtimeCardList.Count == 0)
            return null;

        selectedCardIndex = Mathf.Clamp(
            selectedCardIndex,
            0,
            runtimeCardList.Count - 1
        );

        return runtimeCardList[selectedCardIndex];
    }

    private void AddSelectedCardToHand()
    {
        AutoFindRefs();

        if (battleManager == null)
        {
            Debug.LogWarning("[BattleDebugHotkeys] battleManager 是 null，無法加牌");
            return;
        }

        CardData cardData = GetSelectedCardData();

        if (cardData == null)
        {
            Debug.LogWarning("[BattleDebugHotkeys] 沒有選擇卡牌，無法加牌");
            return;
        }

        int finalAmount = Mathf.Max(1, addCardAmount);

        for (int i = 0; i < finalAmount; i++)
        {
            battleManager.AddCardToHand(cardData);
        }

        string cardName = string.IsNullOrWhiteSpace(cardData.cardName)
            ? cardData.name
            : cardData.cardName;

        Debug.Log($"[BattleDebugHotkeys] 加入手牌：{cardName} x{finalAmount}");
    }

    // =========================================================
    // Status Logic
    // =========================================================

    private void ToggleStatusDebugUI()
    {
        showStatusDebugUI = !showStatusDebugUI;

        if (showStatusDebugUI)
        {
            AutoFindRefs();
            RefreshDebugTargets();
            RefreshDebugCardList();
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

        Debug.Log($"[BattleDebugHotkeys] 對 {target.unitName} 套用狀態 {GetStatusDisplayName(statusType)} x{statusAmount}");

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

    private string GetStatusDisplayName(StatusType statusType)
    {
        switch (statusType)
        {
            case StatusType.Strength:
                return "力量";

            case StatusType.TemporaryStrength:
                return "臨時力量";

            case StatusType.Weak:
                return "虛弱";

            case StatusType.Vulnerable:
                return "易傷";

            case StatusType.Frail:
                return "脆弱";

            case StatusType.Poison:
                return "中毒";

            default:
                return statusType.ToString();
        }
    }

    private void AutoFindRefs()
    {
        if (battleManager == null)
            battleManager = FindFirstObjectByType<BattleManager>(FindObjectsInactive.Include);

        if (battleManagerObject == null && battleManager != null)
            battleManagerObject = battleManager.gameObject;
    }

    // =========================================================
    // Old Debug Hotkeys
    // =========================================================

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
    // =========================================================
    // Scene Load Debug
    // =========================================================

    [ContextMenu("Debug Load Scene With Run State")]
    [ContextMenu("Debug Commit Battle And Load Scene")]
    public void DebugCommitBattleAndLoadScene()
    {
        AutoFindRefs();

        if (string.IsNullOrWhiteSpace(debugSceneName))
        {
            Debug.LogWarning("[BattleDebugHotkeys] debugSceneName 是空的，無法載入場景");
            return;
        }

        if (battleManager == null)
        {
            Debug.LogWarning("[BattleDebugHotkeys] battleManager 是 null，無法保存戰鬥紀錄");
            return;
        }

        if (RunStateManager.Instance != null)
        {
            // F5 = 模擬通關，所以要清除保留怪物組
            RunStateManager.Instance.ClearReservedFormation();

            // F5 = 保存玩家目前狀態
            RunStateManager.Instance.SaveFromBattle(
                battleManager.playerUnit,
                battleManager.energySystem,
                battleManager.playerDeck
            );

            Debug.Log("[BattleDebugHotkeys] F5 已保存目前玩家狀態，並清除保留怪物組");
        }
        else
        {
            Debug.LogWarning("[BattleDebugHotkeys] 場景中沒有 RunStateManager，無法保存紀錄");
        }

        Time.timeScale = 1f;

        Debug.Log($"[BattleDebugHotkeys] F5 模擬通關，載入場景：{debugSceneName}");

        SceneManager.LoadScene(debugSceneName);
    }

    private void SaveRunStateForDebugSceneLoad()
    {
        AutoFindRefs();

        if (RunStateManager.Instance == null)
        {
            Debug.LogWarning("[BattleDebugHotkeys] 場景中沒有 RunStateManager，無法保存紀錄。請確認已建立 RunStateManager 物件。");
            return;
        }

        if (battleManager == null)
        {
            Debug.LogWarning("[BattleDebugHotkeys] battleManager 是 null，無法保存紀錄");
            return;
        }

        RunStateManager.Instance.SaveFromBattle(
            battleManager.playerUnit,
            battleManager.energySystem,
            battleManager.playerDeck
        );

        Debug.Log("[BattleDebugHotkeys] 已在載入場景前保存 RunState");
    }
    [ContextMenu("Debug Reload Scene From Battle Start Deck Snapshot")]
    public void DebugReloadSceneFromBattleStartDeckSnapshot()
    {
        AutoFindRefs();

        if (string.IsNullOrWhiteSpace(debugSceneName))
        {
            Debug.LogWarning("[BattleDebugHotkeys] debugSceneName 是空的，無法載入場景");
            return;
        }

        if (RunStateManager.Instance == null)
        {
            Debug.LogWarning("[BattleDebugHotkeys] 沒有 RunStateManager，無法使用戰鬥開始牌組快照");
            return;
        }

        if (RunStateManager.Instance.battleStartDeckSnapshot == null ||
            !RunStateManager.Instance.battleStartDeckSnapshot.hasSnapshot)
        {
            Debug.LogWarning("[BattleDebugHotkeys] 還沒有戰鬥開始牌組快照，請先進入戰鬥");
            return;
        }

        RunStateManager.Instance.pendingRestoreBattleStartDeckSnapshot = true;

        Time.timeScale = 1f;

        Debug.Log($"[BattleDebugHotkeys] F6 使用戰鬥開始牌組快照重新載入場景：{debugSceneName}");

        SceneManager.LoadScene(debugSceneName);
    }
}