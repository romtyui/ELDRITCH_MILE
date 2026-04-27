using System.Collections.Generic;
using UnityEngine;

public class BattleUnit : MonoBehaviour
{
    public string unitName;
    public int maxHp = 100;
    public int currentHp;
    public int block;


    private Dictionary<StatusType, int> statuses = new();


    protected virtual void Awake()
    {
        currentHp = maxHp;
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
        if (currentHp < 0) currentHp = 0;

        Debug.Log($"{unitName} 受到 {amount} 傷害，剩餘 HP: {currentHp}");

        if (currentHp <= 0)
            Die();
        Debug.Log($"[Damage] {unitName} take {amount}, HP = {currentHp}");
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