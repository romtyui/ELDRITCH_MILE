using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Effects/Apply Status")]
public class ApplyStatusEffectData : CardEffectData
{
    public StatusType statusType;
    public int amount;

    public override void Execute(CardResolveContext context)
    {
        if (context.target == null) return;
        context.target.ApplyStatus(statusType, amount);
    }
}

public enum StatusType
{
    Strength,
    Weak,
    Vulnerable,
    Frail,
    Poison
}