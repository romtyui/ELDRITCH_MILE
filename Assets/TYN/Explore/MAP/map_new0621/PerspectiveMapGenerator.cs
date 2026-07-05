using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// ==========================================
// 【純資料層 Data】
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

    [Header("測試與 DEMO 設定")]
    [Tooltip("勾選後，將固定生成 3個戰鬥 + 1個Boss 的直線路線。取消勾選則為隨機路線。")]
    public bool useDemoRoute = false;
    public List<MapNodeExplore> demoRouteNodes;

    [Header("UI 生成設定")]
    public int mapLayers = 5;
    public int startNodeCount = 2;
    public RectTransform mapCanvasRect;
    public RectTransform mapContainer;
    public GameObject linePrefab;
    public GameObject combatNodePrefab;
    public GameObject eventNodePrefab;
    public GameObject bossNodePrefab;

    [Header("隨機生成資料庫")]
    public List<MapNodeExplore> possibleCombatNodes;
    public List<MapNodeExplore> possibleEventNodes;
    public MapNodeExplore bossNode;

    [Header("場景轉場控制")]
    public Camera mapCamera;
    public Canvas mapCanvas;
    public CanvasGroup transitionFade;
    public float fadeDuration = 0.4f;

    [Header("動態展演 (玩家移動)")]
    public RectTransform playerAvatar;
    public Vector2 mapStartOffset = new Vector2(0, 1500f);
    public float introMapDropDuration = 0.8f;
    public float nodePopDuration = 0.3f;
    
    [Tooltip("玩家移動到下一個節點花費的時間")]
    public float avatarMoveDuration = 0.8f; 
    [Tooltip("模擬走路/跳躍的高度起伏幅度")]
    public float bobbingHeight = 25f; 
    [Tooltip("移動過程中上下起伏的次數 (越大越像小碎步)")]
    public float bobbingFrequency = 2f;

    [Header("當前遊戲資料 (唯讀觀察用)")]
    public MapData currentMapData;

    [Header("地圖 Banner UI 設定")]
    public MapBannerUI mapBannerUI;
    public string mapEnterText = "<color=#FFFFFF>地圖</color>";
    public string gameEndText = "<color=#FFD700>體驗結束</color>";
    public string menuSceneName = "MenuScene"; // 你的主選單場景名稱

    private string lastLoadedSceneName = "";
    private Vector2 baseMapPos;

    private Dictionary<string, PerspectiveNode> spawnedNodeUIs = new Dictionary<string, PerspectiveNode>();
    private Dictionary<int, List<RectTransform>> linesByLayer = new Dictionary<int, List<RectTransform>>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (transitionFade != null)
        {
            transitionFade.alpha = 1f; // 初始全黑
            transitionFade.blocksRaycasts = false;
        }

        if (mapCanvasRect != null) baseMapPos = mapCanvasRect.anchoredPosition;

        if (useDemoRoute) GenerateDemoRoute();
        else GenerateProceduralMap();

        StartCoroutine(IntroRoutine());
    }

    // ==========================================
    // 地圖生成邏輯 (隨機 與 DEMO)
    // ==========================================
    private void GenerateProceduralMap()
    {
        currentMapData = new MapData();
        ClearMap();
        List<List<PerspectiveNode>> layers = new List<List<PerspectiveNode>>();

        for (int i = 0; i < mapLayers; i++)
        {
            int count = (i == 0) ? startNodeCount : ((i == mapLayers - 1) ? 1 : Random.Range(2, 4));
            float yPercent = 10f + 80f / (mapLayers - 1) * i;
            var currentLayer = new List<PerspectiveNode>();

            for (int j = 0; j < count; j++)
            {
                float baseX = count == 1 ? 50f : 20f + 60f / (count - 1) * j;
                float jitter = count == 1 ? 0f : Random.Range(-5f, 5f);
                float xPercent = Mathf.Clamp(baseX + jitter, 10f, 90f);

                bool isBoss = (i == mapLayers - 1);
                bool isCombat = Random.value > 0.4f;
                MapNodeExplore data = isBoss ? bossNode : (isCombat ? GetRandom(possibleCombatNodes) : GetRandom(possibleEventNodes));
                GameObject prefab = isBoss ? bossNodePrefab : (isCombat ? combatNodePrefab : eventNodePrefab);

                RunNodeData nodeData = new RunNodeData
                {
                    nodeId = $"Node_{i}_{j}",
                    templateData = data,
                    layer = i,
                    xPercent = xPercent,
                    yPercent = yPercent
                };
                currentMapData.allNodes.Add(nodeData);

                PerspectiveNode nodeScript = SpawnNode(prefab, nodeData);
                currentLayer.Add(nodeScript);
            }
            layers.Add(currentLayer);
        }
        BuildConnections(layers);
    }

    private void GenerateDemoRoute()
    {
        currentMapData = new MapData();
        ClearMap();
        
        if (demoRouteNodes == null || demoRouteNodes.Count == 0) return;

        int layers = demoRouteNodes.Count;
        PerspectiveNode previousNode = null;

        for (int i = 0; i < layers; i++)
        {
            float yPercent = layers <= 1 ? 50f : 10f + 80f / (layers - 1) * i;
            bool isBoss = (i == layers - 1);
            
            MapNodeExplore data = demoRouteNodes[i];
            GameObject prefab = isBoss ? bossNodePrefab : combatNodePrefab;

            RunNodeData nodeData = new RunNodeData
            {
                nodeId = "Node_" + i,
                templateData = data,
                layer = i,
                xPercent = 50f + Random.Range(-3f, 3f), // 稍微加上一點抖動
                yPercent = yPercent
            };
            currentMapData.allNodes.Add(nodeData);

            PerspectiveNode currentNode = SpawnNode(prefab, nodeData);

            if (previousNode != null) ConnectNodes(previousNode, currentNode);
            previousNode = currentNode;
        }
    }

    private MapNodeExplore GetRandom(List<MapNodeExplore> list)
    {
        if (list == null || list.Count == 0) return null;
        return list[Random.Range(0, list.Count)];
    }

    private void ClearMap()
    {
        foreach (Transform child in mapContainer)
        {
            if (playerAvatar != null && child == playerAvatar) continue;
            Destroy(child.gameObject);
        }
        spawnedNodeUIs.Clear();
        linesByLayer.Clear();
    }

    private PerspectiveNode SpawnNode(GameObject prefab, RunNodeData data)
    {
        GameObject nodeObj = Instantiate(prefab, mapContainer);
        RectTransform rect = nodeObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(data.xPercent / 100f, data.yPercent / 100f);
        rect.anchorMax = new Vector2(data.xPercent / 100f, data.yPercent / 100f);
        rect.anchoredPosition = Vector2.zero;

        PerspectiveNode nodeScript = nodeObj.GetComponent<PerspectiveNode>();
        nodeScript.InitData(data);
        spawnedNodeUIs.Add(data.nodeId, nodeScript);
        return nodeScript;
    }

    private void BuildConnections(List<List<PerspectiveNode>> layers)
    {
        for (int i = 0; i < layers.Count - 1; i++)
        {
            var current = layers[i];
            var next = layers[i + 1];

            foreach (var node in current)
            {
                // 找出X軸距離最近的節點連線
                next.Sort((a, b) => Mathf.Abs(a.transform.localPosition.x - node.transform.localPosition.x).CompareTo(Mathf.Abs(b.transform.localPosition.x - node.transform.localPosition.x)));
                int connections = (Random.value > 0.5f && next.Count > 1) ? 2 : 1;
                for (int c = 0; c < connections; c++) ConnectNodes(node, next[c]);
            }

            foreach (var orphan in next)
            {
                if (orphan.runtimeData.nextNodeIds.Count == 0 && !current.Exists(n => n.runtimeData.nextNodeIds.Contains(orphan.runtimeData.nodeId)))
                {
                    current.Sort((a, b) => Mathf.Abs(a.transform.localPosition.x - orphan.transform.localPosition.x).CompareTo(Mathf.Abs(b.transform.localPosition.x - orphan.transform.localPosition.x)));
                    ConnectNodes(current[0], orphan);
                }
            }
        }
    }

    private void ConnectNodes(PerspectiveNode parent, PerspectiveNode child)
    {
        if (!parent.runtimeData.nextNodeIds.Contains(child.runtimeData.nodeId))
        {
            parent.runtimeData.nextNodeIds.Add(child.runtimeData.nodeId);
            DrawLine(parent.GetComponent<RectTransform>(), child.GetComponent<RectTransform>(), parent.runtimeData.layer);
        }
    }

    private void DrawLine(RectTransform start, RectTransform end, int layer)
    {
        GameObject arrowObj = Instantiate(linePrefab, mapContainer);
        arrowObj.transform.SetAsFirstSibling(); 
        RectTransform rect = arrowObj.GetComponent<RectTransform>();

        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);

        Vector2 startPos = start.localPosition;
        Vector2 endPos = end.localPosition;
        Vector2 dir = endPos - startPos;

        rect.localPosition = (startPos + endPos) / 2f;
        rect.sizeDelta = new Vector2(7f, 25f);
        rect.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f);
        rect.localScale = Vector3.zero;

        if (!linesByLayer.ContainsKey(layer)) linesByLayer[layer] = new List<RectTransform>();
        linesByLayer[layer].Add(rect);
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

    // ========================================================
    // 動態展演與轉場邏輯
    // ========================================================
    private IEnumerator IntroRoutine()
    {
        if (transitionFade != null) transitionFade.blocksRaycasts = true;

        foreach (var node in spawnedNodeUIs.Values) node.transform.localScale = Vector3.zero;
        if (playerAvatar != null)
        {
            playerAvatar.gameObject.SetActive(false);
            playerAvatar.localPosition = GetNodeLocalPosition(""); 
        }

        // 開局從黑幕淡出
        StartCoroutine(FadeRoutine(1f, 0f));

        float t = 0;
        Vector2 startMapPos = baseMapPos + mapStartOffset;
        while (t < introMapDropDuration)
        {
            t += Time.deltaTime;
            float progress = t / introMapDropDuration;
            float smooth = 1f - (1f - progress) * (1f - progress); 
            mapCanvasRect.anchoredPosition = Vector2.Lerp(startMapPos, baseMapPos, smooth);
            yield return null;
        }
        mapCanvasRect.anchoredPosition = baseMapPos;

        // --- 新增：顯示地圖進入 Banner ---
        if (mapBannerUI != null)
        {
            StartCoroutine(mapBannerUI.ShowMapTitle(mapEnterText));
        }

        // 2. 節點與連線逐層彈出
        int maxLayer = currentMapData.allNodes.Count > 0 ? currentMapData.allNodes[currentMapData.allNodes.Count - 1].layer : 0;
        for (int l = 0; l <= maxLayer; l++)
        {
            List<PerspectiveNode> nodesInLayer = new List<PerspectiveNode>();
            foreach (var node in spawnedNodeUIs.Values)
                if (node.runtimeData.layer == l) nodesInLayer.Add(node);
            
            List<RectTransform> linesInLayer = linesByLayer.ContainsKey(l) ? linesByLayer[l] : new List<RectTransform>();

            float popT = 0;
            while (popT < nodePopDuration)
            {
                popT += Time.deltaTime;
                float progress = popT / nodePopDuration;
                float scale = progress < 0.7f ? Mathf.Lerp(0, 1.3f, progress / 0.7f) : Mathf.Lerp(1.3f, 1f, (progress - 0.7f) / 0.3f);
                foreach (var n in nodesInLayer) n.transform.localScale = Vector3.one * scale;
                foreach (var line in linesInLayer) line.localScale = Vector3.one * progress;
                yield return null;
            }
            yield return new WaitForSeconds(0.05f); 
        }

        SyncMapState();
        if (playerAvatar != null)
        {
            playerAvatar.gameObject.SetActive(true);
            playerAvatar.SetAsLastSibling(); 
        }

    
        // ----------------------------------

        visualCurrentNodeId = currentMapData.currentNodeId; // 同步視覺狀態
        if (transitionFade != null) transitionFade.blocksRaycasts = false;
    }

    public void OnNodeClicked(RunNodeData clickedNode)
    {
        if (transitionFade.blocksRaycasts) return; // 防止轉場中連點

        // 1. 記錄軌跡
        string previousNodeId = currentMapData.currentNodeId;
        if (!string.IsNullOrEmpty(currentMapData.currentNodeId))
        {
            currentMapData.historyNodeIds.Add(currentMapData.currentNodeId);
        }
        currentMapData.currentNodeId = clickedNode.nodeId;
        SyncMapState(); 

        string targetScene = clickedNode.templateData?.targetSceneName ?? "ExploreScene";
        Debug.Log($"[地圖] 前往節點: {clickedNode.templateData?.name}");

        // 2. 開始移動與載入流程 (先走過去，到了才黑畫面)
        StartCoroutine(MoveAndLoadRoutine(previousNodeId, clickedNode.nodeId, targetScene));
    }

    private IEnumerator MoveAndLoadRoutine(string fromId, string toId, string targetScene)
    {
        if (transitionFade != null) transitionFade.blocksRaycasts = true;

        // 階段 1：棋子移動 (包含走路/起伏特效)
        yield return StartCoroutine(MoveAvatarAndScrollMap(fromId, toId));

        // 階段 2：走到定位了，畫面變黑
        lastLoadedSceneName = targetScene;
        yield return StartCoroutine(FadeRoutine(0f, 1f));

        // 階段 3：背景疊加載入探索場景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive);
        asyncLoad.allowSceneActivation = false;
        while (asyncLoad.progress < 0.9f) yield return null;
        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone) yield return null;

        // 階段 4：關閉地圖相機，把畫面控制權交給 ExploreScene (讓它負責黑幕淡出)
        if (mapCamera != null) mapCamera.gameObject.SetActive(false);
        if (mapCanvas != null) mapCanvas.gameObject.SetActive(false);
        
        yield return StartCoroutine(FadeRoutine(1f, 0f));
    }

    public void WakeUpMapAndUnload()
    {
        if (string.IsNullOrEmpty(lastLoadedSceneName)) return;
        StartCoroutine(WakeUpRoutine());
    }

    private IEnumerator WakeUpRoutine()
    {
        // 1. 地圖接手時，因為剛從 ExplorationManager 回來，畫面已經是黑的
        if (transitionFade != null)
        {
            transitionFade.alpha = 1f;
            transitionFade.blocksRaycasts = true;
        }

        // 2. 重開地圖物件
        if (mapCamera != null) mapCamera.gameObject.SetActive(true);
        if (mapCanvas != null) mapCanvas.gameObject.SetActive(true);
        
        // 3. 卸載場景
        AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(lastLoadedSceneName);
        while (!asyncUnload.isDone) yield return null;
        lastLoadedSceneName = "";

        // 4. 地圖淡入亮起
        yield return StartCoroutine(FadeRoutine(1f, 0f));

        // 動態展演 2：醒來後，如果位置有變，執行棋子跳躍與地圖捲動 (這段原本的邏輯保留)
        /* 
        if (visualCurrentNodeId != currentMapData.currentNodeId)
        {
            yield return StartCoroutine(MoveAvatarAndScrollMap(visualCurrentNodeId, currentMapData.currentNodeId));
            visualCurrentNodeId = currentMapData.currentNodeId;
        }
        */
    }

    private IEnumerator MoveAvatarAndScrollMap(string fromId, string toId)
    {
        Vector2 startLocalPos = GetNodeLocalPosition(fromId);
        Vector2 endLocalPos = GetNodeLocalPosition(toId);
        Vector2 mapStartPos = mapCanvasRect.anchoredPosition;
        
        // 地圖反向跟隨 (X軸跟一半，Y軸完全跟)
        float deltaY = endLocalPos.y - startLocalPos.y;
        float deltaX = endLocalPos.x - startLocalPos.x;
        Vector2 mapEndPos = mapStartPos - new Vector2(deltaX * 0.5f, deltaY); 

        float t = 0f;
        while (t < avatarMoveDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.SmoothStep(0, 1, t / avatarMoveDuration);

            if (playerAvatar != null)
            {
                Vector2 currentPos = Vector2.Lerp(startLocalPos, endLocalPos, progress);
                
                // 【優化】加入 Head Bobbing / 走路起伏效果 (用 Abs Sin 模擬腳步點地彈起)
                // frequency 控制跨幾步，height 控制彈多高
                float walkBounce = Mathf.Abs(Mathf.Sin(progress * Mathf.PI * bobbingFrequency)) * bobbingHeight;
                
                playerAvatar.localPosition = currentPos + new Vector2(0, walkBounce);
            }

            mapCanvasRect.anchoredPosition = Vector2.Lerp(mapStartPos, mapEndPos, progress);
            yield return null;
        }

        if (playerAvatar != null) playerAvatar.localPosition = endLocalPos;
        mapCanvasRect.anchoredPosition = mapEndPos;
        baseMapPos = mapEndPos; 
    }

    private Vector2 GetNodeLocalPosition(string id)
    {
        if (string.IsNullOrEmpty(id) || !spawnedNodeUIs.ContainsKey(id))
        {
            if (currentMapData.allNodes.Count > 0)
                return new Vector2(0, (currentMapData.allNodes[0].yPercent / 100f - 0.5f) * mapContainer.rect.height - 150f);
            return new Vector2(0, -300f);
        }
        Vector2 nodePos = spawnedNodeUIs[id].GetComponent<RectTransform>().localPosition;
        return nodePos + new Vector2(40f, -20f); 
    }


    private IEnumerator TransitionToSceneRoutine(string targetScene)
    {
        lastLoadedSceneName = targetScene;

        yield return StartCoroutine(FadeRoutine(0f, 1f));

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f) yield return null;

        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone) yield return null;

        if (mapCamera != null) mapCamera.gameObject.SetActive(false);
        if (mapCanvas != null) mapCanvas.gameObject.SetActive(false);

        yield return StartCoroutine(FadeRoutine(1f, 0f));
    }

    public void TransferToBattleScene(string battleSceneName)
    {
        StartCoroutine(TransferRoutine(lastLoadedSceneName, battleSceneName));
    }

    private IEnumerator TransferRoutine(string sceneToUnload, string sceneToLoad)
    {
        lastLoadedSceneName = sceneToLoad;

        yield return StartCoroutine(FadeRoutine(0f, 1f));

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);
        asyncLoad.allowSceneActivation = false;
        while (asyncLoad.progress < 0.9f) yield return null;

        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone) yield return null;

        if (!string.IsNullOrEmpty(sceneToUnload))
        {
            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(sceneToUnload);
            while (!asyncUnload.isDone) yield return null;
        }

        yield return StartCoroutine(FadeRoutine(1f, 0f));
    }

    public void WakeUpMapAndUnload()
    {
        if (string.IsNullOrEmpty(lastLoadedSceneName)) return;
        StartCoroutine(WakeUpRoutine());
    }

    private IEnumerator WakeUpRoutine()
    {
        yield return StartCoroutine(FadeRoutine(0f, 1f));

        if (mapCamera != null) mapCamera.gameObject.SetActive(true);
        if (mapCanvas != null) mapCanvas.gameObject.SetActive(true);
        
        AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(lastLoadedSceneName);
        while (!asyncUnload.isDone) yield return null;
        
        lastLoadedSceneName = "";

        yield return StartCoroutine(FadeRoutine(1f, 0f));

        // --- 新增：判斷是否剛打完最後的 Boss 節點 ---
        RunNodeData currentNode = currentMapData.allNodes.Find(n => n.nodeId == currentMapData.currentNodeId);
        bool isBossNode = false;
        
        if (currentNode != null)
        {
            // 如果當前節點的 layer 等於總層數-1，代表是最後一層 (Boss)
            isBossNode = (currentNode.layer == currentMapData.allNodes.Count - 1);
        }

        if (isBossNode)
        {
            // 打完最後一關，顯示體驗結束與按鈕
            if (mapBannerUI != null) 
                StartCoroutine(mapBannerUI.ShowEndGame(gameEndText, menuSceneName));
        }
        else
        {
            // 打完一般關卡，顯示普通地圖 Banner
            if (mapBannerUI != null) 
                StartCoroutine(mapBannerUI.ShowMapTitle(mapEnterText));
        }

        // ========================================================
        // 動態展演 2：醒來後，如果位置有變，執行棋子跳躍與地圖捲動
        // ========================================================
        if (visualCurrentNodeId != currentMapData.currentNodeId)
        {
            yield return StartCoroutine(MoveAvatarAndScrollMap(visualCurrentNodeId, currentMapData.currentNodeId));
            visualCurrentNodeId = currentMapData.currentNodeId;
        }

        
        // ----------------------------------------------
    }

    // ========================================================
    // 動態展演 3：玩家棋子移動 & 鏡頭(地圖)完美跟隨
    // ========================================================
    

    // 輔助函式：取得節點相對於 MapContainer 的座標

    private IEnumerator FadeRoutine(float startAlpha, float endAlpha)
    {
        if (transitionFade == null) yield break;

        transitionFade.blocksRaycasts = true; 
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            transitionFade.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);
            yield return null;
        }

        transitionFade.alpha = endAlpha;
        transitionFade.blocksRaycasts = (endAlpha > 0.5f); 
    }
}