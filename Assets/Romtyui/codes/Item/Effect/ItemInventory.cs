using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemInventory : MonoBehaviour
{
    [Header("Settings")]
    [Min(1)]
    public int maxItemCount = 3;

    [Header("Current Items")]
    [SerializeField]
    private List<ScriptableObject> currentItems = new();

    public IReadOnlyList<ScriptableObject> CurrentItems => currentItems;

    public int Count => currentItems.Count;
    public bool HasSpace => currentItems.Count < maxItemCount;
    public bool IsFull => currentItems.Count >= maxItemCount;

    public event Action OnInventoryChanged;

    public bool AddItem(ScriptableObject item)
    {
        if (item == null)
        {
            Debug.LogWarning("[ItemInventory] AddItem 失敗，item 是 null");
            return false;
        }

        if (IsFull)
        {
            Debug.Log(
                $"[ItemInventory] 道具欄已滿：{currentItems.Count}/{maxItemCount}"
            );
            return false;
        }

        currentItems.Add(item);

        Debug.Log(
            $"[ItemInventory] 加入道具：{item.name}，目前 {currentItems.Count}/{maxItemCount}"
        );

        OnInventoryChanged?.Invoke();

        return true;
    }

    public ScriptableObject GetItem(int index)
    {
        if (index < 0 || index >= currentItems.Count)
            return null;

        return currentItems[index];
    }

    public bool HasItemAt(int index)
    {
        return index >= 0 &&
               index < currentItems.Count &&
               currentItems[index] != null;
    }

    public bool RemoveItemAt(int index)
    {
        if (index < 0 || index >= currentItems.Count)
        {
            Debug.LogWarning(
                $"[ItemInventory] RemoveItemAt 索引超出範圍：{index}"
            );
            return false;
        }

        ScriptableObject item = currentItems[index];

        currentItems.RemoveAt(index);

        Debug.Log(
            $"[ItemInventory] 移除道具：" +
            $"{(item != null ? item.name : "null")}"
        );

        OnInventoryChanged?.Invoke();

        return true;
    }

    public void Clear()
    {
        currentItems.Clear();

        OnInventoryChanged?.Invoke();

        Debug.Log("[ItemInventory] 已清空所有道具");
    }
}