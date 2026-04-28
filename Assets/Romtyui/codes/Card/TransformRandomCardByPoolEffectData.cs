using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Special/Transform Random Card By Pool")]
public class TransformRandomCardByPoolEffectData : CardEffectData
{
    [Header("Transform")]
    public CardTransformPoolData transformPool;

    public override void Execute(CardResolveContext context)
    {
        if (context == null)
        {
            Debug.LogWarning("[TransformRandomCardByPoolEffectData] context 是 null");
            return;
        }

        if (context.battleManager == null)
        {
            Debug.LogWarning("[TransformRandomCardByPoolEffectData] battleManager 是 null");
            return;
        }

        if (context.battleManager.playerDeck == null)
        {
            Debug.LogWarning("[TransformRandomCardByPoolEffectData] playerDeck 是 null");
            return;
        }

        if (transformPool == null)
        {
            Debug.LogWarning("[TransformRandomCardByPoolEffectData] transformPool 沒有指定");
            return;
        }

        bool success = context.battleManager.playerDeck.TransformRandomCardInDrawPileByPool(transformPool);

        if (success)
        {
            Debug.Log($"[變化牌] 使用變化卡池：{transformPool.transformId}");
        }
    }
}