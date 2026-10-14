using UnityEngine;

public enum EnemyDamageType
{
    Normal = 0,
    BlockBreaker = 1,
    Piercing = 2
}

[CreateAssetMenu(menuName = "CardGame/Enemy Actions/Damage Player")]
public class EnemyDamageActionData : EnemyActionData
{
    [Tooltip("攻擊的基礎傷害，實際傷害會套用力量、虛弱、易傷及 Modifier 修正。")]
    public int amount = 6;

    [Min(1)]
    [Tooltip("攻擊段數。每段各自套用傷害修正、護盾與反擊；任一方死亡後停止後續攻擊。原本單次攻擊設為 1。")]
    public int hitCount = 1;

    [Tooltip("普通：正常傷害。破盾：目標有護盾時，修正後傷害翻倍。穿透：無視目標護盾且不消耗護盾。")]
    public EnemyDamageType damageType = EnemyDamageType.Normal;

    public override void Execute(EnemyActionContext context)
    {
        if (context == null) return;
        if (context.enemy == null) return;
        if (context.player == null) return;

        int totalHits = Mathf.Max(1, hitCount);

        for (int i = 0; i < totalHits; i++)
        {
            if (context.enemy == null || context.player == null) break;
            if (context.enemy.currentHp <= 0 || context.player.currentHp <= 0) break;

            switch (damageType)
            {
                case EnemyDamageType.Normal:
                    context.enemy.DealDamageTo(context.player, amount);
                    break;

                case EnemyDamageType.BlockBreaker:
                    context.enemy.DealDamageTo(context.player, amount, false, true);
                    break;

                case EnemyDamageType.Piercing:
                    context.enemy.DealDamageTo(context.player, amount, true, false);
                    break;

                default:
                    context.enemy.DealDamageTo(context.player, amount);
                    break;
            }

            Debug.Log($"{context.enemy.unitName} 攻擊玩家，類型：{damageType}，基礎傷害：{amount}，段數：{i + 1}/{totalHits}");
        }
    }
}