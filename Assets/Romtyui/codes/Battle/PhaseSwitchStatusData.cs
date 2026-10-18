using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Status/Statuses/Phase Switch")]
public class PhaseSwitchStatusData : BattleStatusBehaviourData
{
    public override StatusType SupportedStatus => StatusType.PhaseSwitch;

    public override bool SelectsIntentAtPlayerTurnEnd => false;

    [Header("Phase Switch Conditions")]
    [Min(1)]
    [Tooltip("從怪物出場後開始計算，第幾次玩家回合結束起允許切換階段。設為 3，表示第三次玩家回合結束時首次檢查。")]
    public int minimumPlayerTurnEnds = 3;

    [Tooltip("是否同時要求自身護盾嚴格大於指定門檻。關閉後只檢查回合數。")]
    public bool requireBlockCondition = true;

    [Min(0)]
    [Tooltip("自身護盾必須嚴格大於這個值才符合條件。設為 10 時，護盾至少需要 11。")]
    public int blockThreshold = 10;

    [Header("Phase Intents")]
    [Tooltip("第一階段依序循環的意圖。這隻怪物只放第一階段的縮殼。所有意圖也必須放進 EnemyData.Intents。")]
    public List<EnemyIntentData> firstPhaseIntents = new List<EnemyIntentData>();

    [Tooltip("第二階段依序循環的意圖。切換當次從第一個開始。所有意圖也必須放進 EnemyData.Intents。")]
    public List<EnemyIntentData> secondPhaseIntents = new List<EnemyIntentData>();

    [Header("Phase Switch Reward")]
    [Min(0)]
    [Tooltip("進入第二階段時獲得的力量，只觸發一次。")]
    public int strengthGainOnSwitch = 2;

    [Header("Animator")]
    [Tooltip("亮、暗怪物外觀 Animator 中，用於進入第二階段的 Bool 名稱。新生成外觀的這個 Bool 預設值應為 false。")]
    public string secondPhaseBoolName = "IsOpen";

    public override void OnSpawn(EnemyUnit enemy)
    {
        if (enemy == null) return;

        enemy.phaseSwitchRuntime = new EnemyUnit.PhaseSwitchRuntimeState();

        if (!ValidateIntentList(enemy, firstPhaseIntents, "第一階段")) return;

        TrySelectPhaseIntent(enemy);
    }

    public override bool ControlsNextIntent(EnemyUnit enemy)
    {
        return enemy != null && enemy.HasStatus(SupportedStatus);
    }

    public override void OnPlayerTurnEnd(EnemyUnit enemy)
    {
        if (enemy == null || enemy.currentHp <= 0) return;
        if (!enemy.HasStatus(SupportedStatus)) return;

        EnemyUnit.PhaseSwitchRuntimeState state = enemy.phaseSwitchRuntime;

        if (state.isSecondPhase) return;

        state.playerTurnEndCount++;

        if (state.playerTurnEndCount < Mathf.Max(1, minimumPlayerTurnEnds)) return;
        if (!enemy.CanSwitchStatusPhase) return;
        if (requireBlockCondition && enemy.block <= Mathf.Max(0, blockThreshold)) return;
        if (!ValidateIntentList(enemy, secondPhaseIntents, "第二階段")) return;

        // 先確認能選到第二階段第一招，成功後才正式切換。
        if (!enemy.TrySetStatusIntent(secondPhaseIntents[0])) return;

        state.isSecondPhase = true;
        state.nextIntentIndex = secondPhaseIntents.Count > 1 ? 1 : 0;

        if (strengthGainOnSwitch > 0) enemy.ApplyStatus(StatusType.Strength, strengthGainOnSwitch);

        EnemySlotUI slot = enemy.GetComponent<EnemySlotUI>();

        if (slot != null) slot.SetVisualAnimatorBool(secondPhaseBoolName, true);

        enemy.RefreshAllUI();

        Debug.Log($"[PhaseSwitch] {enemy.unitName} 進入第二階段，玩家回合結束次數：{state.playerTurnEndCount}，護盾：{enemy.block}");
    }

    public override bool TrySelectNextIntentAtTurnEnd(EnemyUnit enemy)
    {
        if (enemy == null) return false;

        TrySelectPhaseIntent(enemy);

        // 此狀態負責階段內循環。
        // 設定錯誤時維持目前意圖，避免退回整份 Intents 循環而跨階段。
        return true;
    }

    private bool TrySelectPhaseIntent(EnemyUnit enemy)
    {
        EnemyUnit.PhaseSwitchRuntimeState state = enemy.phaseSwitchRuntime;
        List<EnemyIntentData> phaseIntents = state.isSecondPhase ? secondPhaseIntents : firstPhaseIntents;
        string phaseName = state.isSecondPhase ? "第二階段" : "第一階段";

        if (!ValidateIntentList(enemy, phaseIntents, phaseName)) return false;

        int index = state.nextIntentIndex;

        if (index < 0 || index >= phaseIntents.Count) index = 0;
        if (!enemy.TrySetStatusIntent(phaseIntents[index])) return false;

        state.nextIntentIndex = (index + 1) % phaseIntents.Count;

        return true;
    }

    private bool ValidateIntentList(EnemyUnit enemy, List<EnemyIntentData> phaseIntents, string phaseName)
    {
        if (phaseIntents == null || phaseIntents.Count == 0)
        {
            Debug.LogWarning($"[PhaseSwitch] {enemy.unitName} 的{phaseName}意圖清單未設定");
            return false;
        }

        for (int i = 0; i < phaseIntents.Count; i++)
        {
            EnemyIntentData intent = phaseIntents[i];

            if (intent == null)
            {
                Debug.LogWarning($"[PhaseSwitch] {enemy.unitName} 的{phaseName}意圖清單第 {i} 項為空");
                return false;
            }

            if (enemy.intents == null || !enemy.intents.Contains(intent))
            {
                Debug.LogWarning($"[PhaseSwitch] {enemy.unitName} 的{phaseName}意圖 {intent.name} 沒有放入 EnemyData.Intents");
                return false;
            }
        }

        return true;
    }

    public override string GetDescription(EnemyUnit enemy, int amount)
    {
        string condition = requireBlockCondition ? $"，且自身護盾大於 {Mathf.Max(0, blockThreshold)}" : "";
        string reward = strengthGainOnSwitch > 0 ? $"，並獲得 {strengthGainOnSwitch} 層力量" : "";
        bool isSecondPhase = enemy != null && enemy.phaseSwitchRuntime.isSecondPhase;
        string phaseText = isSecondPhase ? "目前為第二階段。" : "目前為第一階段。";

        return $"第 {Mathf.Max(1, minimumPlayerTurnEnds)} 次玩家回合結束起{condition}，進入第二階段{reward}。只觸發一次。\n{phaseText}";
    }
}