using UnityEngine;

// ⚠️ 封存的舊碼。包進 namespace 是為了**不要污染全域命名空間** ——
//    這裡宣告的 RunNodeData / MapData / ICardInteractable 等等
//    與正式碼同名，放在全域會蓋掉正式那組，症狀是新檔案裡出現
//    「MapData 沒有 GetNode」這種完全不會指向這裡的錯誤。
namespace EldritchMile.Archive
{

public class ExplorationCardResolveContext
{
    // 將這裡改為新名稱 CardExplorationManager
    public CardExplorationManager manager; 
    public ExplorationDeck deck;
    public CardInstance card;
    public ExplorationInteractableTarget target;
    public Camera playerCamera;

    public ExplorationCardResolveContext(
        CardExplorationManager manager,
        ExplorationDeck deck,
        CardInstance card,
        ExplorationInteractableTarget target,
        Camera playerCamera
    )
    {
        this.manager = manager;
        this.deck = deck;
        this.card = card;
        this.target = target;
        this.playerCamera = playerCamera;
    }
}
}
