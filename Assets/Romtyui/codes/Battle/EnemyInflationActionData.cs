using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy Actions/Inflation")]
public class EnemyInflationActionData : EnemyActionData
{
    [Min(0)]
    public int blockAmount = 5;

    [Min(0)]
    public int counterAmount = 3;

    [Min(1)]
    public int maxInflationStacks = 3;

    public override void Execute(EnemyActionContext context)
    {
        if (context == null || context.enemy == null) return;

        EnemyUnit enemy = context.enemy;

        if (enemy.currentHp <= 0 || !enemy.gameObject.activeInHierarchy) return;

        int currentStacks = enemy.GetStatus(StatusType.Inflation);
        int nextStacks = Mathf.Min(currentStacks + 1, Mathf.Max(1, maxInflationStacks));

        enemy.GainBlock(Mathf.Max(0, blockAmount));

        if (counterAmount > 0) enemy.ApplyStatus(StatusType.Counter, counterAmount);

        enemy.SetStatus(StatusType.Inflation, nextStacks);
        enemy.RefreshAllUI();

        Debug.Log($"[Inflation] {enemy.unitName} 膨脹至 {nextStacks} 層，目前反擊 {enemy.GetStatus(StatusType.Counter)} 層。");
    }
}