using System.Collections.Generic;
using UnityEngine;

// ⚠️ 封存的舊碼。包進 namespace 是為了**不要污染全域命名空間** ——
//    這裡宣告的 RunNodeData / MapData / ICardInteractable 等等
//    與正式碼同名，放在全域會蓋掉正式那組，症狀是新檔案裡出現
//    「MapData 沒有 GetNode」這種完全不會指向這裡的錯誤。
namespace EldritchMile.Archive
{

public class RoomController : MonoBehaviour
{
    [Header("房間出口設定")]
    [Tooltip("請將房間內掛載了 Door 腳本的 GameObject 拖曳至此清單")]
    public List<Door> doors;

    private MapNodeExplore nodeData;
    private int totalInteractables = 0;
    private int currentInteracted = 0;
    private bool isCleared = false;

    public void InitializeRoom(MapNodeExplore node)
    {
        nodeData = node;

        // 1. 設定門 (不再需要讀取節點拓撲，直接全部開啟作為返回地圖的出口)
        for (int i = 0; i < doors.Count; i++)
        {
            if (doors[i] != null)
            {
                doors[i].gameObject.SetActive(true);
            }
        }

        // 2. 自動計算房間內有多少「需調查物件」與「容器」
        InspectableObject[] inspectables = GetComponentsInChildren<InspectableObject>(true);
        ContainerObject[] containers = GetComponentsInChildren<ContainerObject>(true);
        totalInteractables = inspectables.Length + containers.Length;
        
        // 如果房間一開始就沒有東西可以點，直接視為完成
        if (totalInteractables == 0) ReportInteraction(); 
    }

    // 由物件或容器被點擊時呼叫
    public void ReportInteraction()
    {
        if (isCleared) return;

        currentInteracted++;
        if (currentInteracted >= totalInteractables)
        {
            isCleared = true;
            if (!string.IsNullOrEmpty(nodeData.roomClearText))
            {
                // 將總結文本送入 UIManager 進行排隊
                UIManager.Instance.QueueRoomClearText(nodeData.roomClearText);
            }
        }
    }
}
}
