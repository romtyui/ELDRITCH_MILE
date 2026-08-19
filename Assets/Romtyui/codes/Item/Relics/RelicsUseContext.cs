public class RelicsUseContext
{
    // =========================================================
    // Battle
    // =========================================================

    public BattleManager battleManager;


    // =========================================================
    // Player
    // =========================================================

    public BattleUnit player;


    // =========================================================
    // Trigger
    // =========================================================

    public RelicsTriggerType triggerType;


    // =========================================================
    // Card
    // =========================================================

    /// <summary>
    /// CardPlayed 時，代表剛剛打出的那張牌。
    ///
    /// 其他 Trigger，例如 BattleStart、
    /// PlayerTurnStart、PlayerTurnEnd，
    /// 這個值通常會是 null。
    /// </summary>
    public CardInstance playedCard;


    // =========================================================
    // Constructor
    // =========================================================

    public RelicsUseContext(
        BattleManager battleManager,
        BattleUnit player,
        RelicsTriggerType triggerType,
        CardInstance playedCard = null
    )
    {
        this.battleManager =
            battleManager;

        this.player =
            player;

        this.triggerType =
            triggerType;

        this.playedCard =
            playedCard;
    }
}