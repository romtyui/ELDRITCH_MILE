using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using EldritchMile.Core;

[RequireComponent(typeof(Button))]
public class FoodSelectionEntryUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField, Tooltip("Button 底下顯示食物 ShelfIcon 的 Image。")]
    private Image itemImage;

    [SerializeField, Tooltip("可選：顯示倉庫持有數量的文字。")]
    private TMP_Text quantityText;

    private Button entryButton;
    private ItemData item;
    private FoodSelectionUI owner;

    [Header("格子背景")]
    [SerializeField, Tooltip("Button 本身的 Image。未指定時自動取得。")]
    private Image slotBackgroundImage;

    [SerializeField, Tooltip("沒有食物時的格子圖片。")]
    private Sprite emptySlotSprite;

    [SerializeField, Tooltip("有食物時的格子圖片。")]
    private Sprite occupiedSlotSprite;

    [SerializeField, Tooltip("食物被選取時的格子圖片。未指定時沿用有食物的圖片。")]
    private Sprite selectedSlotSprite;

    private void Awake()
    {
        entryButton = GetComponent<Button>();
        entryButton.onClick.AddListener(OnClick);
    }

    private void OnDestroy()
    {
        if (entryButton != null) entryButton.onClick.RemoveListener(OnClick);
    }

    public void Bind(ItemData data, int quantity, FoodSelectionUI selectionUI)
    {
        item = data;
        owner = item != null ? selectionUI : null;

        if (entryButton == null) entryButton = GetComponent<Button>();
        if (slotBackgroundImage == null) slotBackgroundImage = GetComponent<Image>();

        bool hasItem = item != null;

        if (entryButton != null) entryButton.interactable = hasItem;

        if (itemImage != null)
        {
            itemImage.sprite = hasItem ? item.displayIcon : null;
            itemImage.enabled = itemImage.sprite != null;
        }

        if (quantityText != null) quantityText.text = hasItem ? $"×{quantity}" : "";

        UpdateSelectionFrame(null);
    }

    public void UpdateSelectionFrame(ItemData selectedItem)
    {
        if (slotBackgroundImage == null) slotBackgroundImage = GetComponent<Image>();
        if (slotBackgroundImage == null) return;

        bool hasItem = item != null;
        bool isSelected = hasItem && selectedItem != null && item.id == selectedItem.id;

        Sprite backgroundSprite = hasItem ? occupiedSlotSprite : emptySlotSprite;
        if (isSelected && selectedSlotSprite != null) backgroundSprite = selectedSlotSprite;

        slotBackgroundImage.sprite = backgroundSprite;
        slotBackgroundImage.enabled = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (owner != null) owner.Preview(item);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (owner != null) owner.EndPreview(item);
    }

    private void OnClick()
    {
        if (owner != null) owner.Select(item);
    }
}