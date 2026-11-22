using System;
using System.Collections.Generic;
using UnityEngine;
using EldritchMile.Core;

public class StartingRelicLoadout : MonoBehaviour
{
    public static StartingRelicLoadout Instance { get; private set; }

    [SerializeField, Min(1), Tooltip("開局可攜帶的遺物格數。")]
    private int slotCount = 1;

    private readonly List<string> relicIds = new List<string>();

    public event Action Changed;

    public int SlotCount => relicIds.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        for (int i = 0; i < Mathf.Max(1, slotCount); i++) relicIds.Add("");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public string GetRelicId(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < relicIds.Count ? relicIds[slotIndex] : "";
    }

    public bool CanAssign(int slotIndex, ItemData item, RelicWarehouse warehouse)
    {
        if (slotIndex < 0 || slotIndex >= relicIds.Count) return false;
        if (warehouse == null || !warehouse.IsRelic(item) || !warehouse.Owns(item.id)) return false;

        for (int i = 0; i < relicIds.Count; i++)
        {
            if (i != slotIndex && relicIds[i] == item.id) return false;
        }

        return true;
    }

    public bool TryAssign(int slotIndex, ItemData item, RelicWarehouse warehouse)
    {
        if (!CanAssign(slotIndex, item, warehouse)) return false;

        relicIds[slotIndex] = item.id;
        Changed?.Invoke();
        return true;
    }

    public void ClearSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= relicIds.Count) return;

        relicIds[slotIndex] = "";
        Changed?.Invoke();
    }

    public void ClearAll()
    {
        for (int i = 0; i < relicIds.Count; i++) relicIds[i] = "";
        Changed?.Invoke();
    }

    public List<string> CreateRelicIdSnapshot()
    {
        return new List<string>(relicIds);
    }
}