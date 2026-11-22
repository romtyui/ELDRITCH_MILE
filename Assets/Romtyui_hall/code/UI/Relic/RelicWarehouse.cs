using System;
using System.Collections.Generic;
using UnityEngine;
using EldritchMile.Core;

public class RelicWarehouse : MonoBehaviour
{
    [Serializable]
    private class SaveData
    {
        public List<string> itemIds = new List<string>();
    }

    public static RelicWarehouse Instance { get; private set; }

    [SerializeField, Tooltip("遺物資料所在的道具資料庫。")]
    private ItemDatabase database;

    [SerializeField, Tooltip("尚無存檔時持有的遺物。ItemData 必須具有 Curio 標籤。")]
    private List<ItemData> initialRelics = new List<ItemData>();

    [SerializeField, Tooltip("遺物倉庫的獨立存檔識別名稱。")]
    private string saveKey = "StartSelection.RelicWarehouse.v1";

    private readonly List<string> ownedIds = new List<string>();

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

    public bool IsRelic(ItemData item)
    {
        return item != null && !string.IsNullOrWhiteSpace(item.id) && item.HasTag("Curio");
    }

    public ItemData GetItem(string itemId)
    {
        return database != null ? database.GetById(itemId) : null;
    }

    public bool Owns(string itemId)
    {
        return !string.IsNullOrEmpty(itemId) && ownedIds.Contains(itemId);
    }

    public List<ItemData> GetOwnedRelics()
    {
        List<ItemData> result = new List<ItemData>();

        foreach (string itemId in ownedIds)
        {
            ItemData item = GetItem(itemId);
            if (IsRelic(item)) result.Add(item);
        }

        return result;
    }

    public bool AddRelic(string itemId)
    {
        if (!IsRelic(GetItem(itemId)) || Owns(itemId)) return false;

        ownedIds.Add(itemId);
        Save();
        Changed?.Invoke();
        return true;
    }

    public bool RemoveRelic(string itemId)
    {
        if (!ownedIds.Remove(itemId)) return false;

        Save();
        Changed?.Invoke();
        return true;
    }

    private void Load()
    {
        ownedIds.Clear();

        if (!PlayerPrefs.HasKey(saveKey))
        {
            ResetToInitialRelics();
            return;
        }

        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(saveKey));
            if (data == null || data.itemIds == null) return;

            foreach (string itemId in data.itemIds)
            {
                if (string.IsNullOrWhiteSpace(itemId) || ownedIds.Contains(itemId)) continue;
                ownedIds.Add(itemId);
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"遺物倉庫讀取失敗：{exception.Message}", this);
        }
    }

    private void Save()
    {
        SaveData data = new SaveData { itemIds = new List<string>(ownedIds) };
        PlayerPrefs.SetString(saveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    [ContextMenu("用初始清單重設遺物倉庫（執行期間）")]
    public void ResetToInitialRelics()
    {
        if (!Application.isPlaying) return;

        ownedIds.Clear();

        foreach (ItemData item in initialRelics)
        {
            if (!IsRelic(item) || ownedIds.Contains(item.id)) continue;
            ownedIds.Add(item.id);
        }

        Save();
        Changed?.Invoke();
    }
}