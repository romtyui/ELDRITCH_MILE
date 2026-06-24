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
                Debug.Log($"[EnemyAction] {self.unitName} 獲得再生 {amount} 層，目前 {self.GetStatus(StatusType.Regeneration)} 層");
                break;

            case ApplyMode.OnlyIfMissing:
                if (currentRegeneration <= 0)
                {
                    self.ApplyStatus(StatusType.Regeneration, amount);
                    Debug.Log($"[EnemyAction] {self.unitName} 沒有再生，獲得再生 {amount} 層");
                }
                else
                {
                    Debug.Log($"[EnemyAction] {self.unitName} 已有再生 {currentRegeneration} 層，不觸發");
                }
                break;

            case ApplyMode.OnlyIfHas:
                if (currentRegeneration > 0)
                {
                    self.ApplyStatus(StatusType.Regeneration, amount);
                    Debug.Log($"[EnemyAction] {self.unitName} 已有再生，額外獲得 {amount} 層，目前 {self.GetStatus(StatusType.Regeneration)} 層");
                }
                else
                {
                    Debug.Log($"[EnemyAction] {self.unitName} 沒有再生，不觸發");
                }
                break;

            case ApplyMode.SetToValue:
                self.SetStatus(StatusType.Regeneration, amount);
                Debug.Log($"[EnemyAction] {self.unitName} 的再生被設定為 {amount} 層");
                break;
        }

        self.RefreshAllUI();
    }
}