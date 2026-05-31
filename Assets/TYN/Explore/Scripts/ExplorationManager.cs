using UnityEngine;
using System.Collections.Generic;

public class ExplorationManager : MonoBehaviour
{
    // 單例模式，方便其他腳本 (如 Door) 直接呼叫
    public static ExplorationManager Instance { get; private set; }

    [Header("探索設定")]
    public MapNodeExplore startingNode; // 遊戲開始時的第一個節點
    public Transform roomSpawnPoint; // 房間生成的中心錨點 (預設原點)

    [Header("當前狀態 (唯讀)")]
    public MapNodeExplore currentNode;
    private GameObject activeRoomInstance;

    public List<MapNodeExplore> historyNodes = new List<MapNodeExplore>(); // 紀錄歷史節點 (可選)
    public MiniMapManager miniMapManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // 遊戲開始時自動載入起點房間
        if (startingNode != null)
        {
            TransitionToNode(startingNode);
        }
        else
        {
            Debug.LogError("[ExplorationManager] 尚未設定起始節點 (startingNode)！");
        }
    }

    public void TransitionToNode(MapNodeExplore newNode)
    {
        if (newNode == null || newNode.roomPrefab == null)
        {
            Debug.LogError("[ExplorationManager] 節點無效或缺少預製件！");
            return;
        }

        // 【新增】如果不是空歷史，就把當前節點存入歷史中
        if (currentNode != null) 
        {
            historyNodes.Add(currentNode);
        }

        // 1. 清理舊房間實體
        if (activeRoomInstance != null) Destroy(activeRoomInstance);

        // 2. 更新當前節點狀態
        currentNode = newNode;

        // 3. 生成新房間實體
        Vector3 spawnPos = roomSpawnPoint != null ? roomSpawnPoint.position : Vector3.zero;
        activeRoomInstance = Instantiate(newNode.roomPrefab, spawnPos, Quaternion.identity);
        activeRoomInstance.name = $"Room_{newNode.roomName}";

        // 4. 設定房間內的門與節點拓撲
        RoomController controller = activeRoomInstance.GetComponent<RoomController>();
        if (controller != null)
        {
            // 將整個節點資料傳入，以便讀取門的連接與完成文本
            controller.InitializeRoom(newNode); 
        }
        // 【新增】每次切換房間後，更新小地圖
        if (miniMapManager != null)
        {
            miniMapManager.DrawMap(currentNode, historyNodes);
        }

        Debug.Log($"[ExplorationManager] 成功進入節點: {newNode.roomName}");
        
        // --- 可以在這裡擴充 UI 更新邏輯 ---
        // 例如：UIManager.Instance.UpdateStoryText(newNode.entryText);
    }
}