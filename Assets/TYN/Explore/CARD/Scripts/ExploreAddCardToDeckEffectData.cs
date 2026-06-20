using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Exploration Effects/Add Card To Discard Pile")]
public class ExploreAddCardToDeckEffectData : ExplorationCardEffectData
{
    public CardData cardToAdd;

    public override void Execute(ExplorationCardResolveContext context)
    {
        if (context == null || context.deck == null) return;
        context.deck.AddCardToDiscardPile(cardToAdd);
        
        if (cardToAdd != null)
            Debug.Log($"[ExploreAddCardToDeckEffectData] 加入卡牌到棄牌堆：{cardToAdd.cardName}");
    }
}