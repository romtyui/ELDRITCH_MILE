public class RelicsUseContext
{
    public BattleManager battleManager;
    public BattleUnit player;

    public RelicsUseContext(
        BattleManager battleManager,
        BattleUnit player
    )
    {
        this.battleManager = battleManager;
        this.player = player;
    }
}