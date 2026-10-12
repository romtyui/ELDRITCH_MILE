using System.Collections.Generic;
using UnityEngine;

public enum EnemyBlockTargetMode
{
    SelfOnly = 0,
    SpecificSlots = 1,
    RandomEnemy = 2,
    AllEnemies = 3,
    AllOtherEnemies = 4
}

[CreateAssetMenu(menuName = "CardGame/Enemy Actions/Gain Block")]
public class EnemyGainBlockActionData : EnemyActionData
{
    [Header("Block")]
    [Tooltip("每個目標獲得的基礎格擋，實際數值沿用 BattleUnit.GainBlock 的脆弱及 Modifier 修正。")]
    public int amount = 5;

    [Header("Target")]
    [Tooltip("選擇只給自己、指定槽位、隨機一隻怪物、全部怪物或全部其他怪物。")]
    public EnemyBlockTargetMode targetMode = EnemyBlockTargetMode.SelfOnly;

    [Tooltip("SpecificSlots 模式使用。填入 EnemySlotUI 的 Slot Image 物件名稱，例如 image1、image2、image3。可填多個，不區分大小寫。")]
    public List<string> specificSlotNames = new List<string>();

    public override void Execute(EnemyActionContext context)
    {
        if (context == null) return;
        if (context.enemy == null) return;

        if (targetMode == EnemyBlockTargetMode.SelfOnly)
        {
            GiveBlockToTarget(context, context.enemy);
            return;
        }

        if (context.battleManager == null) return;
        if (context.battleManager.enemies == null) return;

        List<EnemyUnit> targets = new List<EnemyUnit>();

        for (int i = 0; i < context.battleManager.enemies.Count; i++)
        {
            EnemyUnit enemy = context.battleManager.enemies[i];

            if (!CanReceiveBlock(context, enemy)) continue;
            if (targets.Contains(enemy)) continue;

            switch (targetMode)
            {
                case EnemyBlockTargetMode.SpecificSlots:
                    if (!IsSpecificSlotTarget(enemy)) continue;
                    break;

                case EnemyBlockTargetMode.AllOtherEnemies:
                    if (enemy == context.enemy) continue;
                    break;

                case EnemyBlockTargetMode.RandomEnemy:
                case EnemyBlockTargetMode.AllEnemies:
                    break;

                default:
                    continue;
            }

            targets.Add(enemy);
        }

        if (targetMode == EnemyBlockTargetMode.RandomEnemy)
        {
            if (targets.Count == 0) return;

            int randomIndex = Random.Range(0, targets.Count);
            GiveBlockToTarget(context, targets[randomIndex]);
            return;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            GiveBlockToTarget(context, targets[i]);
        }
    }

    private bool CanReceiveBlock(EnemyActionContext context, EnemyUnit enemy)
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

    private void GiveBlockToTarget(EnemyActionContext context, EnemyUnit enemy)
    {
        if (!CanReceiveBlock(context, enemy)) return;

        enemy.GainBlock(amount);
        enemy.RefreshAllUI();

        Debug.Log($"{context.enemy.unitName} 給予 {enemy.unitName} 基礎格擋 {amount}");
    }
}