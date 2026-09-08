using UnityEngine;

// ⚠️ 封存的舊碼。包進 namespace 是為了**不要污染全域命名空間** ——
//    這裡宣告的 RunNodeData / MapData / ICardInteractable 等等
//    與正式碼同名，放在全域會蓋掉正式那組，症狀是新檔案裡出現
//    「MapData 沒有 GetNode」這種完全不會指向這裡的錯誤。
namespace EldritchMile.Archive
{

[CreateAssetMenu(menuName = "CardGame/Exploration Effects/Add Card To Discard Pile")]
public class ExploreAddCardToDeckEffectData : ExplorationCardEffectData
{
    public CardDataExplore cardToAdd;

    public override void Execute(ExplorationCardResolveContext context)
    {
        if (context == null || context.deck == null) return;
        context.deck.AddCardToDiscardPile(cardToAdd);
        
        if (cardToAdd != null)
            Debug.Log($"[ExploreAddCardToDeckEffectData] 加入卡牌到棄牌堆：{cardToAdd.cardName}");
    }
}
}
