using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy/Enemy Formation")]
public class EnemyFormationData : ScriptableObject
{
    public string formationName;
    public List<EnemySpawnEntry> enemies = new();

    [Header("金幣獎勵")]
    [Min(0)]
    [Tooltip("最多生成幾欄。0 不生成；大於 0 時隨機生成 1～設定值欄。")]
    public int moneyRewardMaxRows;

    [Tooltip("每欄金幣數量的最小與最大值，包含兩端。")]
    public Vector2Int moneyRewardRange = new Vector2Int(10, 20);

    [Header("武器獎勵")]
    [Min(0)]
    [Tooltip("最多生成幾欄。0 不生成；大於 0 時隨機生成 1～設定值欄。")]
    public int weaponRewardMaxRows = 1;

    [Min(1)]
    [Tooltip("每欄的候選卡牌張數。同一欄不重複，卡池不足時顯示實際張數。")]
    public int weaponChoiceCount = 3;

    [Header("遺物獎勵")]
    [Min(0)]
    [Tooltip("最多生成幾欄。0 不生成；大於 0 時隨機生成 1～設定值欄。")]
    public int relicRewardMaxRows;

    [Min(1)]
    [Tooltip("指定遺物 tire。設定 2 就只抽 tire 為 2 的遺物，不改抽其他等級。")]
    public int relicRewardMaxLevel = 1;
}

[System.Serializable]
public class EnemySpawnEntry
{
    [Tooltip("0 = MonsterPos_1/Image, 1 = MonsterPos_2/Image, 2 = MonsterPos_3/Image")]
    public int spawnIndex;

    public EnemyData enemyData;
}