using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemyEncounterPoolEntry
{
    [Header("Formation")]
    public EnemyFormationData formation;

    [Header("Spawn Rules")]
    [Min(0)]
    public int weight = 1;

    [Tooltip("勾選後，這組怪物只有在普通怪物組合都出現過後才會出現")]
    public bool isBoss;
}

[CreateAssetMenu(menuName = "CardGame/Enemy/Encounter Pool")]
public class EnemyEncounterPoolData : ScriptableObject
{
    [Header("Formations")]
    public List<EnemyEncounterPoolEntry> entries = new();

    private readonly HashSet<EnemyFormationData> usedFormations = new();

    [Header("Debug Runtime Only")]
    [SerializeField] private List<string> debugUsedFormationNames = new();

    private void OnEnable()
    {
        ResetRuntimeUsedFormations();
    }

    public EnemyFormationData GetRandomFormation()
    {
        List<EnemyEncounterPoolEntry> normalCandidates = GetUnusedNormalEntries();

        if (normalCandidates.Count > 0)
        {
            EnemyEncounterPoolEntry entry = GetWeightedRandomEntry(normalCandidates);

            if (entry == null || entry.formation == null)
                return null;

            MarkFormationUsed(entry.formation);

            Debug.Log($"[EnemyEncounterPoolData] 抽到普通怪物組合：{entry.formation.formationName}");

            return entry.formation;
        }

        List<EnemyEncounterPoolEntry> bossCandidates = GetUnusedBossEntries();

        if (bossCandidates.Count > 0)
        {
            EnemyEncounterPoolEntry entry = GetWeightedRandomEntry(bossCandidates);

            if (entry == null || entry.formation == null)
                return null;

            MarkFormationUsed(entry.formation);

            Debug.Log($"[EnemyEncounterPoolData] 普通組合已用完，抽到 Boss 組合：{entry.formation.formationName}");

            return entry.formation;
        }

        Debug.LogWarning($"[EnemyEncounterPoolData] {name} 沒有可用的怪物組合");
        return null;
    }

    public bool IsFormationUsed(EnemyFormationData formation)
    {
        if (formation == null)
            return false;

        return usedFormations.Contains(formation);
    }

    public void MarkFormationUsed(EnemyFormationData formation)
    {
        if (formation == null)
            return;

        usedFormations.Add(formation);
        RefreshDebugUsedFormationNames();
    }

    public EnemyEncounterPoolEntry GetWeightedRandomEntry(List<EnemyEncounterPoolEntry> candidates)
    {
        if (candidates == null || candidates.Count == 0)
            return null;

        int totalWeight = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i] == null)
                continue;

            totalWeight += Mathf.Max(0, candidates[i].weight);
        }

        if (totalWeight <= 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, candidates.Count);
            Debug.LogWarning("[EnemyEncounterPoolData] 候選組合總權重為 0，改用等機率隨機抽選");
            return candidates[randomIndex];
        }

        int roll = UnityEngine.Random.Range(0, totalWeight);

        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i] == null)
                continue;

            int weight = Mathf.Max(0, candidates[i].weight);

            if (roll < weight)
                return candidates[i];

            roll -= weight;
        }

        return candidates[candidates.Count - 1];
    }

    private List<EnemyEncounterPoolEntry> GetUnusedNormalEntries()
    {
        List<EnemyEncounterPoolEntry> candidates = new();

        for (int i = 0; i < entries.Count; i++)
        {
            EnemyEncounterPoolEntry entry = entries[i];

            if (!IsValidEntry(entry))
                continue;

            if (entry.isBoss)
                continue;

            if (IsFormationUsed(entry.formation))
                continue;

            candidates.Add(entry);
        }

        return candidates;
    }

    private List<EnemyEncounterPoolEntry> GetUnusedBossEntries()
    {
        List<EnemyEncounterPoolEntry> candidates = new();

        for (int i = 0; i < entries.Count; i++)
        {
            EnemyEncounterPoolEntry entry = entries[i];

            if (!IsValidEntry(entry))
                continue;

            if (!entry.isBoss)
                continue;

            if (IsFormationUsed(entry.formation))
                continue;

            candidates.Add(entry);
        }

        return candidates;
    }

    private bool IsValidEntry(EnemyEncounterPoolEntry entry)
    {
        if (entry == null)
            return false;

        if (entry.formation == null)
            return false;

        return true;
    }

    public void ResetRuntimeUsedFormations()
    {
        usedFormations.Clear();
        RefreshDebugUsedFormationNames();

        Debug.Log($"[EnemyEncounterPoolData] Runtime 已出現怪物組合已清空：{name}");
    }

    [ContextMenu("Reset Runtime Used Formations")]
    public void ContextResetRuntimeUsedFormations()
    {
        ResetRuntimeUsedFormations();
    }

    private void RefreshDebugUsedFormationNames()
    {
        debugUsedFormationNames.Clear();

        foreach (EnemyFormationData formation in usedFormations)
        {
            if (formation == null)
                continue;

            debugUsedFormationNames.Add(formation.formationName);
        }
    }
}