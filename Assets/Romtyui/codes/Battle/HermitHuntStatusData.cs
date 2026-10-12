using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Status/Statuses/Hermit Hunt")]
public class HermitHuntStatusData : BattleStatusBehaviourData
{
    public override StatusType SupportedStatus => StatusType.HermitHunt;

    [Header("First Intent")]
    [Tooltip("持有此狀態的怪物出場後，第一個行動使用的意圖。此意圖也必須放入怪物的 Intents。")]
    public EnemyIntentData firstIntent;

    [Header("Block Conditions")]
    [Tooltip("自身回合結束時，護盾為 0，下一次使用的意圖。")]
    public EnemyIntentData zeroBlockIntent;

    [Tooltip("自身回合結束時，護盾嚴格大於門檻，下一次使用的意圖。")]
    public EnemyIntentData highBlockIntent;

    [Min(0)]
    [Tooltip("護盾嚴格大於此值時使用 High Block Intent。")]
    public int blockThreshold = 10;

    [Header("Random Intents")]
    [Tooltip("護盾大於 0，且未超過門檻時，從這些意圖中隨機選擇。每個意圖也必須放入怪物的 Intents。")]
    public List<EnemyIntentData> randomIntents = new List<EnemyIntentData>();

    public override void OnSpawn(EnemyUnit enemy)
    {
        if (enemy == null) return;
        if (!enemy.HasStatus(SupportedStatus)) return;

        if (!enemy.TrySetStatusIntent(firstIntent))
        {
            Debug.LogWarning($"[HermitHunt] {enemy.unitName} 的第一個意圖未設定，或沒有放入怪物的 Intents");
        }
    }

    public override bool ControlsNextIntent(EnemyUnit enemy)
    {
        return enemy != null && enemy.HasStatus(SupportedStatus);
    }

    public override bool TrySelectNextIntentAtTurnEnd(EnemyUnit enemy)
    {
        if (enemy == null) return false;
        if (!enemy.HasStatus(SupportedStatus)) return false;

        if (enemy.block <= 0)
        {
            return enemy.TrySetStatusIntent(zeroBlockIntent);
        }

        int threshold = Mathf.Max(0, blockThreshold);

        if (enemy.block > threshold)
        {
            return enemy.TrySetStatusIntent(highBlockIntent);
        }

        if (randomIntents == null || randomIntents.Count == 0) return false;
        if (enemy.intents == null || enemy.intents.Count == 0) return false;

        List<EnemyIntentData> validIntents = new List<EnemyIntentData>();

        for (int i = 0; i < randomIntents.Count; i++)
        {
            EnemyIntentData intent = randomIntents[i];

            if (intent == null) continue;
            if (!enemy.intents.Contains(intent)) continue;
            if (validIntents.Contains(intent)) continue;

            validIntents.Add(intent);
        }

        if (validIntents.Count == 0) return false;

        int randomIndex = Random.Range(0, validIntents.Count);

        return enemy.TrySetStatusIntent(validIntents[randomIndex]);
    }

    public override string GetDescription(EnemyUnit enemy, int amount)
    {
        int threshold = Mathf.Max(0, blockThreshold);
        string firstName = GetIntentName(firstIntent);
        string zeroName = GetIntentName(zeroBlockIntent);
        string highName = GetIntentName(highBlockIntent);

        return $"第一個行動使用「{firstName}」。\n自身回合結束時，護盾為 0，下一次使用「{zeroName}」。\n護盾大於 {threshold}，下一次使用「{highName}」。\n其他情況從指定意圖中隨機選擇。";
    }

    private string GetIntentName(EnemyIntentData intent)
    {
        if (intent == null) return "未設定";
        if (string.IsNullOrWhiteSpace(intent.intentName)) return intent.name;

        return intent.intentName;
    }
}