using UnityEngine;

public enum ApplyStatusTargetMode
{
    Source,
    Target
}

[CreateAssetMenu(menuName = "CardGame/Effects/Apply Status")]
public class ApplyStatusEffectData : CardEffectData, CardDescriptionValueProvider
{
    [Header("Status")]
    public StatusType statusType;

    [Tooltip("要套用的狀態層數")]
    public int amount = 1;

    [Header("Apply Target")]
    [Tooltip("Source = 出牌者自己。Target = CardData 決定的目標")]
    public ApplyStatusTargetMode applyTarget = ApplyStatusTargetMode.Target;

    public override void Execute(CardResolveContext context)
    {
        if (context == null)
            return;

        BattleUnit targetUnit = ResolveApplyTarget(context);

        if (targetUnit == null)
        {
            Debug.LogWarning(
                $"[ApplyStatusEffectData] 找不到可套用狀態的目標。Status = {statusType}, ApplyTarget = {applyTarget}"
            );
            return;
        }

        int finalAmount = Mathf.Max(0, amount);

        if (finalAmount <= 0)
            return;

        targetUnit.ApplyStatus(statusType, finalAmount);

        Debug.Log(
            $"[ApplyStatusEffectData] {targetUnit.unitName} 獲得 {statusType} x{finalAmount}"
        );
    }

    private BattleUnit ResolveApplyTarget(CardResolveContext context)
    {
        if (context == null)
            return null;

        switch (applyTarget)
        {
            case ApplyStatusTargetMode.Source:
                return context.source;

            case ApplyStatusTargetMode.Target:
                return context.target;
        }

        return null;
    }

    public bool TryGetDescriptionValue(string key, CardResolveContext context, out int value)
    {
        value = 0;

        if (key == "statusAmount" ||
            key == "status" ||
            key == "amount" ||
            key == "狀態層數")
        {
            value = Mathf.Max(0, amount);
            return true;
        }

        return false;
    }
}
