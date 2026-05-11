using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy/Enemy Encounter Pool")]
public class EnemyEncounterPoolData : ScriptableObject
{
    [Header("Possible Formations")]
    public List<EnemyFormationData> formations = new();

    public EnemyFormationData GetRandomFormation()
    {
        if (formations == null || formations.Count == 0)
            return null;

        int index = Random.Range(0, formations.Count);
        return formations[index];
    }
}