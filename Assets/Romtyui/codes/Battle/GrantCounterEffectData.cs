using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Counter/Grant Counter")]
public class GrantCounterEffectData : CardEffectData, CardDescriptionValueProvider
{
    [Header("Counter")]
    [Tooltip("給予出牌者的反擊層數")]
    public int amount = 1;

    public override void Execute(CardResolveContext context)
    {
        if (context == null)
            return;

        if (context.source == null)
        {
            Debug.LogWarning("[GrantCounterEffectData] context.source 是 null，無法給予反擊");
            return;
        }

        int finalAmount = Mathf.Max(0, amount);

        if (finalAmount <= 0)
            return;

        context.source.ApplyStatus(StatusType.Counter, finalAmount);

        Debug.Log(
            $"[GrantCounterEffectData] {context.source.unitName} 獲得反擊 x{finalAmount}"
        );
    }

    public bool TryGetDescriptionValue(string key, CardResolveContext context, out int value)
    {
        value = 0;

        if (key == "counter" ||
            key == "counterAmount" ||
            key == "status" ||
            key == "amount")
        {
            value = Mathf.Max(0, amount);
            return true;
        }

        return false;
    }
}