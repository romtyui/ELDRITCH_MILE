using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Token/Generate Token To Hand")]
public class GenerateTokenToHandEffectData : CardEffectData
{
    [Header("Token")]
    public CardData tokenCardData;

    [Header("Amount")]
    public int amount = 1;

    public override void Execute(CardResolveContext context)
    {
        if (context == null) return;
        if (context.battleManager == null) return;

        if (tokenCardData == null)
        {
            Debug.LogWarning("[GenerateTokenToHandEffectData] tokenCardData 沒有指定");
            return;
        }

        if (!tokenCardData.isToken)
        {
            Debug.LogWarning($"[GenerateTokenToHandEffectData] {tokenCardData.cardName} 沒有勾 isToken");
        }

        if (!tokenCardData.retain)
        {
            Debug.LogWarning($"[GenerateTokenToHandEffectData] {tokenCardData.cardName} 沒有勾 retain，回合結束會被棄掉");
        }

        for (int i = 0; i < amount; i++)
        {
            context.battleManager.AddCardToHand(tokenCardData);
        }

        Debug.Log($"[Token] 生成 {amount} 張 Token 到手牌：{tokenCardData.cardName}");
    }
}