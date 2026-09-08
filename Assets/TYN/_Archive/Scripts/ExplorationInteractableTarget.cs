using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// ⚠️ 封存的舊碼。包進 namespace 是為了**不要污染全域命名空間** ——
//    這裡宣告的 RunNodeData / MapData / ICardInteractable 等等
//    與正式碼同名，放在全域會蓋掉正式那組，症狀是新檔案裡出現
//    「MapData 沒有 GetNode」這種完全不會指向這裡的錯誤。
namespace EldritchMile.Archive
{

public class ExplorationInteractableTarget : MonoBehaviour
{
    [Header("Identity")]
    public string targetId;
    public string displayName;

    [Header("Accepted Cards")]
    public bool acceptAnyExplorationCard = true;
    public List<string> acceptedCardIds = new();

    [Header("Interaction")]
    public bool canInteractOnce = false;
    public bool hasInteracted;
    
    [Tooltip("當正確的卡牌打在它身上時，要觸發什麼功能？ (拉入 Door 或 ContainerObject 的方法)")]
    public UnityEvent onInteract;

    public bool CanAccept(CardData cardData)
    {
        if (cardData == null) return false;
        if (hasInteracted && canInteractOnce) return false;
        if (acceptAnyExplorationCard) return true;
        if (string.IsNullOrEmpty(cardData.cardId)) return false;
        return acceptedCardIds.Contains(cardData.cardId);
    }

    public bool Interact(ExplorationCardResolveContext context)
    {
        if (context == null || context.card == null || context.card.data == null) return false;
        if (!CanAccept(context.card.data)) return false;

        hasInteracted = true;
        Debug.Log($"[Target] {displayName} 被卡牌 {context.card.data.cardName} 互動");
        
        // 觸發外部事件 (低耦合的核心！)
        onInteract?.Invoke(); 
        return true;
    }
}
}
