public class CardResolveContext
{
    public BattleUnit source;
    public BattleUnit target;
    public CardInstance card;
    public BattleManager battleManager;

    public CardResolveContext(BattleUnit source, BattleUnit target, CardInstance card, BattleManager battleManager)
    {
        this.source = source;
        this.target = target;
        this.card = card;
        this.battleManager = battleManager;
    }
}