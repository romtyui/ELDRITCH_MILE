using UnityEngine;

[CreateAssetMenu(
    menuName = "CardGame/Item Effects/Gain Temporary Strength"
)]
public class GainTemporaryStrengthItemEffectData : ItemEffectData
{
    [Header("Temporary Strength")]
    public int amount = 2;

    public override void Execute(ItemUseContext context)
    {
        if (context == null)
        {
            Debug.LogWarning(
                "[GainTemporaryStrengthItemEffectData] context 是 null"
            );

            return;
        }

        if (context.player == null)
        {
            Debug.LogWarning(
                "[GainTemporaryStrengthItemEffectData] player 是 null"
            );

            return;
        }

        if (amount <= 0)
            return;

        context.player.ApplyStatus(
            StatusType.TemporaryStrength,
            amount
        );

        Debug.Log(
            $"[Item Effect] 獲得 TemporaryStrength x{amount}"
        );
    }
}