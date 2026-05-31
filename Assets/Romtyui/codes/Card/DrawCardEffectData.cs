using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Draw Cards")]
public class DrawCardEffectData : CardEffectData
{
    public int amount;

    public override void Execute(CardResolveContext context)
    {
        context.battleManager.PlayerDrawCards(amount);
    }
}