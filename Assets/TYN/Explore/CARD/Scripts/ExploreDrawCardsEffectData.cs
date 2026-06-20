using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Exploration Effects/Draw Cards")]
public class ExploreDrawCardsEffectData : ExplorationCardEffectData
{
    public int amount = 1;

    public override void Execute(ExplorationCardResolveContext context)
    {
        if (context == null || context.manager == null) return;
        context.manager.DrawCards(amount);
    }
}