using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // 新增這行來處理場景載入

// ==========================================
// 【純資料層 Data】(不會掛在物件上)
// 用來記錄這局遊戲的狀態，未來要存檔就是存這個
// ==========================================
[System.Serializable]
public class RunNodeData
{
    public string nodeId;                 // 節點唯一ID
    public MapNodeExplore templateData;   // 對應的 SO (只讀不寫)
    public int layer;                     // 第幾層
    public float xPercent;                // 畫布上的 X 座標百分比
    public float yPercent;                // 畫布上的 Y 座標百分比
    public List<string> nextNodeIds = new List<string>(); // 連向哪些節點的 ID
}

[System.Serializable]
public class MapData
{
    public List<RunNodeData> allNodes = new List<RunNodeData>();
    public string currentNodeId = "";     // 玩家目前所在的節點 ID
    public List<string> historyNodeIds = new List<string>(); // 走過的節點 ID
}

// ==========================================
// 【邏輯與畫面層 Generator & View】
// ==========================================
public class PerspectiveMapGenerator : MonoBehaviour
{
    public static PerspectiveMapGenerator Instance { get; private set; }

    [Header("測試 DEMO 路線設定")]
    [Tooltip("自訂 DEMO 路線的節點資料 (依序從下到上排列，例如放入 test1, test2, test3, test4)")]
    public List<MapNodeExplore> demoRouteNodes;

    [Header("UI 生成設定")]
    public RectTransform mapContainer; 
    public GameObject linePrefab;      
    public GameObject combatNodePrefab;
    public GameObject eventNodePrefab;
    public GameObject bossNodePrefab;

    [Header("場景轉場控制")]
    [Tooltip("地圖專用的相機，進入探索時需關閉")]
    public Camera mapCamera;
    [Tooltip("地圖專用的畫布，進入探索時需關閉")]
    public Canvas mapCanvas;
    [Tooltip("要疊加載入的探索場景名稱")]
    public string exploreSceneName = "ExploreScene"; 

    [Header("當前遊戲資料 (唯讀觀察用)")]
    public MapData currentMapData;

    // 紀錄畫面上生成的 UI 物件，方便更新狀態
    private Dictionary<string, PerspectiveNode> spawnedNodeUIs = new Dictionary<string, PerspectiveNode>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // 1. 產生純資料 (Data)
        currentMapData = GenerateDemoData(); 
        
        // 2. 根據資料畫出畫面 (View)
        RenderMapView(currentMapData);       
        
        // 3. 更新明暗狀態
        SyncMapState();                      
    }

    // --------------------------------------------------------
    // 步驟 1：只算資料，不碰 UI
    // --------------------------------------------------------
    private MapData GenerateDemoData()
    {
        MapData newMap = new MapData();

        if (demoRouteNodes == null || demoRouteNodes.Count == 0)
        {
            Debug.LogWarning("DEMO路線未設定！");
            return newMap;
        }

        int layers = demoRouteNodes.Count;
        RunNodeData previousNode = null;

        for (int i = 0; i < layers; i++)
        {
            RunNodeData nodeData = new RunNodeData
            {
                nodeId = "Node_" + i, // 給予唯一 ID
                templateData = demoRouteNodes[i],
                layer = i,
                xPercent = 50f, // 固定在中間
                yPercent = layers <= 1 ? 50f : 10f + 80f / (layers - 1) * i
            };

            newMap.allNodes.Add(nodeData);

            // 建立連線資料 (前一個節點的 nextIds 記錄當前節點的 ID)
            if (previousNode != null)
            {
                previousNode.nextNodeIds.Add(nodeData.nodeId);
            }
            previousNode = nodeData;
        }

        return newMap;
    }

    // --------------------------------------------------------
    // 步驟 2：只畫 UI，不碰邏輯
    // --------------------------------------------------------
    private void RenderMapView(MapData mapData)
    {
        // 清空舊 UI
        foreach (Transform child in mapContainer) Destroy(child.gameObject);
        spawnedNodeUIs.Clear();

        // 畫節點
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
            nodeScript.InitData(nodeData); // 綁定資料
            
            spawnedNodeUIs.Add(nodeData.nodeId, nodeScript);
        }

        // 畫連線
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

    // --------------------------------------------------------
    // 狀態更新與點擊處理
    // --------------------------------------------------------
    public void SyncMapState()
    {
        if (currentMapData == null) return;

        // 找出目前所在節點的資料
        RunNodeData currentData = currentMapData.allNodes.Find(n => n.nodeId == currentMapData.currentNodeId);
        List<string> availableNextIds = currentData != null ? currentData.nextNodeIds : new List<string>();

        foreach (var kvp in spawnedNodeUIs)
        {
            string id = kvp.Key;
            PerspectiveNode uiNode = kvp.Value;

            bool isCurrent = (id == currentMapData.currentNodeId);
            bool isVisited = currentMapData.historyNodeIds.Contains(id);
            
            // 如果還沒出發 (currentNodeId == "")，則第 0 層為可選
            bool isSelectable = false;
            if (string.IsNullOrEmpty(currentMapData.currentNodeId) && uiNode.runtimeData.layer == 0) isSelectable = true;
            else if (availableNextIds.Contains(id)) isSelectable = true;

            uiNode.UpdateVisual(isCurrent, isSelectable, isVisited);
        }
    }

    // 供 PerspectiveNode 點擊時呼叫
    public void OnNodeClicked(RunNodeData clickedNode)
    {
        // 1. 記錄歷史軌跡
        if (!string.IsNullOrEmpty(currentMapData.currentNodeId))
        {
            currentMapData.historyNodeIds.Add(currentMapData.currentNodeId);
        }
        
        // 2. 更新當前位置
        currentMapData.currentNodeId = clickedNode.nodeId;
        SyncMapState();

        // 3. 【方案A的轉場起點】
        Debug.Log($"玩家進入了節點: {clickedNode.templateData.name}，準備載入場景...");

        // 關閉地圖專用的相機與畫布，把螢幕「讓」給接下來要載入的探索場景
        if (mapCamera != null) mapCamera.gameObject.SetActive(false);
        if (mapCanvas != null) mapCanvas.gameObject.SetActive(false);

        // 疊加載入探索場景
        SceneManager.LoadSceneAsync(exploreSceneName, LoadSceneMode.Additive);
    }

    // 供探索場景結束時呼叫 (例如打完怪了，回到地圖)
    public void ReturnToMap()
    {
        // 重新開啟地圖相機與畫布
        if (mapCamera != null) mapCamera.gameObject.SetActive(true);
        if (mapCanvas != null) mapCanvas.gameObject.SetActive(true);
        
        // 卸載探索場景
        SceneManager.UnloadSceneAsync(exploreSceneName);
    }
}