using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Status Behaviours/Great Compassion")]
public class GreatCompassionStatusData : BattleStatusBehaviourData
{
    [Min(0)] public int playerHealAmount = 1;
    [Min(0)] public int sanLossAmount = 1;
    [Range(0f, 1f)] public float sanLossChance = 0.5f;

    public override StatusType SupportedStatus => StatusType.GreatCompassion;

    public override void OnAttacked(EnemyUnit enemy, BattleUnit attacker)
    {
        BattleManager manager = enemy.battleManager;

        if (manager == null || manager.playerUnit == null) return;
        if (attacker != manager.playerUnit) return;
        if (manager.playerUnit.currentHp <= 0) return;

        manager.playerUnit.Heal(playerHealAmount, enemy);

        if (manager.energySystem == null) return;
        if (Random.value >= sanLossChance) return;

        int actualSanLoss = Mathf.Min(sanLossAmount, manager.energySystem.currentEnergy);

        if (actualSanLoss > 0) manager.energySystem.Spend(actualSanLoss);
    }

    public override string GetDescription(EnemyUnit enemy, int amount)
    {
        return $"被玩家攻擊時，恢復玩家{playerHealAmount}點HP，並有{sanLossChance * 100f:0}%機率減少玩家{sanLossAmount}點SAN。";
    }
}