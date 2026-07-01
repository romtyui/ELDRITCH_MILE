using System.Collections.Generic;
using UnityEngine;

public class EnemyFormationSpawner : MonoBehaviour
{
    [Header("Encounter")]
    public EnemyEncounterPoolData encounterPool;

    [Header("Stage Spawn Rule")]
    [Tooltip("false：模式一，使用 EncounterPool 原本規則。true：模式二，使用目前關卡權重限制。")]
    public bool useStageWeightRule = false;

    [Tooltip("模式二使用。這一關只能出現 weight 小於等於這個數值的怪物組合。")]
    public int currentStageMaxWeight = 1;

    [Tooltip("模式二使用。false：只抽普通怪。true：只抽 Boss。")]
    public bool currentStageIsBoss = false;

    [Header("Enemy Slots")]
    public List<EnemySlotUI> enemySlots = new();

    [Header("Battle")]
    public BattleManager battleManager;

    [Header("Debug Runtime")]
    [SerializeField] private EnemyFormationData debugCurrentFormation;
    [SerializeField] private List<string> debugSpawnedEnemyNames = new();
    [SerializeField] private List<string> debugCandidateNames = new();
    [SerializeField] private string debugSpawnMode;

    private readonly List<EnemyUnit> spawnedEnemies = new();

    [ContextMenu("Spawn Random Formation")]
    public void SpawnRandomFormation()
    {
        EnemyFormationData formation = null;

        if (encounterPool == null)
        {
            Debug.LogWarning("[EnemyFormationSpawner] encounterPool 沒有指定");
            return;
        }

        // 1. 如果有保留的怪物組，優先使用保留組
        // 這通常代表：玩家中途離開遊戲後重新進入戰鬥
        if (RunStateManager.Instance != null &&
            RunStateManager.Instance.TryGetReservedFormation(out formation))
        {
            if (formation != null)
            {
                debugSpawnMode = "保留戰鬥：使用上次保留的怪物組";

                Debug.Log($"[EnemyFormationSpawner] 使用保留怪物組：{formation.name}");

                SpawnFormation(formation);
                return;
            }
        }

        // 2. 沒有保留怪物組，才走你原本的抽怪規則
        if (useStageWeightRule)
        {
            formation = GetFormationByStageRule();
        }
        else
        {
            debugSpawnMode = "模式一：EncounterPool 原本規則";
            formation = encounterPool.GetRandomFormation();
        }

        if (formation == null)
        {
            Debug.LogWarning("[EnemyFormationSpawner] 沒有可用的怪物組合");
            return;
        }

        // 3. 新抽到的怪物組先保留起來
        // 注意：這裡只是保留，不代表消耗怪物池
        if (RunStateManager.Instance != null)
        {
            RunStateManager.Instance.ReserveFormation(formation);
        }

        SpawnFormation(formation);
    }

    private EnemyFormationData GetFormationByStageRule()
    {
        debugSpawnMode = currentStageIsBoss
            ? $"模式二：Boss 關，MaxWeight = {currentStageMaxWeight}"
            : $"模式二：普通/菁英關，MaxWeight = {currentStageMaxWeight}";

        debugCandidateNames.Clear();

        List<EnemyEncounterPoolEntry> candidates = new();

        for (int i = 0; i < encounterPool.entries.Count; i++)
        {
            EnemyEncounterPoolEntry entry = encounterPool.entries[i];

            if (entry == null)
                continue;

            if (entry.formation == null)
                continue;

            if (encounterPool.IsFormationUsed(entry.formation))
                continue;

            if (entry.weight > currentStageMaxWeight)
                continue;

            if (currentStageIsBoss)
            {
                if (!entry.isBoss)
                    continue;
            }
            else
            {
                if (entry.isBoss)
                    continue;
            }

            candidates.Add(entry);

            string typeText = entry.isBoss ? "Boss" : "Normal";
            debugCandidateNames.Add($"{typeText} / W{entry.weight} / {entry.formation.formationName}");
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning(
                $"[EnemyFormationSpawner] 模式二沒有可用組合。BossOnly = {currentStageIsBoss}, MaxWeight = {currentStageMaxWeight}"
            );

            return null;
        }

        EnemyEncounterPoolEntry selectedEntry = encounterPool.GetWeightedRandomEntry(candidates);

        if (selectedEntry == null || selectedEntry.formation == null)
            return null;

        encounterPool.MarkFormationUsed(selectedEntry.formation);

        Debug.Log(
            $"[EnemyFormationSpawner] 模式二抽到組合：{selectedEntry.formation.formationName}, Weight = {selectedEntry.weight}, Boss = {selectedEntry.isBoss}"
        );

        return selectedEntry.formation;
    }

    public void SpawnFormation(EnemyFormationData formation)
    {
        if (formation == null)
            return;

        debugCurrentFormation = formation;
        debugSpawnedEnemyNames.Clear();

        ClearSlots();

        spawnedEnemies.Clear();

        for (int i = 0; i < formation.enemies.Count; i++)
        {
            EnemySpawnEntry entry = formation.enemies[i];

            if (entry == null)
                continue;

            if (entry.enemyData == null)
            {
                Debug.LogWarning($"[EnemyFormationSpawner] {formation.formationName} 有空的 enemyData");
                continue;
            }

            if (entry.spawnIndex < 0 || entry.spawnIndex >= enemySlots.Count)
            {
                Debug.LogWarning($"[EnemyFormationSpawner] spawnIndex 超出範圍：{entry.spawnIndex}");
                continue;
            }

            EnemySlotUI slot = enemySlots[entry.spawnIndex];

            if (slot == null)
                continue;

            EnemyUnit enemy = slot.SpawnEnemy(entry.enemyData);

            if (enemy != null)
            {
                spawnedEnemies.Add(enemy);
                debugSpawnedEnemyNames.Add($"{entry.spawnIndex}: {enemy.unitName}");
            }
        }

        RegisterEnemiesToBattleManager();

        Debug.Log($"[EnemyFormationSpawner] 生成怪物組合：{formation.formationName}");
    }

    private void ClearSlots()
    {
        for (int i = 0; i < enemySlots.Count; i++)
        {
            if (enemySlots[i] != null)
                enemySlots[i].ClearSlot();
        }

        spawnedEnemies.Clear();

        if (battleManager != null)
        {
            battleManager.enemies.Clear();
            battleManager.currentEnemy = null;
        }
    }

    private void RegisterEnemiesToBattleManager()
    {
        if (battleManager == null)
        {
            Debug.LogWarning("[EnemyFormationSpawner] battleManager 沒有指定");
            return;
        }

        battleManager.enemies.Clear();

        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            EnemyUnit enemy = spawnedEnemies[i];

            if (enemy == null)
                continue;

            if (!battleManager.enemies.Contains(enemy))
                battleManager.enemies.Add(enemy);
        }

        battleManager.currentEnemy = battleManager.enemies.Count > 0
            ? battleManager.enemies[0]
            : null;

        Debug.Log($"[EnemyFormationSpawner] 已登記 {battleManager.enemies.Count} 隻怪物");
    }

    [ContextMenu("Reset Encounter Pool Used Formations")]
    public void ResetEncounterPoolUsedFormations()
    {
        if (encounterPool != null)
            encounterPool.ResetRuntimeUsedFormations();
    }

    public void SetStageSpawnRule(bool enableRule, int maxWeight, bool isBossStage)
    {
        useStageWeightRule = enableRule;
        currentStageMaxWeight = Mathf.Max(0, maxWeight);
        currentStageIsBoss = isBossStage;

        Debug.Log(
            $"[EnemyFormationSpawner] 設定關卡怪物規則：useStageWeightRule = {useStageWeightRule}, MaxWeight = {currentStageMaxWeight}, IsBoss = {currentStageIsBoss}"
        );
    }
}