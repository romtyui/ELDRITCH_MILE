using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Hand/Grant Break Block To Hand Cards")]
public class GrantBreakBlockToHandCardsEffectData : CardEffectData
{
    [Header("Target Card")]
    [Tooltip("要賦予破盾的卡片 Card Id")]
    public string targetCardId = "goat_token";

    [Header("Break Block")]
    [Min(1f)]
    [Tooltip("目標有護盾時的基礎傷害倍率。2 表示 2 倍；重複賦予時保留較高倍率")]
    public float multiplier = 2f;

    public override void Execute(CardResolveContext context)
    {
        if (context == null || context.battleManager == null) return;
        if (context.battleManager.playerDeck == null) return;
        if (string.IsNullOrWhiteSpace(targetCardId)) return;

        var hand = context.battleManager.playerDeck.Hand;
        string requiredCardId = targetCardId.Trim();
        float grantedMultiplier = Mathf.Max(1f, multiplier);
        int grantedCount = 0;

        for (int i = 0; i < hand.Count; i++)
        {
            CardInstance card = hand[i];

            if (card == null || card.data == null) continue;
            if (!string.Equals(card.data.cardId, requiredCardId, System.StringComparison.Ordinal)) continue;
            if (card.hasGrantedBreakBlock && card.grantedBreakBlockMultiplier >= grantedMultiplier) continue;

            card.grantedBreakBlockMultiplier = card.hasGrantedBreakBlock ? Mathf.Max(card.grantedBreakBlockMultiplier, grantedMultiplier) : grantedMultiplier;
            card.hasGrantedBreakBlock = true;
            grantedCount++;
        }

        if (context.battleManager.handUIController != null) context.battleManager.handUIController.RefreshCardDescriptionsOnly();

        Debug.Log($"[GrantBreakBlockToHandCardsEffectData] {grantedCount} 張 {requiredCardId} 獲得或提升破盾，倍率：{grantedMultiplier}");
    }
}