using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyReplacementController : MonoBehaviour
{
    [Serializable]
    public class ReplacementOption
    {
        [Tooltip("要替換成的怪物資料。")]
        public EnemyData enemyData;

        [Tooltip("這次替換是否使用 Slot 的 Boss UI。")]
        public bool useBossUI;
    }

    [Header("References")]
    public BattleManager battleManager;
    public EnemyFormationSpawner formationSpawner;

    [Tooltip("替換後重新套用目前 SAN 的燈光設定。未指定時會自動尋找。")]
    public EnergyLightPowerBinder energyLightPowerBinder;

    [Header("Replacement Candidates")]
    [Tooltip("可供 Debug 替換的怪物，以及對應使用的一般／Boss UI。")]
    public List<ReplacementOption> replacementOptions = new List<ReplacementOption>();

    [Header("Enemy Database")]
    [Tooltip("Debug 換怪清單使用的怪物總資料庫。")]
    public EnemyDatabase enemyDatabase;

    private string enemyIdSearch = "";

    private int selectedSlotIndex;
    private int selectedOptionIndex;
    private Vector2 optionScrollPosition;
    private string resultMessage;

    private void Awake()
    {
        AutoFindReferences();
    }

    private void AutoFindReferences()
    {
        if (battleManager == null) battleManager = FindFirstObjectByType<BattleManager>();

        if (formationSpawner == null && battleManager != null) formationSpawner = battleManager.enemyFormationSpawner;
        if (formationSpawner == null) formationSpawner = FindFirstObjectByType<EnemyFormationSpawner>();

        if (battleManager == null && formationSpawner != null) battleManager = formationSpawner.battleManager;

        if (energyLightPowerBinder == null) energyLightPowerBinder = FindFirstObjectByType<EnergyLightPowerBinder>();
    }

    public void DrawDebugGUI()
    {
        AutoFindReferences();

        GUILayout.Label("怪物替換");

        if (battleManager == null || formationSpawner == null)
        {
            GUILayout.Label("請指定 BattleManager 與 EnemyFormationSpawner。");
            return;
        }

        if (formationSpawner.enemySlots == null || formationSpawner.enemySlots.Count == 0)
        {
            GUILayout.Label("Spawner 沒有設定 Enemy Slots。");
            return;
        }

        selectedSlotIndex = Mathf.Clamp(selectedSlotIndex, 0, formationSpawner.enemySlots.Count - 1);

        GUILayout.Space(6);
        GUILayout.Label("選擇要替換的位置");

        for (int i = 0; i < formationSpawner.enemySlots.Count; i++)
        {
            EnemySlotUI slot = formationSpawner.enemySlots[i];
            EnemyUnit enemy = slot != null ? slot.enemyUnit : null;
            bool occupied = slot != null && slot.gameObject.activeInHierarchy && enemy != null && enemy.currentHp > 0;
            string enemyName = occupied ? enemy.unitName : "無存活怪物";
            string label = $"Slot {i}：{enemyName}";

            if (GUILayout.Toggle(selectedSlotIndex == i, label, "Button")) selectedSlotIndex = i;
        }

        GUILayout.Space(8);

        if (enemyDatabase != null) GUILayout.Label($"怪物資料庫：{enemyDatabase.name}");
        else GUILayout.Label("未指定 EnemyDatabase，目前只使用手動清單。");

        GUILayout.BeginHorizontal();
        GUILayout.Label("Enemy ID", GUILayout.Width(75));

        string newSearch = GUILayout.TextField(enemyIdSearch);

        if (GUILayout.Button("清除", GUILayout.Width(45))) newSearch = "";

        GUILayout.EndHorizontal();

        if (newSearch != enemyIdSearch)
        {
            enemyIdSearch = newSearch;
            optionScrollPosition = Vector2.zero;
            selectedOptionIndex = -1;
        }

        List<ReplacementOption> options = BuildReplacementOptions();

        if (options.Count == 0)
        {
            GUILayout.Label("沒有可替換的怪物，請指定已收集怪物的 EnemyDatabase。");
            return;
        }

        int matchingCount = 0;
        int firstMatchingIndex = -1;

        for (int i = 0; i < options.Count; i++)
        {
            if (!MatchesEnemyIdSearch(options[i].enemyData)) continue;

            if (firstMatchingIndex < 0) firstMatchingIndex = i;

            matchingCount++;
        }

        bool selectionMatches = selectedOptionIndex >= 0 && selectedOptionIndex < options.Count && MatchesEnemyIdSearch(options[selectedOptionIndex].enemyData);

        if (!selectionMatches) selectedOptionIndex = firstMatchingIndex;

        GUILayout.Space(4);
        GUILayout.Label($"符合搜尋：{matchingCount} / {options.Count}");

        optionScrollPosition = GUILayout.BeginScrollView(optionScrollPosition, GUILayout.Height(160));

        for (int i = 0; i < options.Count; i++)
        {
            ReplacementOption option = options[i];

            if (!MatchesEnemyIdSearch(option.enemyData)) continue;

            string enemyId = string.IsNullOrWhiteSpace(option.enemyData.enemyId) ? "未設定 ID" : option.enemyData.enemyId;
            string enemyName = option.enemyData.unitName;
            string uiName = option.useBossUI ? "Boss UI" : "一般 UI";
            string label = $"{enemyId} / {enemyName} / {uiName}";

            if (GUILayout.Toggle(selectedOptionIndex == i, label, "Button")) selectedOptionIndex = i;
        }

        if (matchingCount == 0) GUILayout.Label("找不到符合此 Enemy ID 的怪物。");

        GUILayout.EndScrollView();
        GUILayout.Space(8);

        bool hasSelection = selectedOptionIndex >= 0 && selectedOptionIndex < options.Count;

        if (hasSelection) GUILayout.Label($"準備替換為：{options[selectedOptionIndex].enemyData.unitName}");

        bool previousEnabled = GUI.enabled;
        GUI.enabled = previousEnabled && battleManager.CanReplaceEnemyForDebug && hasSelection;

        if (GUILayout.Button("替換選定位置的怪物", GUILayout.Height(32)))
        {
            ReplacementOption selectedOption = options[selectedOptionIndex];
            TryReplaceEnemyAtSlot(selectedSlotIndex, selectedOption.enemyData, selectedOption.useBossUI);
        }

        GUI.enabled = previousEnabled;

        if (!battleManager.CanReplaceEnemyForDebug) GUILayout.Label("請在玩家回合、出牌與回合切換結束後替換。");

        if (!string.IsNullOrEmpty(resultMessage)) GUILayout.Label(resultMessage);
    }

    public bool TryReplaceEnemy(int slotIndex, int optionIndex)
    {
        AutoFindReferences();

        List<ReplacementOption> options = BuildReplacementOptions();

        if (optionIndex < 0 || optionIndex >= options.Count) return Fail("替換清單索引無效。");

        ReplacementOption option = options[optionIndex];

        if (option == null || option.enemyData == null) return Fail("請先指定要替換的 EnemyData。");

        return TryReplaceEnemyAtSlot(slotIndex, option.enemyData, option.useBossUI);
    }

    public bool TryReplaceEnemyAtSlot(int slotIndex, EnemyData enemyData, bool useBossUI = false)
    {
        AutoFindReferences();

        if (battleManager == null || formationSpawner == null) return Fail("找不到 BattleManager 或 EnemyFormationSpawner。");
        if (!battleManager.CanReplaceEnemyForDebug) return Fail("目前不能替換，請等待玩家回合的結算完成。");
        if (enemyData == null) return Fail("EnemyData 沒有指定。");
        if (enemyData.maxHp <= 0) return Fail("新怪物的 Max HP 必須大於 0。");
        if (formationSpawner.enemySlots == null || slotIndex < 0 || slotIndex >= formationSpawner.enemySlots.Count) return Fail("Slot 索引無效。");

        EnemySlotUI slot = formationSpawner.enemySlots[slotIndex];

        if (slot == null || !slot.gameObject.activeInHierarchy) return Fail("這個 Slot 目前沒有啟用。");

        EnemyUnit enemy = slot.enemyUnit;

        if (enemy == null || !enemy.isActiveAndEnabled || enemy.currentHp <= 0) return Fail("這個 Slot 沒有可替換的存活怪物。");
        if (battleManager.enemies == null || !battleManager.enemies.Contains(enemy)) return Fail("這隻怪物沒有登記在目前的戰鬥清單。");

        string previousName = enemy.unitName;

        // 停止這個 Slot 尚未完成的舊外觀骨架重綁流程。
        slot.StopAllCoroutines();

        // 清除舊怪物執行中的動畫、蓄力及特殊狀態紀錄。
        enemy.PrepareForDebugReplacement();

        // 沿用既有 Slot 的清理與初始化流程，不呼叫死亡流程。
        slot.ClearSlot();
        enemy.battleManager = battleManager;

        EnemyUnit replacement = slot.SpawnEnemy(enemyData, useBossUI);

        if (replacement == null) return Fail("Slot 初始化失敗，請檢查 EnemyUnit 引用。");

        replacement.RefreshAllUI();
        battleManager.RefreshStatusUI();

        if (energyLightPowerBinder != null) energyLightPowerBinder.RefreshLightPower();

        resultMessage = $"Slot {slotIndex}：{previousName} → {replacement.unitName}";
        Debug.Log($"[EnemyReplacementController] {resultMessage}", this);

        return true;
    }

    private bool Fail(string message)
    {
        resultMessage = message;
        Debug.LogWarning($"[EnemyReplacementController] {message}", this);
        return false;
    }
    private List<ReplacementOption> BuildReplacementOptions()
    {
        List<ReplacementOption> options = new List<ReplacementOption>();

        if (enemyDatabase != null && enemyDatabase.enemies != null)
        {
            for (int i = 0; i < enemyDatabase.enemies.Count; i++)
            {
                EnemyData enemyData = enemyDatabase.enemies[i];

                if (enemyData == null) continue;

                bool alreadyAdded = false;

                for (int j = 0; j < options.Count; j++)
                {
                    if (options[j].enemyData != enemyData) continue;

                    alreadyAdded = true;
                    break;
                }

                if (alreadyAdded) continue;

                ReplacementOption option = new ReplacementOption();
                option.enemyData = enemyData;
                option.useBossUI = GetDefaultBossUI(enemyData);

                options.Add(option);
            }
        }

        if (replacementOptions != null)
        {
            for (int i = 0; i < replacementOptions.Count; i++)
            {
                ReplacementOption manualOption = replacementOptions[i];

                if (manualOption == null || manualOption.enemyData == null) continue;

                int existingIndex = -1;

                for (int j = 0; j < options.Count; j++)
                {
                    if (options[j].enemyData != manualOption.enemyData) continue;

                    existingIndex = j;
                    break;
                }

                if (existingIndex >= 0) options[existingIndex] = manualOption;
                else options.Add(manualOption);
            }
        }

        return options;
    }

    private bool GetDefaultBossUI(EnemyData enemyData)
    {
        if (enemyData == null || formationSpawner == null || formationSpawner.encounterPool == null) return false;
        if (formationSpawner.encounterPool.entries == null) return false;

        bool foundBoss = false;
        bool foundNormal = false;

        for (int i = 0; i < formationSpawner.encounterPool.entries.Count; i++)
        {
            EnemyEncounterPoolEntry poolEntry = formationSpawner.encounterPool.entries[i];

            if (poolEntry == null || poolEntry.formation == null || poolEntry.formation.enemies == null) continue;

            for (int j = 0; j < poolEntry.formation.enemies.Count; j++)
            {
                EnemySpawnEntry spawnEntry = poolEntry.formation.enemies[j];

                if (spawnEntry == null || spawnEntry.enemyData != enemyData) continue;

                if (poolEntry.isBoss) foundBoss = true;
                else foundNormal = true;
            }
        }

        return foundBoss && !foundNormal;
    }
    private bool MatchesEnemyIdSearch(EnemyData enemyData)
    {
        if (enemyData == null) return false;

        string keyword = enemyIdSearch.Trim();

        if (string.IsNullOrEmpty(keyword)) return true;

        string enemyId = enemyData.enemyId ?? "";

        return enemyId.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}