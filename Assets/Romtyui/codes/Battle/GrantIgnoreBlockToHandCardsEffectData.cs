using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Hand/Grant Ignore Block To Hand Cards")]
public class GrantIgnoreBlockToHandCardsEffectData : CardEffectData
{
    [Header("Target Card")]
    [Tooltip("要賦予穿透的卡片 Card Id")]
    public string targetCardId = "goat_token";

    public override void Execute(CardResolveContext context)
    {
        if (context == null || context.battleManager == null) return;
        if (context.battleManager.playerDeck == null) return;
        if (string.IsNullOrWhiteSpace(targetCardId)) return;

        var hand = context.battleManager.playerDeck.Hand;
        string requiredCardId = targetCardId.Trim();
        int grantedCount = 0;

        for (int i = 0; i < hand.Count; i++)
        {
            CardInstance card = hand[i];

            if (card == null || card.data == null) continue;
            if (!string.Equals(card.data.cardId, requiredCardId, System.StringComparison.Ordinal)) continue;
            if (card.hasGrantedIgnoreBlock) continue;

            card.hasGrantedIgnoreBlock = true;
            grantedCount++;
        }

        if (context.battleManager.handUIController != null) context.battleManager.handUIController.RefreshCardDescriptionsOnly();

        Debug.Log($"[GrantIgnoreBlockToHandCardsEffectData] {grantedCount} 張 {requiredCardId} 獲得穿透");
    }
}