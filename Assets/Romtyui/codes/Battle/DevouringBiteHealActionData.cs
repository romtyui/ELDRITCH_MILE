using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy Actions/Devouring Bite Heal")]
public class DevouringBiteHealActionData : EnemyActionData
{
    [Range(0f, 1f)]
    public float healMaxHpRatio = 0.2f;

    public override void Execute(EnemyActionContext context)
    {
        if (context == null || context.enemy == null) return;
        if (context.enemy.currentHp <= 0) return;
        if (!context.enemy.gameObject.activeInHierarchy) return;

        int healAmount = Mathf.RoundToInt(context.enemy.maxHp * Mathf.Clamp01(healMaxHpRatio));

        if (healAmount <= 0) return;

        context.enemy.Heal(healAmount, context.enemy);
        context.enemy.RefreshHpUI();

        Debug.Log($"[DevouringBite] {context.enemy.unitName} 恢復最大HP的 {healMaxHpRatio * 100f:0.#}%，基礎治療量：{healAmount}。");
    }
}