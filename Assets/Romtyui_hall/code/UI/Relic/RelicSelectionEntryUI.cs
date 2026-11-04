using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using EldritchMile.Core;

[RequireComponent(typeof(Button))]
public class RelicSelectionEntryUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField, Tooltip("遺物清單按鈕底下顯示 ShelfIcon 的 Image。")]
    private Image itemImage;

    private Button entryButton;
    private ItemData item;
    private RelicSelectionUI owner;
    [Header("格子背景")]
    [SerializeField, Tooltip("Button 本身的 Image。未指定時自動取得。")]
    private Image slotBackgroundImage;

    [SerializeField, Tooltip("沒有遺物時的格子圖片。")]
    private Sprite emptySlotSprite;

    [SerializeField, Tooltip("有遺物時的格子圖片。")]
    private Sprite occupiedSlotSprite;

    private void Awake()
    {
        entryButton = GetComponent<Button>();
        entryButton.onClick.AddListener(OnClick);
    }

    private void OnDestroy()
    {
        if (entryButton != null) entryButton.onClick.RemoveListener(OnClick);
    }

    public void Bind(ItemData data, RelicSelectionUI selectionUI)
    {
        item = data;
        owner = item != null ? selectionUI : null;

        if (entryButton == null) entryButton = GetComponent<Button>();
        if (slotBackgroundImage == null) slotBackgroundImage = GetComponent<Image>();

        bool hasItem = item != null;

        if (entryButton != null) entryButton.interactable = hasItem;

        if (slotBackgroundImage != null)
        {
            slotBackgroundImage.sprite = hasItem ? occupiedSlotSprite : emptySlotSprite;
            slotBackgroundImage.enabled = true;
        }

        if (itemImage != null)
        {
            itemImage.sprite = hasItem ? item.ShelfIcon : null;
            itemImage.enabled = itemImage.sprite != null;
        }
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