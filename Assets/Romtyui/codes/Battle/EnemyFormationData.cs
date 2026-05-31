using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy/Enemy Formation")]
public class EnemyFormationData : ScriptableObject
{
    public string formationName;
    public List<EnemySpawnEntry> enemies = new();
}

[System.Serializable]
public class EnemySpawnEntry
{
    [Tooltip("0 = MonsterPos_1/Image, 1 = MonsterPos_2/Image, 2 = MonsterPos_3/Image")]
    public int spawnIndex;

    public EnemyData enemyData;
}