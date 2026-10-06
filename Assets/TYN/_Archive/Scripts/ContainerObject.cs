using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// ⚠️ 封存的舊碼。包進 namespace 是為了**不要污染全域命名空間** ——
//    這裡宣告的 RunNodeData / MapData / ICardInteractable 等等
//    與正式碼同名，放在全域會蓋掉正式那組，症狀是新檔案裡出現
//    「MapData 沒有 GetNode」這種完全不會指向這裡的錯誤。
namespace EldritchMile.Archive
{

public class ContainerObject : MonoBehaviour, IPointerClickHandler
{
    [Header("容器設定")]
    public string containerName = "舊保險箱";
    
    [Tooltip("這個箱子裡包含的道具名稱 (MVP階段用文字代替卡牌資料)")]
    public List<string> lootItems = new List<string>();

    [Header("視覺切換 (非必填)")]
    [Tooltip("此物件身上的 SpriteRenderer (若無可留空)")]
    public SpriteRenderer targetRenderer;
    [Tooltip("打開後替換的圖案 (例如：開著的寶箱)。若無素材請留空。")]
    public Sprite openedSprite;

    private bool isOpened = false;
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
        if (isOpened) return; // 已經開過的箱子不能再開

        isOpened = true;

        // 替換打開後的圖片
        if (targetRenderer != null && openedSprite != null)
        {
            targetRenderer.sprite = openedSprite;
        }
        
        // 呼叫 UI 顯示 Loot 介面
        UIManager.Instance.ShowLootUI(containerName, lootItems);

        // 回報探索進度
        if (roomController != null) roomController.ReportInteraction();
    }
}
}
