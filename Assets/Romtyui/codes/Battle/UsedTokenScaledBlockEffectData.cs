using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Token/Used Token Scaled Block")]
public class UsedTokenScaledBlockEffectData : CardEffectData
{
    [Header("Token")]
    public string tokenId = "DefaultToken";

    [Header("Block")]
    public int baseBlock = 0;
    public int blockPerUsedToken = 1;

    public override void Execute(CardResolveContext context)
    {
        if (context == null) return;
        if (context.source == null) return;
        if (context.battleManager == null) return;

        int usedTokenCount = context.battleManager.GetUsedTokenCount(tokenId);
        int finalBlock = baseBlock + usedTokenCount * blockPerUsedToken;

        context.source.GainBlock(finalBlock);

        Debug.Log($"[Token Block] tokenId={tokenId}, used={usedTokenCount}, block={finalBlock}");
    }
}