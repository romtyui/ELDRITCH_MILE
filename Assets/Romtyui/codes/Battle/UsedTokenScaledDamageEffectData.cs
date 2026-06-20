using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Token/Used Token Scaled Damage")]
public class UsedTokenScaledDamageEffectData : CardEffectData
{
    [Header("Token")]
    public string tokenId = "DefaultToken";

    [Header("Damage")]
    public int baseDamage = 0;
    public int damagePerUsedToken = 1;

    public override void Execute(CardResolveContext context)
    {
        if (context == null) return;
        if (context.source == null) return;
        if (context.target == null) return;
        if (context.battleManager == null) return;

        int usedTokenCount = context.battleManager.GetUsedTokenCount(tokenId);
        int finalDamage = baseDamage + usedTokenCount * damagePerUsedToken;

        context.source.DealDamageTo(context.target, finalDamage);

        Debug.Log($"[Token Damage] tokenId={tokenId}, used={usedTokenCount}, damage={finalDamage}");
    }
}