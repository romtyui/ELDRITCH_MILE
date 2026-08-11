using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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