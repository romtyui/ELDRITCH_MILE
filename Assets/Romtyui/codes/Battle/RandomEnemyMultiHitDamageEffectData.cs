using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Random Enemy Multi Hit Damage")]
public class RandomEnemyMultiHitDamageEffectData : CardEffectData, CardDescriptionValueProvider
{
    [Header("Damage")]
    public int damagePerHit = 3;

    [Header("Hit Count")]
    public int hitCount = 3;

    public override void Execute(CardResolveContext context)
    {
        if (context == null)
            return;

        if (context.source == null)
            return;

        if (context.target == null)
        {
            Debug.LogWarning("[RandomEnemyMultiHitDamageEffectData] context.target 是 null，無法造成多段傷害");
            return;
        }

        for (int i = 0; i < hitCount; i++)
        {
            if (context.target.currentHp <= 0)
            {
                Debug.Log("[RandomEnemyMultiHitDamageEffectData] 目標已死亡，停止後續多段傷害");
                return;
            }

            context.source.DealDamageTo(context.target, damagePerHit);

            Debug.Log(
                $"[Random Multi Hit] 第 {i + 1} 次命中 {context.target.unitName}，基礎傷害 {damagePerHit}"
            );
        }
    }

    public bool TryGetDescriptionValue(string key, CardResolveContext context, out int value)
    {
        value = 0;

        if (key == "damage" || key == "damege" || key == "damage0" || key == "damege0")
        {
            int previewDamage = damagePerHit;

            if (context != null && context.source != null)
                previewDamage = context.source.ModifyOutgoingDamage(previewDamage);

            if (context != null && context.target != null)
                previewDamage = context.target.ModifyIncomingDamage(previewDamage);

            value = Mathf.Max(0, previewDamage);
            return true;
        }

        if (key == "hit" || key == "hits" || key == "count" || key == "hitCount")
        {
            value = hitCount;
            return true;
        }

        return false;
    }
}