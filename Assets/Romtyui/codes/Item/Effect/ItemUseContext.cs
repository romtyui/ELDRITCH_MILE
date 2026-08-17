public class ItemUseContext
{
    public BattleUnit player;
    public BattleManager battleManager;

    public ItemUseContext(
        BattleUnit player,
        BattleManager battleManager
    )
    {
        this.player = player;
        this.battleManager = battleManager;
    }
}