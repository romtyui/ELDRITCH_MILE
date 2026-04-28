public class EnemyActionContext
{
    public EnemyUnit enemy;
    public BattleUnit player;
    public BattleManager battleManager;

    public EnemyActionContext(EnemyUnit enemy, BattleUnit player, BattleManager battleManager)
    {
        this.enemy = enemy;
        this.player = player;
        this.battleManager = battleManager;
    }
}