using UnityEngine;

[CreateAssetMenu(
    menuName = "CardGame/Item Effects/Heal"
)]
public class HealItemEffectData : ItemEffectData
{
    [Header("Heal")]
    public int healAmount = 10;

    public override void Execute(ItemUseContext context)
    {
        if (context == null)
        {
            Debug.LogWarning(
                "[HealItemEffectData] context 是 null"
            );

            return;
        }

        if (context.player == null)
        {
            Debug.LogWarning(
                "[HealItemEffectData] player 是 null"
            );

            return;
        }

        if (healAmount <= 0)
            return;

        context.player.Heal(healAmount);

        Debug.Log(
            $"[Item Effect] 回復 {healAmount} 點生命"
        );
    }
}