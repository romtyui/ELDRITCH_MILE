using UnityEngine;
using System.Collections.Generic;

public class ExplorationManager : MonoBehaviour
{
    public static ExplorationManager Instance { get; private set; }

    [Header("探索設定")]
    public MapNodeExplore startingNode; // 容錯用 (單一場景測試)
    public Transform roomSpawnPoint; 

    [Header("轉場視覺效果")]
    public Camera mainCamera; 
    public CanvasGroup fadeCanvasGroup; 
    public float transitionSpeed = 0.3f; 

    [Header("卡牌系統連動")]
    public CardExplorationManager cardManager;

    [Header("當前狀態 (唯讀)")]
    public MapNodeExplore currentNode;
    private GameObject activeRoomInstance;
    public bool isTransitioning = false; 

    private float defaultFOV = 60f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (mainCamera != null) defaultFOV = mainCamera.fieldOfView;
        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 1f; 

        // 向大地圖索取要生成的房間資料
        if (PerspectiveMapGenerator.Instance != null && PerspectiveMapGenerator.Instance.currentMapData != null)
        {
            string currentId = PerspectiveMapGenerator.Instance.currentMapData.currentNodeId;
            var currentData = PerspectiveMapGenerator.Instance.currentMapData.allNodes.Find(n => n.nodeId == currentId);
            
            if (currentData != null)
            {
                EnterRoom(currentData.templateData);
                return;
            }
        }

        // 容錯處理：單一場景測試
        if (startingNode != null) EnterRoom(startingNode);
    }

    public void EnterRoom(MapNodeExplore node)
    {
        if (node == null || node.roomPrefab == null || isTransitioning) return;
        StartCoroutine(EnterRoutine(node));
    }

    private System.Collections.IEnumerator EnterRoutine(MapNodeExplore node)
    {
        isTransitioning = true;
        currentNode = node;

        if (activeRoomInstance != null) Destroy(activeRoomInstance);
        Vector3 spawnPos = roomSpawnPoint != null ? roomSpawnPoint.position : Vector3.zero;
        activeRoomInstance = Instantiate(node.roomPrefab, spawnPos, Quaternion.identity);
        
        RoomController controller = activeRoomInstance.GetComponent<RoomController>();
        if (controller != null) controller.InitializeRoom(node);

        if (cardManager != null)
        {
            cardManager.DiscardHand();
            cardManager.DrawCards(cardManager.cardsOnEnterExploration);
        }

        float timeElapsed = 0f;
        while (timeElapsed < transitionSpeed)
        {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / transitionSpeed;
            float smoothT = 1f - (1f - t) * (1f - t);

            if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, smoothT);
            if (mainCamera != null) mainCamera.fieldOfView = Mathf.Lerp(defaultFOV - 20f, defaultFOV, smoothT);
            yield return null;
        }

        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 0f;
        if (mainCamera != null) mainCamera.fieldOfView = defaultFOV;
        isTransitioning = false;
    }

    // 返回大地圖
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
            // --- 修改：直接呼叫無參數的方法 ---
            PerspectiveMapGenerator.Instance.WakeUpMapAndUnload();
        }
        else
        {
            Debug.LogWarning("[ExplorationManager] 找不到大地圖總管，無法卸載場景！(可能是在單場景測試中)");
            isTransitioning = false; 
        }
    }
}