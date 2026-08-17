using UnityEngine;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Prop 底下的 Image，目前有道具時顯示，沒有道具時透明。")]
    public Image itemBackgroundImage;

    [Tooltip("Prop 底下的針筒 Image。")]
    public Image syringeImage;

    [Header("Syringe Sprites")]
    [Tooltip("沒有道具時顯示的針筒圖片。")]
    public Sprite emptySyringeSprite;

    [Tooltip("有道具時顯示的針筒圖片。")]
    public Sprite filledSyringeSprite;

    [Header("Temporary Filled Visual")]
    [Tooltip("目前還不知道正式道具資料的顏色欄位，所以先用這個顏色測試有道具狀態。")]
    public Color filledBackgroundColor = Color.white;

    [Header("Runtime")]
    [SerializeField]
    private int slotIndex = -1;

    private ScriptableObject currentItem;

    public int SlotIndex => slotIndex;

    public ScriptableObject CurrentItem => currentItem;

    public void Bind(
        ScriptableObject item,
        int index
    )
    {
        slotIndex = index;
        currentItem = item;

        if (currentItem != null)
        {
            ShowItem();
        }
        else
        {
            ShowEmpty();
        }
    }

    private void ShowItem()
    {
        if (itemBackgroundImage != null)
        {
            Color color = filledBackgroundColor;

            // 有道具時一定完全顯示
            color.a = 1f;

            itemBackgroundImage.color = color;
        }

        if (syringeImage != null)
        {
            if (filledSyringeSprite != null)
            {
                syringeImage.sprite =
                    filledSyringeSprite;
            }

            Color syringeColor =
                syringeImage.color;

            syringeColor.a = 1f;

            syringeImage.color =
                syringeColor;

            syringeImage.enabled = true;
        }
    }

    private void ShowEmpty()
    {
        if (itemBackgroundImage != null)
        {
            Color color =
                itemBackgroundImage.color;

            // Empty 時完全透明
            color.a = 0f;

            itemBackgroundImage.color =
                color;
        }

        if (syringeImage != null)
        {
            if (emptySyringeSprite != null)
            {
                syringeImage.sprite =
                    emptySyringeSprite;
            }

            // 空針筒本身仍然顯示
            Color syringeColor =
                syringeImage.color;

            syringeColor.a = 1f;

            syringeImage.color =
                syringeColor;

            syringeImage.enabled = true;
        }
    }
}