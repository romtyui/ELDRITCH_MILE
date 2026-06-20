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
    private bool isTransitioning = false; // 防止連續點擊

    private float defaultFOV = 60f;

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

        if (mainCamera != null) defaultFOV = mainCamera.fieldOfView;
        
        // 確保一開始畫面是亮的
        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 0f; 

        if (startingNode != null) TransitionToNode(startingNode);
    }

    public void TransitionToNode(MapNodeExplore newNode)
    {
        if (newNode == null || newNode.roomPrefab == null || isTransitioning) return;
        // 啟動轉場動畫協程
        StartCoroutine(TransitionRoutine(newNode));
    }

    private System.Collections.IEnumerator TransitionRoutine(MapNodeExplore newNode)
    {
        // 【新增】如果不是空歷史，就把當前節點存入歷史中
        if (currentNode != null) 
        {
            historyNodes.Add(currentNode);
        }

        isTransitioning = true;

        // ==========================================
        // 階段 1：往前衝刺 (縮小 FOV) 與 畫面變黑
        // ==========================================
        float timeElapsed = 0f;
        while (timeElapsed < transitionSpeed)
        {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / transitionSpeed;
            
            // 加上平滑曲線 (Ease In)
            float smoothT = t * t; 

            if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, smoothT);
            if (mainCamera != null) mainCamera.fieldOfView = Mathf.Lerp(defaultFOV, defaultFOV - 20f, smoothT); // FOV 縮小 20 產生衝刺感

            yield return null;
        }

        // ==========================================
        // 階段 2：在全黑的瞬間，替換房間與卡牌
        // ==========================================
        if (activeRoomInstance != null) Destroy(activeRoomInstance);

        currentNode = newNode;
        Vector3 spawnPos = roomSpawnPoint != null ? roomSpawnPoint.position : Vector3.zero;
        activeRoomInstance = Instantiate(newNode.roomPrefab, spawnPos, Quaternion.identity);
        activeRoomInstance.name = $"Room_{newNode.roomName}";

        RoomController controller = activeRoomInstance.GetComponent<RoomController>();
        if (controller != null) controller.InitializeRoom(newNode);
        // 【新增】每次切換房間後，更新小地圖
        if (miniMapManager != null)
        {
            miniMapManager.DrawMap(currentNode, historyNodes);
        }

        // --- 方案 A：重置手牌資源 ---
        if (cardManager != null)
        {
            cardManager.DiscardHand();
            cardManager.DrawCards(cardManager.cardsOnEnterExploration);
            Debug.Log("[ExplorationManager] 進入新房間，重新抽取手牌。");
        }

        // 可以加一點微小的停頓，讓黑畫面保持一下下 (可選)
        yield return new WaitForSeconds(0.1f);

        // ==========================================
        // 階段 3：豁然開朗 (恢復 FOV) 與 畫面變亮
        // ==========================================
        timeElapsed = 0f;
        while (timeElapsed < transitionSpeed)
        {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / transitionSpeed;
            
            // 加上平滑曲線 (Ease Out)
            float smoothT = 1f - (1f - t) * (1f - t);

            if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, smoothT);
            if (mainCamera != null) mainCamera.fieldOfView = Mathf.Lerp(defaultFOV - 20f, defaultFOV, smoothT);

            yield return null;
        }

        // 確保完全恢復
        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 0f;
        if (mainCamera != null) mainCamera.fieldOfView = defaultFOV;

        isTransitioning = false;
        Debug.Log($"[ExplorationManager] 成功進入節點: {newNode.roomName}");
    }

}


