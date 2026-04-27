using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Damage")]
public class DamageEffectData : CardEffectData
{
    public int amount;

    public override void Execute(CardResolveContext context)
    {
        if (context.target == null) return;

        int finalDamage = amount;

        // 之後可加：力量、虛弱、易傷等修正
        context.target.TakeDamage(finalDamage);
    }
}