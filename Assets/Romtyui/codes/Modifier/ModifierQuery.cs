public class ModifierQuery
{
    public ModifierType type;

    public BattleUnit source;
    public BattleUnit target;

    public CardInstance card;
    public BattleManager battleManager;

    public ModifierRoundingMode roundingMode = ModifierRoundingMode.Nearest;

    public bool clampResultToZero = true;

    public ModifierQuery(ModifierType type, BattleUnit source = null, BattleUnit target = null, CardInstance card = null, BattleManager battleManager = null)
    {
        this.type = type;
        this.source = source;
        this.target = target;
        this.card = card;
        this.battleManager = battleManager;
    }
}