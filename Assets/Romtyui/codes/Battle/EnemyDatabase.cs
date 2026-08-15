using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy/Enemy Database")]
public class EnemyDatabase : ScriptableObject
{
    public List<EnemyData> enemies = new();

    public EnemyData GetById(string enemyId)
    {
        if (string.IsNullOrWhiteSpace(enemyId))
            return null;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyData enemy = enemies[i];

            if (enemy == null)
                continue;

            if (enemy.enemyId == enemyId)
                return enemy;
        }

        return null;
    }
}