using System;
using System.Collections.Generic;
using UnityEngine;
using EldritchMile.Core;

public class StartingFoodLoadout : MonoBehaviour
{
    public static StartingFoodLoadout Instance { get; private set; }

    [SerializeField, Min(1), Tooltip("開局可以攜帶的食物格數。")]
    private int slotCount = 3;

    private readonly List<string> foodIds = new List<string>();

    public event Action Changed;

    public int SlotCount => foodIds.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        for (int i = 0; i < Mathf.Max(1, slotCount); i++) foodIds.Add("");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public string GetFoodId(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < foodIds.Count ? foodIds[slotIndex] : "";
    }

    public int GetAssignedCount(string itemId, int excludedSlotIndex = -1)
    {
        if (string.IsNullOrEmpty(itemId)) return 0;

        int count = 0;

        for (int i = 0; i < foodIds.Count; i++)
        {
            if (i != excludedSlotIndex && foodIds[i] == itemId) count++;
        }

        return count;
    }

    public bool CanAssign(int slotIndex, ItemData item, FoodWarehouse warehouse)
    {
        if (slotIndex < 0 || slotIndex >= foodIds.Count) return false;
        if (warehouse == null || !warehouse.IsFood(item)) return false;

        int assignedElsewhere = GetAssignedCount(item.id, slotIndex);
        return warehouse.GetQuantity(item.id) > assignedElsewhere;
    }

    public bool TryAssign(int slotIndex, ItemData item, FoodWarehouse warehouse)
    {
        if (!CanAssign(slotIndex, item, warehouse)) return false;

        foodIds[slotIndex] = item.id;
        Changed?.Invoke();
        return true;
    }

    public void ClearSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= foodIds.Count) return;

        foodIds[slotIndex] = "";
        Changed?.Invoke();
    }

    public void ClearAll()
    {
        for (int i = 0; i < foodIds.Count; i++) foodIds[i] = "";
        Changed?.Invoke();
    }

    public List<string> CreateFoodIdSnapshot()
    {
        return new List<string>(foodIds);
    }
}