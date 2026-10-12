using System.Collections.Generic;
using UnityEngine;

public enum EnemyStatusTargetMode
{
    AllOtherEnemies = 0,
    SelfOnly = 1,
    SpecificSlots = 2,
    RandomEnemy = 3,
    AllEnemies = 4
}

[CreateAssetMenu(menuName = "CardGame/Enemy Actions/Apply Status To Other Enemies")]
public class EnemyApplyStatusToOtherEnemiesActionData : EnemyActionData
{
    [Header("Status")]
    [Tooltip("要施加的狀態種類。")]
    public StatusType statusType = StatusType.Strength;

    [Tooltip("每個目標增加的狀態層數，沿用 BattleUnit.ApplyStatus 的疊加方式。")]
    public int amount = 1;

    [Header("Target")]
    [Tooltip("選擇只給自己、指定槽位、隨機一隻怪物、全部怪物或全部其他怪物。")]
    public EnemyStatusTargetMode targetMode = EnemyStatusTargetMode.AllOtherEnemies;

    [Tooltip("SpecificSlots 模式使用。填入 EnemySlotUI 的 Slot Image 物件名稱，例如 image1、image2、image3。可填多個，不區分大小寫。")]
    public List<string> specificSlotNames = new List<string>();

    public override void Execute(EnemyActionContext context)
    {
        if (context == null) return;
        if (context.enemy == null) return;

        if (targetMode == EnemyStatusTargetMode.SelfOnly)
        {
            ApplyStatusToTarget(context, context.enemy);
            return;
        }

        if (context.battleManager == null) return;
        if (context.battleManager.enemies == null) return;

        List<EnemyUnit> targets = new List<EnemyUnit>();

        for (int i = 0; i < context.battleManager.enemies.Count; i++)
        {
            EnemyUnit enemy = context.battleManager.enemies[i];

            if (!CanReceiveStatus(context, enemy)) continue;
            if (targets.Contains(enemy)) continue;

            switch (targetMode)
            {
                case EnemyStatusTargetMode.AllOtherEnemies:
                    if (enemy == context.enemy) continue;
                    break;

                case EnemyStatusTargetMode.SpecificSlots:
                    if (!IsSpecificSlotTarget(enemy)) continue;
                    break;

                case EnemyStatusTargetMode.RandomEnemy:
                case EnemyStatusTargetMode.AllEnemies:
                    break;

                default:
                    continue;
            }

            targets.Add(enemy);
        }

        if (targetMode == EnemyStatusTargetMode.RandomEnemy)
        {
            if (targets.Count == 0) return;

            int randomIndex = Random.Range(0, targets.Count);
            ApplyStatusToTarget(context, targets[randomIndex]);
            return;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            ApplyStatusToTarget(context, targets[i]);
        }
    }

    private bool CanReceiveStatus(EnemyActionContext context, EnemyUnit enemy)
    {
        if (enemy == null) return false;
        if (enemy == context.player) return false;
        if (enemy.isPlayerUnit) return false;
        if (!enemy.gameObject.activeInHierarchy) return false;
        if (enemy.currentHp <= 0) return false;

        return true;
    }

    private bool IsSpecificSlotTarget(EnemyUnit enemy)
    {
        if (specificSlotNames == null || specificSlotNames.Count == 0) return false;

        EnemySlotUI slot = enemy.GetComponentInParent<EnemySlotUI>();

        if (slot == null) return false;
        if (slot.slotImage == null) return false;

        string slotName = slot.slotImage.gameObject.name;

        for (int i = 0; i < specificSlotNames.Count; i++)
        {
            string configuredName = specificSlotNames[i];

            if (string.IsNullOrWhiteSpace(configuredName)) continue;
            if (string.Equals(slotName, configuredName.Trim(), System.StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }

    private void ApplyStatusToTarget(EnemyActionContext context, EnemyUnit enemy)
    {
        if (!CanReceiveStatus(context, enemy)) return;

        enemy.ApplyStatus(statusType, amount);
        enemy.RefreshAllUI();

        Debug.Log($"[EnemyAction] {context.enemy.unitName} 賦予 {enemy.unitName} {statusType} {amount} 層");
    }
}