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

    private Dictionary<StatusType, int> statuses = new();

    [Header("Hit Feedback")]
    public HitFlashObject hitFlashObject;

    protected virtual void Awake()
    {
        currentHp = maxHp;
        OnHpChanged?.Invoke();
    }

    public virtual void TakeDamage(int amount)
    {
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

        OnHpChanged?.Invoke();

        if (remaining > 0 && hitFlashObject != null)
            hitFlashObject.Play();

        Debug.Log($"{unitName} 受到 {amount} 傷害，剩餘 HP: {currentHp}");

        if (currentHp <= 0)
            Die();

        Debug.Log($"[Damage] {unitName} take {amount}, HP = {currentHp}");
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
        block += amount;
        Debug.Log($"{unitName} 獲得 {amount} 格擋，當前格擋: {block}");
    }

    public virtual void ResetBlock()
    {
        block = 0;
    }

    public virtual void ApplyStatus(StatusType statusType, int amount)
    {
        if (!statuses.ContainsKey(statusType))
            statuses[statusType] = 0;

        statuses[statusType] += amount;

        Debug.Log($"{unitName} 獲得狀態 {statusType} x{amount}");
    }

    public int GetStatus(StatusType statusType)
    {
        return statuses.TryGetValue(statusType, out int value) ? value : 0;
    }

    protected virtual void Die()
    {
        Debug.Log($"{unitName} 死亡");
    }
}