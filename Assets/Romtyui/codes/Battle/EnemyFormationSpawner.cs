using System.Collections.Generic;
using UnityEngine;

public class EnemyFormationSpawner : MonoBehaviour
{
    [Header("Encounter")]
    public EnemyEncounterPoolData encounterPool;

    [Header("Enemy Slots")]
    public List<EnemySlotUI> enemySlots = new();

    [Header("Battle")]
    public BattleManager battleManager;

    private readonly List<EnemyUnit> spawnedEnemies = new();

    [ContextMenu("Spawn Random Formation")]
    public void SpawnRandomFormation()
    {
        EnemyFormationData formation = encounterPool != null
            ? encounterPool.GetRandomFormation()
            : null;

        if (formation == null)
        {
            Debug.LogWarning("[EnemyFormationSpawner] 沒有可用的怪物組合");
            return;
        }

        SpawnFormation(formation);
    }

    public void SpawnFormation(EnemyFormationData formation)
    {
        if (formation == null)
            return;

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
                spawnedEnemies.Add(enemy);
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
}