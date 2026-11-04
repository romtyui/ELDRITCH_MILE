using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EldritchMile.Core;

public class RelicSelectionUI : MonoBehaviour
{
    [Serializable]
    public class RelicCategory
    {
        public string label;

        [Tooltip("此分類需要的標籤。留空代表全部持有遺物。")]
        public string requiredTag;
    }

    [Header("資料")]
    [SerializeField] private RelicWarehouse warehouse;
    [SerializeField] private StartingRelicLoadout loadout;
    [SerializeField] private StartSelectionPageUI pageUI;

    [Header("遺物清單")]
    [SerializeField] private Transform content;
    [SerializeField] private RelicSelectionEntryUI entryTemplate;
    [SerializeField] private ScrollRect scrollRect;

    [Header("遺物分類")]
    [SerializeField] private List<RelicCategory> categories = new List<RelicCategory>();

    [Header("遺物詳細資料")]
    [SerializeField, Tooltip("包含圖片、名稱、描述與確認按鈕的詳細資料根物件。沒有預覽或選取內容時隱藏。")]
    private GameObject detailRoot;
    [SerializeField] private Image detailIcon;
    [SerializeField] private TMP_Text detailName;
    [SerializeField] private TMP_Text detailDescription;
    [SerializeField] private Button confirmButton;

    [Header("主畫面遺物格")]
    [SerializeField, Tooltip("Relics_Root 下各個 Options 的子 Image，顯示遺物圖片。")]
    private Image[] slotImages = new Image[1];

    [SerializeField, Tooltip("Relics_Root 下各個 Options 本身的 Image，顯示有／無遺物外框。")]
    private Image[] slotBackgroundImages = new Image[1];

    [SerializeField] private Sprite emptySlotSprite;
    [SerializeField] private Sprite occupiedSlotSprite;

    private readonly List<RelicSelectionEntryUI> spawnedEntries = new List<RelicSelectionEntryUI>();


    [SerializeField, Min(0), Tooltip("遺物清單至少顯示的格子數量。資料不足時補空格，超過時自動增加。")]
    private int minimumSlotCount = 35;

    private ItemData selectedItem;
    private ItemData previewedItem;
    private int editingSlotIndex = -1;
    private int categoryIndex;
    private bool initialized;

    private void Start()
    {
        if (RelicWarehouse.Instance != null) warehouse = RelicWarehouse.Instance;
        if (StartingRelicLoadout.Instance != null) loadout = StartingRelicLoadout.Instance;

        if (entryTemplate != null) entryTemplate.gameObject.SetActive(false);
        if (confirmButton != null) confirmButton.onClick.AddListener(ConfirmSelection);
        if (warehouse != null) warehouse.Changed += OnDataChanged;
        if (loadout != null) loadout.Changed += OnDataChanged;

        initialized = true;
        RefreshSlotImages();
        ShowDetails(null);
    }

    private void OnDestroy()
    {
        if (confirmButton != null) confirmButton.onClick.RemoveListener(ConfirmSelection);
        if (warehouse != null) warehouse.Changed -= OnDataChanged;
        if (loadout != null) loadout.Changed -= OnDataChanged;
    }

    public bool BeginSelection(int slotIndex)
    {
        if (!initialized || warehouse == null || loadout == null || content == null || entryTemplate == null)
        {
            Debug.LogWarning($"遺物選擇無法開啟：初始化完成={initialized}、Warehouse已指定={warehouse != null}、Loadout已指定={loadout != null}、Content已指定={content != null}、EntryTemplate已指定={entryTemplate != null}", this);
            return false;
        }

        if (slotIndex < 0 || slotIndex >= loadout.SlotCount)
        {
            Debug.LogWarning("遺物格編號超出 StartingRelicLoadout 的格數。", this);
            return false;
        }

        editingSlotIndex = slotIndex;
        selectedItem = null;
        previewedItem = null;

        RebuildList();
        ShowDetails(null);
        return true;
    }

    public void SetCategory(int index)
    {
        if (index < 0 || index >= categories.Count) return;

        categoryIndex = index;
        previewedItem = null;
        RebuildList();
        ShowDetails(selectedItem);
    }

    private void RebuildList()
    {
        foreach (RelicSelectionEntryUI entry in spawnedEntries)
        {
            if (entry == null) continue;

            entry.gameObject.SetActive(false);
            Destroy(entry.gameObject);
        }

        spawnedEntries.Clear();

        if (warehouse == null || content == null || entryTemplate == null) return;

        entryTemplate.gameObject.SetActive(false);

        string requiredTag = "";

        if (categoryIndex >= 0 && categoryIndex < categories.Count && categories[categoryIndex] != null) requiredTag = categories[categoryIndex].requiredTag;

        List<ItemData> visibleItems = new List<ItemData>();

        foreach (ItemData item in warehouse.GetOwnedRelics())
        {
            if (!string.IsNullOrWhiteSpace(requiredTag) && !item.HasTag(requiredTag)) continue;
            visibleItems.Add(item);
        }

        int totalSlots = Mathf.Max(minimumSlotCount, visibleItems.Count);
        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();

        if (grid != null && grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
        {
            int columns = Mathf.Max(1, grid.constraintCount);
            totalSlots = Mathf.CeilToInt(totalSlots / (float)columns) * columns;
        }

        for (int i = 0; i < totalSlots; i++)
        {
            ItemData item = i < visibleItems.Count ? visibleItems[i] : null;

            RelicSelectionEntryUI entry = Instantiate(entryTemplate, content);
            entry.Bind(item, this);
            entry.gameObject.SetActive(true);
            spawnedEntries.Add(entry);
        }

        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 1f;
            scrollRect.horizontalNormalizedPosition = 0f;
        }
    }

    public void Preview(ItemData item)
    {
        if (selectedItem != null || !IsEditing()) return;

        previewedItem = item;
        ShowDetails(item);
    }

    public void EndPreview(ItemData item)
    {
        if (selectedItem != null || previewedItem != item) return;

        previewedItem = null;
        ShowDetails(null);
    }

    public void Select(ItemData item)
    {
        if (!IsEditing() || warehouse == null || !warehouse.IsRelic(item)) return;

        if (selectedItem != null && selectedItem.id == item.id)
        {
            selectedItem = null;
            previewedItem = item;
            ShowDetails(item);
            return;
        }

        selectedItem = item;
        previewedItem = null;
        ShowDetails(item);
    }

    private bool IsEditing()
    {
        return pageUI != null && pageUI.IsRelicSelectionOpen && pageUI.EditingRelicSlotIndex == editingSlotIndex;
    }

    private void ShowDetails(ItemData item)
    {
        if (detailIcon != null)
        {
            detailIcon.sprite = item != null ? item.ShelfIcon : null;
            detailIcon.enabled = detailIcon.sprite != null;
        }

        if (detailName != null) detailName.text = item != null ? item.Label : "";
        if (detailDescription != null) detailDescription.text = item != null ? item.FullText : "";

        if (confirmButton != null) confirmButton.interactable = selectedItem != null && loadout != null && warehouse != null && loadout.CanAssign(editingSlotIndex, selectedItem, warehouse);

        if (detailRoot != null) detailRoot.SetActive(item != null);
    }

    public void ConfirmSelection()
    {
        if (!IsEditing() || selectedItem == null || loadout == null || warehouse == null) return;

        if (!loadout.TryAssign(editingSlotIndex, selectedItem, warehouse))
        {
            ShowDetails(selectedItem);
            return;
        }

        selectedItem = null;
        previewedItem = null;
        editingSlotIndex = -1;
        ShowDetails(null);
        pageUI.CloseRelicSelection();
    }

    private void OnDataChanged()
    {
        RefreshSlotImages();

        if (editingSlotIndex >= 0) RebuildList();

        ShowDetails(selectedItem != null ? selectedItem : previewedItem);
    }

    private void RefreshSlotImages()
    {
        if (warehouse == null || loadout == null) return;

        int itemImageCount = slotImages != null ? slotImages.Length : 0;
        int backgroundImageCount = slotBackgroundImages != null ? slotBackgroundImages.Length : 0;
        int count = Mathf.Max(itemImageCount, backgroundImageCount);

        for (int i = 0; i < count; i++)
        {
            ItemData item = warehouse.GetItem(loadout.GetRelicId(i));
            bool hasItem = item != null;

            if (i < itemImageCount)
            {
                Image itemImage = slotImages[i];

                if (itemImage != null)
                {
                    Sprite sprite = hasItem ? (item.icon != null ? item.icon : item.ShelfIcon) : null;
                    itemImage.sprite = sprite;
                    itemImage.enabled = sprite != null;
                }
            }

            if (i < backgroundImageCount)
            {
                Image backgroundImage = slotBackgroundImages[i];

                if (backgroundImage != null)
                {
                    backgroundImage.sprite = hasItem ? occupiedSlotSprite : emptySlotSprite;
                    backgroundImage.enabled = true;
                }
            }
        }
    }
}