using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Status/Statuses/Carrion Feast")]
public class CarrionFeastStatusData : BattleStatusBehaviourData
{
    public override StatusType SupportedStatus => StatusType.CarrionFeast;

    [Range(0f, 1f)]
    [Tooltip("每成功使玩家隨機捨棄一張手牌，恢復自身最大HP的比例。")]
    public float healMaxHpRatioPerCard = 0.1f;

    public override void OnSuccessfullyDiscardedHandCards(EnemyUnit enemy, int amount)
    {
        if (enemy == null || enemy.currentHp <= 0) return;
        if (!enemy.gameObject.activeInHierarchy) return;
        if (!enemy.HasStatus(SupportedStatus)) return;
        if (amount <= 0) return;

        int healPerCard = Mathf.RoundToInt(enemy.maxHp * Mathf.Clamp01(healMaxHpRatioPerCard));

        if (healPerCard <= 0) return;

        for (int i = 0; i < amount; i++)
        {
            if (enemy.currentHp <= 0 || !enemy.gameObject.activeInHierarchy) break;

            enemy.Heal(healPerCard, enemy);
        }

        enemy.RefreshHpUI();

        Debug.Log($"[CarrionFeast] {enemy.unitName} 成功使玩家捨棄 {amount} 張手牌，每張基礎治療量為 {healPerCard}。");
    }

    public override string GetDescription(EnemyUnit enemy, int amount)
    {
        return $"當玩家的手牌被此怪物隨機捨棄時，每成功捨棄1張，恢復自身最大HP的{Mathf.Clamp01(healMaxHpRatioPerCard) * 100f:0.#}%。";
    }
}