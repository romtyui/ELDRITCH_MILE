using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Token/Add Card To Hand By Token Count")]
public class RandomDamageByHandTokenCountEffectData : CardEffectData, CardDescriptionValueProvider
{
    [Header("Card To Add")]
    public CardData cardToAdd;

    [Header("Base Amount")]
    public int baseAmount = 1;

    [Header("Token Count Bonus")]
    public bool addByHandTokenCount = false;
    public string tokenId = "DefaultToken";
    public int amountPerTokenInHand = 1;

    public override void Execute(CardResolveContext context)
    {
        if (context == null)
            return;

        if (context.battleManager == null)
            return;

        if (cardToAdd == null)
        {
            Debug.LogWarning("[RandomDamageByHandTokenCountEffectData] cardToAdd 是 null，無法生成卡片");
            return;
        }

        int finalAmount = GetFinalAmount(context);

        for (int i = 0; i < finalAmount; i++)
        {
            context.battleManager.AddCardToHand(cardToAdd);
        }

        Debug.Log($"[RandomDamageByHandTokenCountEffectData] 生成 {cardToAdd.cardName} x{finalAmount} 到手牌");
    }

    private int GetFinalAmount(CardResolveContext context)
    {
        int finalAmount = Mathf.Max(0, baseAmount);

        if (addByHandTokenCount &&
            context != null &&
            context.battleManager != null)
        {
            int tokenCount = context.battleManager.CountTokenInHand(tokenId);
            finalAmount += tokenCount * Mathf.Max(0, amountPerTokenInHand);
        }

        return Mathf.Max(0, finalAmount);
    }

    public bool TryGetDescriptionValue(string key, CardResolveContext context, out int value)
    {
        value = 0;

        if (key == "addCardCount" ||
            key == "cardCount" ||
            key == "createCount" ||
            key == "tokenCount")
        {
            value = GetFinalAmount(context);
            return true;
        }

        return false;
    }
}