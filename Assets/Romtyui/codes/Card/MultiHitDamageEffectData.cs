using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Multi Hit Damage")]
public class MultiHitDamageEffectData : CardEffectData
{
    [Header("Damage")]
    public int damagePerHit = 3;

    [Header("Hit Count")]
    public int hitCount = 3;

    public override void Execute(CardResolveContext context)
    {
        if (context == null) return;
        if (context.target == null) return;

        for (int i = 0; i < hitCount; i++)
        {
            if (context.target.currentHp <= 0)
                break;

            int finalDamage = damagePerHit;

            // 之後可加：力量、虛弱、易傷等修正
            context.target.TakeDamage(finalDamage);

            Debug.Log($"多段傷害第 {i + 1} 下：造成 {finalDamage} 傷害");
        }
    }
}