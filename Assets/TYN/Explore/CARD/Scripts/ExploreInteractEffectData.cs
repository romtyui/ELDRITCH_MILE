using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Exploration Effects/Interact With Target")]
public class ExploreInteractEffectData : ExplorationCardEffectData
{
    public override void Execute(ExplorationCardResolveContext context)
    {
        if (context == null) return;
        if (context.target == null)
        {
            Debug.Log("沒有探索互動目標");
            return;
        }
        // 觸發場景物件 (ExplorationInteractableTarget) 的 Interact 方法
        context.target.Interact(context); 
    }
}