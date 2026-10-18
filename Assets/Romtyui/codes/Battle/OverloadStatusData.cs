using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Status/Statuses/Overload")]
public class OverloadStatusData : BattleStatusBehaviourData
{
    public override StatusType SupportedStatus => StatusType.Overload;

    public override bool SelectsIntentAtPlayerTurnEnd => false;

    [Header("Normal Cycle")]
    [Tooltip("正常行動順序。可以重複放入相同意圖，例如：蠻力、魚叉穿刺、魚叉穿刺。所有意圖也必須放入 EnemyData.Intents。")]
    public List<EnemyIntentData> normalIntentCycle = new List<EnemyIntentData>();

    [Header("Overload")]
    [Range(0.01f, 1f)]
    [Tooltip("同一個玩家回合內，累計失去至少最大 HP 的這個比例時，下一次行動改成疲勞。0.3 表示 30%，所需失血量向上取整。護盾損失不計入，回血不抵銷累計失血。")]
    public float hpLossThresholdRatio = 0.3f;

    [Tooltip("過負荷後執行的疲勞意圖。也必須放入 EnemyData.Intents。執行後從正常循環第一個意圖重新開始。")]
    public EnemyIntentData fatigueIntent;

    public override void OnSpawn(EnemyUnit enemy)
    {
        if (enemy == null) return;

        enemy.InitializeOverloadTracking();

        if (!ValidateConfiguration(enemy)) return;

        TrySelectNormalIntent(enemy);
    }

    public override bool ControlsNextIntent(EnemyUnit enemy)
    {
        return enemy != null && enemy.HasStatus(SupportedStatus);
    }

    public override void OnPlayerTurnEnd(EnemyUnit enemy)
    {
        if (enemy == null) return;
        if (!enemy.HasStatus(SupportedStatus)) return;

        EnemyUnit.OverloadRuntimeState state = enemy.overloadRuntime;
        int hpLost = state.hpLostThisPlayerTurn;

        state.hpLostThisPlayerTurn = 0;
        state.previousHp = enemy.currentHp;

        if (enemy.currentHp <= 0 || !enemy.gameObject.activeInHierarchy) return;
        if (enemy.isCharging || enemy.HasChargeBrokenStunQueued()) return;

        int requiredHpLoss = Mathf.Max(1, Mathf.CeilToInt(enemy.maxHp * Mathf.Clamp(hpLossThresholdRatio, 0.01f, 1f)));

        if (hpLost < requiredHpLoss) return;
        if (!ValidateConfiguration(enemy)) return;
        if (!enemy.TrySetStatusIntent(fatigueIntent)) return;

        // 疲勞執行完成後，從正常循環第一個意圖重新開始。
        state.nextIntentIndex = 0;

        Debug.Log($"[Overload] {enemy.unitName} 在本次玩家回合失去 {hpLost} HP，達到門檻 {requiredHpLoss}，下一次行動改為疲勞");
    }

    public override bool TrySelectNextIntentAtTurnEnd(EnemyUnit enemy)
    {
        if (enemy == null) return false;

        TrySelectNormalIntent(enemy);

        // 設定錯誤時保留目前意圖，避免退回整份 Intents 循環。
        return true;
    }

    private bool TrySelectNormalIntent(EnemyUnit enemy)
    {
        if (!ValidateConfiguration(enemy)) return false;

        EnemyUnit.OverloadRuntimeState state = enemy.overloadRuntime;
        int index = state.nextIntentIndex;

        if (index < 0 || index >= normalIntentCycle.Count) index = 0;
        if (!enemy.TrySetStatusIntent(normalIntentCycle[index])) return false;

        state.nextIntentIndex = (index + 1) % normalIntentCycle.Count;

        return true;
    }

    private bool ValidateConfiguration(EnemyUnit enemy)
    {
        if (normalIntentCycle == null || normalIntentCycle.Count == 0)
        {
            Debug.LogWarning($"[Overload] {enemy.unitName} 沒有設定正常行動循環");
            return false;
        }

        if (fatigueIntent == null || enemy.intents == null || !enemy.intents.Contains(fatigueIntent))
        {
            Debug.LogWarning($"[Overload] {enemy.unitName} 的疲勞意圖未設定，或沒有放入 EnemyData.Intents");
            return false;
        }

        for (int i = 0; i < normalIntentCycle.Count; i++)
        {
            EnemyIntentData intent = normalIntentCycle[i];

            if (intent == null || !enemy.intents.Contains(intent))
            {
                Debug.LogWarning($"[Overload] {enemy.unitName} 的正常循環第 {i} 項為空，或沒有放入 EnemyData.Intents");
                return false;
            }
        }

        return true;
    }

    public override string GetDescription(EnemyUnit enemy, int amount)
    {
        float percent = Mathf.Clamp(hpLossThresholdRatio, 0.01f, 1f) * 100f;

        return $"玩家回合結束時，若本回合累計失去至少最大 HP 的 {percent:0.##}%，下次行動改為疲勞。疲勞後從正常循環第一個行動重新開始。";
    }
}