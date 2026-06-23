using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; 

// ==========================================
// 【純資料層 Data】(不會掛在物件上)
// ==========================================
[System.Serializable]
public class RunNodeData
{
    public string nodeId;                 
    public MapNodeExplore templateData;   
    public int layer;                     
    public float xPercent;                
    public float yPercent;                
    public List<string> nextNodeIds = new List<string>(); 
}

[System.Serializable]
public class MapData
{
    public List<RunNodeData> allNodes = new List<RunNodeData>();
    public string currentNodeId = "";     
    public List<string> historyNodeIds = new List<string>(); 
}

// ==========================================
// 【邏輯與畫面層 Generator & View】
// ==========================================
public class PerspectiveMapGenerator : MonoBehaviour
{
    public static PerspectiveMapGenerator Instance { get; private set; }

    [Header("測試 DEMO 路線設定")]
    public List<MapNodeExplore> demoRouteNodes;

    [Header("UI 生成設定")]
    public RectTransform mapContainer; 
    public GameObject linePrefab;      
    public GameObject combatNodePrefab;
    public GameObject eventNodePrefab;
    public GameObject bossNodePrefab;

    [Header("場景轉場控制")]
    public Camera mapCamera;
    public Canvas mapCanvas;

    [Header("當前遊戲資料 (唯讀觀察用)")]
    public MapData currentMapData;

    // --- 新增：記錄最後疊加載入的場景名稱 ---
    private string lastLoadedSceneName = "";

    private Dictionary<string, PerspectiveNode> spawnedNodeUIs = new Dictionary<string, PerspectiveNode>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        currentMapData = GenerateDemoData(); 
        RenderMapView(currentMapData);       
        SyncMapState();                      
    }

    private MapData GenerateDemoData()
    {
        MapData newMap = new MapData();

        if (demoRouteNodes == null || demoRouteNodes.Count == 0) return newMap;

        int layers = demoRouteNodes.Count;
        RunNodeData previousNode = null;

        for (int i = 0; i < layers; i++)
        {
            RunNodeData nodeData = new RunNodeData
            {
                nodeId = "Node_" + i, 
                templateData = demoRouteNodes[i],
                layer = i,
                xPercent = 50f, 
                yPercent = layers <= 1 ? 50f : 10f + 80f / (layers - 1) * i
            };

            newMap.allNodes.Add(nodeData);

            if (previousNode != null) previousNode.nextNodeIds.Add(nodeData.nodeId);
            previousNode = nodeData;
        }

        return newMap;
    }

    private void RenderMapView(MapData mapData)
    {
        foreach (Transform child in mapContainer) Destroy(child.gameObject);
        spawnedNodeUIs.Clear();

        foreach (var nodeData in mapData.allNodes)
        {
            bool isBoss = (nodeData.layer == mapData.allNodes.Count - 1);
            GameObject prefab = isBoss ? bossNodePrefab : combatNodePrefab;

            GameObject nodeObj = Instantiate(prefab, mapContainer);
            RectTransform rect = nodeObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(nodeData.xPercent / 100f, nodeData.yPercent / 100f);
            rect.anchorMax = new Vector2(nodeData.xPercent / 100f, nodeData.yPercent / 100f);
            rect.anchoredPosition = Vector2.zero;

            PerspectiveNode nodeScript = nodeObj.GetComponent<PerspectiveNode>();
            nodeScript.InitData(nodeData); 
            
            spawnedNodeUIs.Add(nodeData.nodeId, nodeScript);
        }

        foreach (var nodeData in mapData.allNodes)
        {
            foreach (var targetId in nodeData.nextNodeIds)
            {
                if (spawnedNodeUIs.TryGetValue(nodeData.nodeId, out var startNode) && 
                    spawnedNodeUIs.TryGetValue(targetId, out var endNode))
                {
                    DrawLine(startNode.GetComponent<RectTransform>(), endNode.GetComponent<RectTransform>());
                }
            }
        }
    }

    private void DrawLine(RectTransform start, RectTransform end)
    {
        GameObject lineObj = Instantiate(linePrefab, mapContainer);
        lineObj.transform.SetAsFirstSibling();
        RectTransform rect = lineObj.GetComponent<RectTransform>();

        Vector2 dir = end.anchoredPosition - start.anchoredPosition;
        rect.anchoredPosition = start.anchoredPosition + dir / 2f;
        rect.sizeDelta = new Vector2(dir.magnitude, 5f);
        rect.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    public void SyncMapState()
    {
        if (currentMapData == null) return;

        RunNodeData currentData = currentMapData.allNodes.Find(n => n.nodeId == currentMapData.currentNodeId);
        List<string> availableNextIds = currentData != null ? currentData.nextNodeIds : new List<string>();

        foreach (var kvp in spawnedNodeUIs)
        {
            string id = kvp.Key;
            PerspectiveNode uiNode = kvp.Value;

            bool isCurrent = (id == currentMapData.currentNodeId);
            bool isVisited = currentMapData.historyNodeIds.Contains(id);
            
            bool isSelectable = false;
            if (string.IsNullOrEmpty(currentMapData.currentNodeId) && uiNode.runtimeData.layer == 0) isSelectable = true;
            else if (availableNextIds.Contains(id)) isSelectable = true;

            uiNode.UpdateVisual(isCurrent, isSelectable, isVisited);
        }
    }

    public void OnNodeClicked(RunNodeData clickedNode)
    {
        if (!string.IsNullOrEmpty(currentMapData.currentNodeId))
        {
            currentMapData.historyNodeIds.Add(currentMapData.currentNodeId);
        }
        
        currentMapData.currentNodeId = clickedNode.nodeId;
        SyncMapState();

        if (mapCamera != null) mapCamera.gameObject.SetActive(false);
        if (mapCanvas != null) mapCanvas.gameObject.SetActive(false);

        string targetScene = "ExploreScene";
        if (clickedNode.templateData != null && !string.IsNullOrEmpty(clickedNode.templateData.targetSceneName))
        {
            targetScene = clickedNode.templateData.targetSceneName;
        }

        string nodeName = clickedNode.templateData != null ? clickedNode.templateData.name : "Unknown";
        Debug.Log($"[地圖] 玩家進入了節點: {nodeName}，準備疊加載入場景: {targetScene}");

        // --- 記錄載入的場景名稱 ---
        lastLoadedSceneName = targetScene;

        SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive);
    }

    // ========================================================
    // --- 新增：由探索場景呼叫，將目前的探索場景替換成戰鬥場景 ---
    // ========================================================
    public void TransferToBattleScene(string battleSceneName)
    {
        Debug.Log($"[地圖] 準備從 {lastLoadedSceneName} 切換至戰鬥場景: {battleSceneName}");

        // 1. 卸載當前的探索場景
        if (!string.IsNullOrEmpty(lastLoadedSceneName))
        {
            SceneManager.UnloadSceneAsync(lastLoadedSceneName);
        }

        // 2. 疊加載入同學的戰鬥場景
        SceneManager.LoadSceneAsync(battleSceneName, LoadSceneMode.Additive);
        
        // 3. 更新目前疊加在上面的場景名稱！這樣戰鬥結束時才能正確卸載它
        lastLoadedSceneName = battleSceneName;
    }

    // --- 修改：不再需要傳入場景名稱，由地圖總管自行決定卸載誰 ---
    public void WakeUpMapAndUnload()
    {
        if (string.IsNullOrEmpty(lastLoadedSceneName))
        {
            Debug.LogWarning("[地圖] 沒有記錄到任何載入的場景，無法卸載！");
            return;
        }

        Debug.Log($"[地圖] 準備卸載場景 {lastLoadedSceneName} 並重新喚醒地圖相機...");
        
        if (mapCamera != null) mapCamera.gameObject.SetActive(true);
        if (mapCanvas != null) mapCanvas.gameObject.SetActive(true);
        
        // 卸載記錄在案的場景
        SceneManager.UnloadSceneAsync(lastLoadedSceneName);
        
        // 清空記錄
        lastLoadedSceneName = "";
    }
}