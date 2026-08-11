using UnityEngine;
using UnityEngine.EventSystems;

public class InspectableObject : MonoBehaviour, IPointerClickHandler
{
    public enum InteractType { Information, Pickup }
    
    [Header("互動設定")]
    public InteractType type = InteractType.Information;
    
    [Tooltip("若是 Information 則填寫世界觀；若是 Pickup 則填寫獲得的道具名稱")]
    [TextArea(2, 5)]
    public string contentText = "一疊舊報紙...";

    [Header("視覺切換 (非必填)")]
    [Tooltip("此物件身上的 SpriteRenderer (若無可留空)")]
    public SpriteRenderer targetRenderer;
    [Tooltip("調查後替換的圖案 (例如：翻開的書)。若無素材請留空。")]
    public Sprite investigatedSprite;

    private bool hasInteracted = false;
    private RoomController roomController;

    private void Start()
    {
        roomController = GetComponentInParent<RoomController>();
        // 懶人包：如果沒有手動拖曳，程式自動嘗試抓取同一物件上的 SpriteRenderer
        if (targetRenderer == null) 
        {
            targetRenderer = GetComponent<SpriteRenderer>();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (hasInteracted && type == InteractType.Pickup) return;

        // 顯示文本
        if (type == InteractType.Information)
        {
            UIManager.Instance.ShowPopupText(contentText);
        }
        else if (type == InteractType.Pickup)
        {
            UIManager.Instance.ShowPopupText($"獲得道具：\n{contentText}");
            gameObject.SetActive(false); // 拾取後隱藏模型
        }

        // 回報進度 (只回報一次)
        if (!hasInteracted)
        {
            hasInteracted = true;
            // 如果是保留型物件，且有設定替換圖片，則切換 Sprite
            if (type == InteractType.Information && targetRenderer != null && investigatedSprite != null)
            {
                targetRenderer.sprite = investigatedSprite;
            }
            
            if (roomController != null) roomController.ReportInteraction();
        }
    }
}