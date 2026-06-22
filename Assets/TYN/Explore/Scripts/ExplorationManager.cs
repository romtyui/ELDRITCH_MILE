using UnityEngine;
using System.Collections.Generic;

public class ExplorationManager : MonoBehaviour
{
    // 單例模式，方便其他腳本 (如 Door) 直接呼叫
    public static ExplorationManager Instance { get; private set; }

    [Header("探索設定")]
    public MapNodeExplore startingNode; // 遊戲開始時的第一個節點
    public Transform roomSpawnPoint; // 房間生成的中心錨點 (預設原點)

    [Header("轉場視覺效果")]
    [Tooltip("場景的主相機")]
    public Camera mainCamera; 
    [Tooltip("轉場時的黑畫面遮罩 (掛有 CanvasGroup 的 UI Panel)")]
    public CanvasGroup fadeCanvasGroup; 
    public float transitionSpeed = 0.3f; // 轉場速度

    [Header("卡牌系統連動")]
    [Tooltip("把場景上的 CardExplorationManager 拖進來")]
    public CardExplorationManager cardManager;

    [Header("當前狀態 (唯讀)")]
    public MapNodeExplore currentNode;
    private GameObject activeRoomInstance;
    public bool isTransitioning = false; // 防止連續點擊

    private float defaultFOV = 60f;

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
        if (mainCamera != null) defaultFOV = mainCamera.fieldOfView;
        
        // 確保一開始畫面是全黑的，準備淡入
        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 1f; 

        // --- 【方案A核心】：向大地圖索取要生成的房間資料 ---
        if (PerspectiveMapGenerator.Instance != null && PerspectiveMapGenerator.Instance.currentMapData != null)
        {
            string currentId = PerspectiveMapGenerator.Instance.currentMapData.currentNodeId;
            var currentData = PerspectiveMapGenerator.Instance.currentMapData.allNodes.Find(n => n.nodeId == currentId);
            
            if (currentData != null)
            {
                EnterRoom(currentData.templateData);
                return; // 成功從地圖取得資料，直接返回
            }
        }

        // 容錯處理：如果沒有地圖總管 (例如單獨開 ExploreScene 測試時)
        if (startingNode != null)
        {
            Debug.Log("[ExplorationManager] 找不到地圖資料，使用預設起始節點。");
            EnterRoom(startingNode);
        }
        else
        {
            Debug.LogError("[ExplorationManager] 找不到地圖資料且尚未設定 startingNode！");
        }
    }


    // 專注於「進入單一房間」的邏輯
    public void EnterRoom(MapNodeExplore node)
    {
        if (node == null || node.roomPrefab == null || isTransitioning) return;
        StartCoroutine(EnterRoutine(node));
    }

    private System.Collections.IEnumerator EnterRoutine(MapNodeExplore node)
    {
        isTransitioning = true;
        currentNode = node;

        // 生成房間
        if (activeRoomInstance != null) Destroy(activeRoomInstance);
        Vector3 spawnPos = roomSpawnPoint != null ? roomSpawnPoint.position : Vector3.zero;
        activeRoomInstance = Instantiate(node.roomPrefab, spawnPos, Quaternion.identity);
        activeRoomInstance.name = $"Room_{node.roomName}";

        RoomController controller = activeRoomInstance.GetComponent<RoomController>();
        if (controller != null) controller.InitializeRoom(node);

        // 重置手牌資源
        if (cardManager != null)
        {
            cardManager.DiscardHand();
            cardManager.DrawCards(cardManager.cardsOnEnterExploration);
            Debug.Log("[ExplorationManager] 進入新房間，重新抽取手牌。");
        }

        // 豁然開朗 (恢復 FOV) 與 畫面變亮
        float timeElapsed = 0f;
        while (timeElapsed < transitionSpeed)
        {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / transitionSpeed;
            float smoothT = 1f - (1f - t) * (1f - t); // Ease Out

            if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, smoothT);
            if (mainCamera != null) mainCamera.fieldOfView = Mathf.Lerp(defaultFOV - 20f, defaultFOV, smoothT);
            yield return null;
        }

        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 0f;
        if (mainCamera != null) mainCamera.fieldOfView = defaultFOV;

        isTransitioning = false;
        Debug.Log($"[ExplorationManager] 成功進入節點: {node.roomName}");
    }

    // 專注於「退出探索場景，返回大地圖」的邏輯
    public void ExitExploreScene()
    {
        if (isTransitioning) return;
        StartCoroutine(ExitRoutine());
    }

    private System.Collections.IEnumerator ExitRoutine()
    {
        isTransitioning = true;

        // 往前衝刺 (縮小 FOV) 與 畫面變黑
        float timeElapsed = 0f;
        while (timeElapsed < transitionSpeed)
        {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / transitionSpeed;
            float smoothT = t * t; // Ease In

            if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, smoothT);
            if (mainCamera != null) mainCamera.fieldOfView = Mathf.Lerp(defaultFOV, defaultFOV - 20f, smoothT);
            yield return null;
        }

        // --- 【方案A核心】：通知大地圖總管把場景接回去 ---
        if (PerspectiveMapGenerator.Instance != null)
        {
            Debug.Log("[ExplorationManager] 探索結束，交還控制權給大地圖。");
            PerspectiveMapGenerator.Instance.ReturnToMap();
        }
        else
        {
            Debug.LogWarning("[ExplorationManager] 找不到大地圖總管，無法卸載場景！(可能是在單場景測試中)");
            isTransitioning = false; 
        }
    }
}