using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Status/Statuses/Evolution")]
public class EvolutionStatusData : BattleStatusBehaviourData
{
    public override StatusType SupportedStatus => StatusType.Evolution;

    [Min(0)]
    [Tooltip("此怪物每成功使玩家隨機捨棄一張手牌，增加的最大HP。")]
    public int maxHpPerCard = 2;

    public override void OnSuccessfullyDiscardedHandCards(EnemyUnit enemy, int amount)
    {
        if (enemy == null || enemy.currentHp <= 0) return;
        if (!enemy.gameObject.activeInHierarchy) return;
        if (!enemy.HasStatus(SupportedStatus)) return;
        if (amount <= 0) return;

        int maxHpGain = amount * Mathf.Max(0, maxHpPerCard);

        if (maxHpGain <= 0) return;

        enemy.maxHp += maxHpGain;
        enemy.NotifyUnitChanged();
        enemy.RefreshAllUI();

        Debug.Log($"[Evolution] {enemy.unitName} 成功使玩家捨棄 {amount} 張手牌，最大HP增加 {maxHpGain}，目前HP：{enemy.currentHp}/{enemy.maxHp}");
    }

    public override string GetDescription(EnemyUnit enemy, int amount)
    {
        return $"當玩家的手牌被此怪物隨機捨棄時，每成功捨棄1張，自身最大HP增加{Mathf.Max(0, maxHpPerCard)}。";
    }
}