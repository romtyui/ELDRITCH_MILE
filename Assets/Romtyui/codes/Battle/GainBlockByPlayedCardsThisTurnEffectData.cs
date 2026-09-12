using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Block/Gain Block By Played Cards This Turn")]
public class GainBlockByPlayedCardsThisTurnEffectData : CardEffectData, CardDescriptionValueProvider
{
    [Header("Block")]
    [Tooltip("本回合目前每使用過 1 張牌獲得多少格擋，包含這張正在結算的牌自己")]
    public int blockPerCard = 1;

    public override void Execute(CardResolveContext context)
    {
        if (context == null)
            return;

        if (context.source == null)
        {
            Debug.LogWarning("[GainBlockByPlayedCardsThisTurnEffectData] context.source 是 null");
            return;
        }

        if (context.battleManager == null)
        {
            Debug.LogWarning("[GainBlockByPlayedCardsThisTurnEffectData] context.battleManager 是 null");
            return;
        }

        BattleDeck battleDeck = context.battleManager.playerDeck;

        if (battleDeck == null)
        {
            Debug.LogWarning("[GainBlockByPlayedCardsThisTurnEffectData] playerDeck 是 null");
            return;
        }

        int playedCount = battleDeck.PlayedCardsThisTurn.Count;
        int finalBlockPerCard = Mathf.Max(0, blockPerCard);
        int baseBlock = playedCount * finalBlockPerCard;

        if (baseBlock <= 0)
            return;

        context.source.GainBlock(baseBlock);

        Debug.Log(
            $"[GainBlockByPlayedCardsThisTurnEffectData] 本回合目前已使用 {playedCount} 張牌，" +
            $"每張提供 {finalBlockPerCard} 格擋，基礎格擋共 {baseBlock}"
        );
    }

    public bool TryGetDescriptionValue(string key, CardResolveContext context, out int value)
    {
        value = 0;

        if (key != "block")
            return false;

        if (context == null ||
            context.source == null ||
            context.battleManager == null ||
            context.battleManager.playerDeck == null)
        {
            return true;
        }

        int playedCount =
            context.battleManager.playerDeck.PlayedCardsThisTurn.Count;

        int baseBlock =
            playedCount * Mathf.Max(0, blockPerCard);

        value = context.source.ModifyBlockGain(baseBlock);

        return true;
    }
}