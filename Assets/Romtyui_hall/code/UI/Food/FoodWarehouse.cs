using System;
using System.Collections.Generic;
using UnityEngine;
using EldritchMile.Core;

public class FoodWarehouse : MonoBehaviour
{
    [Serializable]
    public class InitialFood
    {
        public ItemData item;

        [Min(1)]
        public int quantity = 1;
    }

    [Serializable]
    public class Stock
    {
        public string itemId;
        public int quantity;
    }

    [Serializable]
    private class SaveData
    {
        public List<Stock> stocks = new List<Stock>();
    }

    public static FoodWarehouse Instance { get; private set; }

    [SerializeField, Tooltip("用 ID 查找道具資料的資料庫。")]
    private ItemDatabase database;

    [SerializeField, Tooltip("判定為食物所需的標籤，必須與 ItemData.tags 的實際內容一致。")]
    private string foodTag = "Food";

    [SerializeField, Tooltip("尚無倉庫存檔時的初始持有內容。")]
    private List<InitialFood> initialFoods = new List<InitialFood>();

    [SerializeField, Tooltip("倉庫的本機存檔識別名稱。")]
    private string saveKey = "StartSelection.FoodWarehouse.v1";

    private List<Stock> stocks = new List<Stock>();

    public event Action Changed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool IsFood(ItemData item)
    {
        return item != null && !string.IsNullOrWhiteSpace(item.id) && item.HasTag(foodTag);
    }

    public ItemData GetItem(string itemId)
    {
        return database != null ? database.GetById(itemId) : null;
    }

    public int GetQuantity(string itemId)
    {
        Stock stock = stocks.Find(entry => entry.itemId == itemId);
        return stock != null ? stock.quantity : 0;
    }

    public List<ItemData> GetOwnedFoods()
    {
        List<ItemData> result = new List<ItemData>();

        foreach (Stock stock in stocks)
        {
            if (stock.quantity <= 0) continue;

            ItemData item = GetItem(stock.itemId);
            if (IsFood(item)) result.Add(item);
        }

        return result;
    }

    public bool AddFood(string itemId, int quantity)
    {
        ItemData item = GetItem(itemId);
        if (!IsFood(item) || quantity <= 0) return false;

        AddStock(itemId, quantity);
        Save();
        Changed?.Invoke();
        return true;
    }

    public bool RemoveFood(string itemId, int quantity)
    {
        if (quantity <= 0) return false;

        Stock stock = stocks.Find(entry => entry.itemId == itemId);
        if (stock == null || stock.quantity < quantity) return false;

        stock.quantity -= quantity;
        if (stock.quantity == 0) stocks.Remove(stock);

        Save();
        Changed?.Invoke();
        return true;
    }

    private void AddStock(string itemId, int quantity)
    {
        Stock stock = stocks.Find(entry => entry.itemId == itemId);

        if (stock == null)
        {
            stocks.Add(new Stock { itemId = itemId, quantity = quantity });
            return;
        }

        stock.quantity += quantity;
    }

    private void Load()
    {
        stocks.Clear();

        if (!PlayerPrefs.HasKey(saveKey))
        {
            ResetToInitialFoods();
            return;
        }

        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(saveKey));
            if (data == null || data.stocks == null) return;

            foreach (Stock stock in data.stocks)
            {
                if (stock == null || string.IsNullOrWhiteSpace(stock.itemId) || stock.quantity <= 0) continue;
                AddStock(stock.itemId, stock.quantity);
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"食物倉庫讀取失敗：{exception.Message}", this);
        }
    }

    private void Save()
    {
        SaveData data = new SaveData { stocks = stocks };
        PlayerPrefs.SetString(saveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    [ContextMenu("用初始清單重設倉庫（執行期間）")]
    public void ResetToInitialFoods()
    {
        if (!Application.isPlaying) return;

        stocks.Clear();

        foreach (InitialFood entry in initialFoods)
        {
            if (entry == null || !IsFood(entry.item) || entry.quantity <= 0) continue;
            AddStock(entry.item.id, entry.quantity);
        }

        Save();
        Changed?.Invoke();
    }
}