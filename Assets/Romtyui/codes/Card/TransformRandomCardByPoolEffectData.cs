using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Special/Transform Random Card By Pool")]
public class TransformRandomCardByPoolEffectData : CardEffectData
{
    [Header("Transform")]
    public CardTransformPoolData transformPool;

    public override void Execute(CardResolveContext context)
    {
        // 保留一般執行方式，避免沒有動畫系統時失效
        ExecuteTransform(context);
    }

    public CardTransformResult ExecuteTransform(CardResolveContext context)
    {
        if (context == null)
        {
            Debug.LogWarning("[TransformRandomCardByPoolEffectData] context 是 null");
            return new CardTransformResult(false);
        }

        if (context.battleManager == null)
        {
            Debug.LogWarning("[TransformRandomCardByPoolEffectData] battleManager 是 null");
            return new CardTransformResult(false);
        }

        if (context.battleManager.playerDeck == null)
        {
            Debug.LogWarning("[TransformRandomCardByPoolEffectData] playerDeck 是 null");
            return new CardTransformResult(false);
        }

        if (transformPool == null)
        {
            Debug.LogWarning("[TransformRandomCardByPoolEffectData] transformPool 沒有指定");
            return new CardTransformResult(false);
        }

        CardTransformResult result = context.battleManager.playerDeck.TransformRandomCardInDrawPileByPool(transformPool);

        if (result.success)
        {
            Debug.Log($"[變化牌] 使用變化卡池：{transformPool.transformId}");
        }

        return result;
    }
}