using UnityEngine;
using System.Collections.Generic;

public enum EnemyHealTargetMode
{
    AllOtherEnemies = 0,//治療其他全部怪物，不包含自己及玩家
    SelfOnly = 1,//只治療自己
    SpecificSlots = 2,//治療指定的 Image 槽位，可指定多個
    RandomEnemy = 3,//隨機治療一隻怪物，包含自己，不包含玩家
    AllEnemies = 4//治療全部怪物，包含自己，不包含玩家
}

[CreateAssetMenu(menuName = "CardGame/Enemy Actions/Heal")]
public class EnemyHealActionData : EnemyActionData
{
    [Header("Heal")]
    [Tooltip("每個目標恢復的基礎生命值，實際恢復量沿用 BattleUnit.Heal 的計算。")]
    public int healAmount = 10;

    [Header("Target")]
    [Tooltip("選擇只治療自己、指定槽位、隨機一隻怪物、全部怪物或全部其他怪物。")]
    public EnemyHealTargetMode targetMode = EnemyHealTargetMode.AllOtherEnemies;

    [Tooltip("SpecificSlots 模式使用。填入 EnemySlotUI 的 Slot Image 物件名稱，例如 image1、image2、image3。可填多個，不區分大小寫。")]
    public List<string> specificSlotNames = new List<string>();

    public override void Execute(EnemyActionContext context)
    {
        if (context == null) return;
        if (context.enemy == null) return;

        if (targetMode == EnemyHealTargetMode.SelfOnly)
        {
            HealTarget(context, context.enemy);
            return;
        }

        if (context.battleManager == null) return;
        if (context.battleManager.enemies == null) return;

        List<EnemyUnit> targets = new List<EnemyUnit>();

        for (int i = 0; i < context.battleManager.enemies.Count; i++)
        {
            EnemyUnit enemy = context.battleManager.enemies[i];

            if (!CanHealTarget(context, enemy)) continue;
            if (targets.Contains(enemy)) continue;

            switch (targetMode)
            {
                case EnemyHealTargetMode.AllOtherEnemies:
                    if (enemy == context.enemy) continue;
                    break;

                case EnemyHealTargetMode.SpecificSlots:
                    if (!IsSpecificSlotTarget(enemy)) continue;
                    break;

                case EnemyHealTargetMode.RandomEnemy:
                case EnemyHealTargetMode.AllEnemies:
                    break;

                default:
                    continue;
            }

            targets.Add(enemy);
        }

        if (targetMode == EnemyHealTargetMode.RandomEnemy)
        {
            if (targets.Count == 0) return;

            int randomIndex = Random.Range(0, targets.Count);
            HealTarget(context, targets[randomIndex]);
            return;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            HealTarget(context, targets[i]);
        }
    }

    private bool CanHealTarget(EnemyActionContext context, EnemyUnit enemy)
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

    private void HealTarget(EnemyActionContext context, EnemyUnit enemy)
    {
        if (!CanHealTarget(context, enemy)) return;

        enemy.Heal(healAmount);
        enemy.RefreshAllUI();

        Debug.Log($"[EnemyAction] {context.enemy.unitName} 治療 {enemy.unitName} {healAmount} 點生命");
    }
}