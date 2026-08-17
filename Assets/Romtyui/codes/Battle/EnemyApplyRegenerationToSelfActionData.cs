using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy Actions/Apply Regeneration To Self")]
public class EnemyApplyRegenerationToSelfActionData : EnemyActionData
{
    public enum ApplyMode
    {
        AlwaysAdd,
        OnlyIfMissing,
        OnlyIfHas,
        SetToValue
    }

    [Header("Regeneration")]
    public int amount = 3;

    [Header("Condition")]
    public ApplyMode applyMode = ApplyMode.OnlyIfMissing;

    public override void Execute(EnemyActionContext context)
    {
        if (context == null)
            return;

        if (context.enemy == null)
            return;

        EnemyUnit self = context.enemy;

        if (self.currentHp <= 0)
            return;

        int currentRegeneration = self.GetStatus(StatusType.Regeneration);

        switch (applyMode)
        {
            case ApplyMode.AlwaysAdd:
                self.ApplyStatus(StatusType.Regeneration, amount);
                Debug.Log($"[EnemyAction] {self.unitName} ��o�A�� {amount} �h�A�ثe {self.GetStatus(StatusType.Regeneration)} �h");
                break;

            case ApplyMode.OnlyIfMissing:
                if (currentRegeneration <= 0)
                {
                    self.ApplyStatus(StatusType.Regeneration, amount);
                    Debug.Log($"[EnemyAction] {self.unitName} �S���A�͡A��o�A�� {amount} �h");
                }
                else
                {
                    Debug.Log($"[EnemyAction] {self.unitName} �w���A�� {currentRegeneration} �h�A��Ĳ�o");
                }
                break;

            case ApplyMode.OnlyIfHas:
                if (currentRegeneration > 0)
                {
                    self.ApplyStatus(StatusType.Regeneration, amount);
                    Debug.Log($"[EnemyAction] {self.unitName} �w���A�͡A�B�~��o {amount} �h�A�ثe {self.GetStatus(StatusType.Regeneration)} �h");
                }
                else
                {
                    Debug.Log($"[EnemyAction] {self.unitName} �S���A�͡A��Ĳ�o");
                }
                break;

            case ApplyMode.SetToValue:
                self.SetStatus(StatusType.Regeneration, amount);
                Debug.Log($"[EnemyAction] {self.unitName} ���A�ͳQ�]�w�� {amount} �h");
                break;
        }

        self.RefreshAllUI();
    }
}