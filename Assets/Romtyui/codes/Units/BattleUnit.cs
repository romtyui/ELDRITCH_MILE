using System;
using System.Collections.Generic;
using UnityEngine;

public class BattleUnit : MonoBehaviour
{
    public string unitName;
    public int maxHp = 100;
    public int currentHp;
    public int block;

    public event Action OnHpChanged;
    public event Action OnStatusChanged;
    [Header("Unit Type")]
    public bool isPlayerUnit;

    [SerializeField] private Dictionary<StatusType, int> statuses = new();

    protected virtual void Awake()
    {
        currentHp = maxHp;
        OnHpChanged?.Invoke();
    }
    public Dictionary<StatusType, int> GetAllStatuses()
    {
        return new Dictionary<StatusType, int>(statuses);
    }
    public virtual void OnTurnStart()
    {
        
        ResolveRegenerationAtTurnStart();
        ResolveHardenAtTurnStart();
    }

    public virtual void OnTurnEnd()
    {
        ResolvePoisonAtTurnStart();
        ClearEndOfTurnStatuses();
        TickTemporaryStatuses();
    }
    protected virtual void ClearEndOfTurnStatuses()
    {
        ClearStatusCompletely(StatusType.TemporaryStrength);
    }
    public virtual void ClearStatusCompletely(StatusType statusType)
    {
        if (!statuses.ContainsKey(statusType))
            return;

        int oldAmount = statuses[statusType];

        statuses.Remove(statusType);

        OnStatusChanged?.Invoke();

        Debug.Log($"{unitName} 的 {statusType} 已在回合結束清除，原本層數：{oldAmount}");
    }

    public virtual void ClearAllStatuses()
    {
        statuses.Clear();
        OnStatusChanged?.Invoke();

        Debug.Log($"{unitName} 的所有狀態已清除");
    }

    public virtual void FullResetUnit()
    {
        currentHp = maxHp;
        block = 0;

        ClearAllStatuses();

        OnHpChanged?.Invoke();

        Debug.Log($"{unitName} 已完全重置");
    }
    public virtual void DealDamageTo(BattleUnit target, int baseDamage)
    {
        if (target == null)
            return;

        int damage = baseDamage;

        damage = ModifyOutgoingDamage(damage);
        damage = target.ModifyIncomingDamage(damage);

        if (damage < 0)
            damage = 0;

        target.TakeDamage(damage);

        Debug.Log($"{unitName} 對 {target.unitName} 造成 {damage} 傷害");
    }

    public virtual int ModifyOutgoingDamage(int damage)
    {
        int strength = GetStatus(StatusType.Strength);
        int temporaryStrength = GetStatus(StatusType.TemporaryStrength);

        damage += strength;
        damage += temporaryStrength;

        if (GetStatus(StatusType.Weak) > 0)
        {
            damage = Mathf.CeilToInt(damage * 0.75f);
        }

        return Mathf.Max(0, damage);
    }

    public virtual int ModifyIncomingDamage(int damage)
    {
        if (GetStatus(StatusType.Vulnerable) > 0)
        {
            damage = Mathf.CeilToInt(damage * 1.5f);
        }

        return Mathf.Max(0, damage);
    }

    public virtual int ModifyBlockGain(int amount)
    {
        if (GetStatus(StatusType.Frail) > 0)
        {
            amount = Mathf.FloorToInt(amount * 0.75f);
        }

        return Mathf.Max(0, amount);
    }

    public virtual void TakeDamage(int amount)
    {
        int hpBefore = currentHp;

        int remaining = amount;

        if (block > 0)
        {
            int absorbed = Mathf.Min(block, remaining);
            block -= absorbed;
            remaining -= absorbed;
        }

        currentHp -= remaining;

        if (currentHp < 0)
            currentHp = 0;

        int realHpDamage = hpBefore - currentHp;

        OnHpChanged?.Invoke();

        if (realHpDamage > 0)
        {
            ReduceRegenerationOnDamage();
            OnAfterHpDamageTaken(realHpDamage);
        }

        if (realHpDamage > 0 && isPlayerUnit)
        {
            if (CameraShake.Instance != null)
                CameraShake.Instance.Shake();
        }

        if (realHpDamage > 0 && this is EnemyUnit enemy)
        {
            enemy.ShowDamagePopup(realHpDamage);
        }

        Debug.Log($"{unitName} 受到 {amount} 傷害，實際扣血 {realHpDamage}，剩餘 HP: {currentHp}");

        if (currentHp <= 0)
        {
            Die();
        }
        else if (realHpDamage > 0)
        {
            OnDamagedButAlive();
        }

        Debug.Log($"[Damage] {unitName} take {amount}, realHpDamage = {realHpDamage}, HP = {currentHp}");
    }

    private void ReduceRegenerationOnDamage()
    {
        int regeneration = GetStatus(StatusType.Regeneration);

        if (regeneration <= 0)
            return;

        RemoveStatus(StatusType.Regeneration, 1);

        Debug.Log($"{unitName} 受到傷害，再生層數減少 1，剩餘 {GetStatus(StatusType.Regeneration)}");
    }
    protected virtual void OnAfterHpDamageTaken(int realHpDamage)
    {
    }
    public virtual void Heal(int amount)
    {
        currentHp += amount;

        if (currentHp > maxHp)
            currentHp = maxHp;

        OnHpChanged?.Invoke();

        Debug.Log($"{unitName} 回復 {amount} HP，當前 HP: {currentHp}");
    }

    public virtual void GainBlock(int amount)
    {
        int finalBlock = ModifyBlockGain(amount);

        block += finalBlock;

        OnHpChanged?.Invoke();

        Debug.Log($"{unitName} 獲得 {finalBlock} 格擋，當前格擋: {block}");
    }

    public virtual void ResetBlock()
    {
        block = 0;
        OnHpChanged?.Invoke();
    }

    public virtual void ApplyStatus(StatusType statusType, int amount)
    {
        if (amount <= 0)
            return;

        if (!statuses.ContainsKey(statusType))
            statuses[statusType] = 0;

        statuses[statusType] += amount;

        OnStatusChanged?.Invoke();

        Debug.Log($"{unitName} 獲得狀態 {statusType} x{amount}");
    }

    public virtual void RemoveStatus(StatusType statusType, int amount)
    {
        if (!statuses.ContainsKey(statusType))
            return;

        statuses[statusType] -= amount;

        if (statuses[statusType] <= 0)
            statuses.Remove(statusType);

        OnStatusChanged?.Invoke();
    }

    public int GetStatus(StatusType statusType)
    {
        return statuses.TryGetValue(statusType, out int value) ? value : 0;
    }

    public bool HasStatus(StatusType statusType)
    {
        return GetStatus(statusType) > 0;
    }
    public virtual void SetStatus(StatusType statusType, int amount)
    {
        if (amount <= 0)
        {
            if (statuses.ContainsKey(statusType))
                statuses.Remove(statusType);

            OnStatusChanged?.Invoke();
            return;
        }

        statuses[statusType] = amount;

        OnStatusChanged?.Invoke();

        Debug.Log($"{unitName} 的 {statusType} 被設定為 {amount}");
    }

    private void ResolvePoisonAtTurnStart()
    {
        int poison = GetStatus(StatusType.Poison);

        if (poison <= 0)
            return;

        Debug.Log($"{unitName} 中毒，受到 {poison} 傷害");

        TakeDamage(poison);

        RemoveStatus(StatusType.Poison, 1);
    }
    private void ResolveHardenAtTurnStart()
    {
        int harden = GetStatus(StatusType.Harden);

        if (harden <= 0)
            return;

        GainBlock(harden);

        Debug.Log($"{unitName} 的硬化發動，回合開始獲得 {harden} 點護盾");
    }
    private void ResolveRegenerationAtTurnStart()
    {
        int regeneration = GetStatus(StatusType.Regeneration);

        if (regeneration <= 0)
            return;

        if (currentHp <= 0)
            return;

        int healAmount = Mathf.CeilToInt(maxHp * regeneration * 0.05f);

        if (healAmount <= 0)
            healAmount = 1;

        Heal(healAmount);

        Debug.Log($"{unitName} 的再生發動，層數 {regeneration}，恢復 {healAmount} 點生命");
    }
    private void TickTemporaryStatuses()
    {
        TickStatus(StatusType.Weak);
        TickStatus(StatusType.Vulnerable);
        TickStatus(StatusType.Frail);

        // Strength 通常不自然下降，所以不 Tick。
        // Poison 已經在 OnTurnStart 裡處理。
    }

    private void TickStatus(StatusType statusType)
    {
        if (!statuses.ContainsKey(statusType))
            return;

        statuses[statusType]--;

        if (statuses[statusType] <= 0)
            statuses.Remove(statusType);

        OnStatusChanged?.Invoke();

        Debug.Log($"{unitName} 狀態 {statusType} 回合結束減少 1");
    }
    protected virtual void OnDamagedButAlive()
    {
    }
    protected virtual void Die()
    {
        Debug.Log($"{unitName} 死亡");
    }
}