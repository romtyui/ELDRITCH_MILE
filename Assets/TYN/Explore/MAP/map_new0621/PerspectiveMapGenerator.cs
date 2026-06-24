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

    [Header("測試 DEMO 路線設定")]
    public List<MapNodeExplore> demoRouteNodes;

    [Header("UI 生成設定")]
    public RectTransform mapCanvasRect;
    public RectTransform mapContainer;
    public GameObject linePrefab;
    public GameObject combatNodePrefab;
    public GameObject eventNodePrefab;
    public GameObject bossNodePrefab;

    [Header("場景轉場控制")]
    public Camera mapCamera;
    public Canvas mapCanvas;

    [Header("專業轉場效果 (全局黑幕)")]
    public CanvasGroup transitionFade;
    public float fadeDuration = 0.4f;

    [Header("動態展演 (玩家與動畫)")]
    [Tooltip("玩家在世界地圖上的代表圖示 (建議為 Image，請放入 MapContainer 底下)")]
    public RectTransform playerAvatar;
    [Tooltip("初次開啟時地圖往下掉的偏移量")]
    public Vector2 mapStartOffset = new Vector2(0, 1500f);
    public float introMapDropDuration = 0.8f;
    public float nodePopDuration = 0.3f;
    public float avatarMoveDuration = 0.6f;

    [Header("當前遊戲資料 (唯讀觀察用)")]
    public MapData currentMapData;

    private string lastLoadedSceneName = "";
    private string visualCurrentNodeId = ""; // 記錄玩家棋子目前「視覺上」停在哪個節點
    private Vector2 baseMapPos; // 記錄 MapContainer 初始的中心座標

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
            transitionFade.alpha = 0f;
            transitionFade.blocksRaycasts = false;
        }

        if (mapCanvasRect != null) baseMapPos = mapCanvasRect.anchoredPosition;

        currentMapData = GenerateDemoData();
        RenderMapView(currentMapData);

        // 啟動開場動態展演
        StartCoroutine(IntroRoutine());
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
                xPercent = 50f + Random.Range(-5f, 5f), // 稍微加上一點 X 軸抖動讓路線不那麼死板
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
        foreach (Transform child in mapContainer)
        {
            // 保留玩家棋子，不要刪掉它
            if (playerAvatar != null && child == playerAvatar) continue;
            Destroy(child.gameObject);
        }
        spawnedNodeUIs.Clear();
        linesByLayer.Clear();

        // 1. 生成所有節點
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

        // 2. 生成所有連線
        foreach (var nodeData in mapData.allNodes)
        {
            foreach (var targetId in nodeData.nextNodeIds)
            {
                if (spawnedNodeUIs.TryGetValue(nodeData.nodeId, out var startNode) && 
                    spawnedNodeUIs.TryGetValue(targetId, out var endNode))
                {
                    DrawLine(startNode.GetComponent<RectTransform>(), endNode.GetComponent<RectTransform>(), nodeData.layer);
                }
            }
        }
    }

    private void DrawLine(RectTransform start, RectTransform end, int layer)
    {
        // 注意：請在 Inspector 的 linePrefab 欄位換成你的 ArrowUI Prefab
        GameObject arrowObj = Instantiate(linePrefab, mapContainer);
        arrowObj.transform.SetAsFirstSibling(); // 壓在最底層
        RectTransform rect = arrowObj.GetComponent<RectTransform>();

        // 【調整1】將 Pivot 設為中心，以便精準放置在兩點正中間
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);

        Vector2 startPos = start.localPosition;
        Vector2 endPos = end.localPosition;
        Vector2 dir = endPos - startPos;

        // 【調整2】位置放在兩個節點的正中間
        rect.localPosition = (startPos + endPos) / 2f;
    
        // 【調整3】強制維持截圖中的長寬比例 (寬 7, 高 25)
        rect.sizeDelta = new Vector2(7f, 25f);

        // 【調整4】計算旋轉角度。
        // 數學上 Mathf.Atan2 算出的 0 度是朝右 (X軸正向)，但你的箭頭原本是朝上 (Y軸正向)。
        // 所以我們需要減去 90 度來校正，箭頭才會正確指向目標。
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f;
        rect.localRotation = Quaternion.Euler(0, 0, angle);

        // 初始隱藏 (統一設為 zero，因為不再是拉伸 X 軸)
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
    // 動態展演 1：開場動畫 (地圖落下 + 節點與連線逐層彈出)
    // ========================================================
    private IEnumerator IntroRoutine()
    {
        if (transitionFade != null) transitionFade.blocksRaycasts = true;

        // 隱藏所有節點與玩家
        foreach (var node in spawnedNodeUIs.Values) node.transform.localScale = Vector3.zero;
        if (playerAvatar != null)
        {
            playerAvatar.gameObject.SetActive(false);
            playerAvatar.localPosition = GetNodeLocalPosition(""); // 放到預設起點位置
        }

        // 1. 底圖從上方掉落 (Ease Out)
        float t = 0;
        Vector2 startMapPos = baseMapPos + mapStartOffset;
        while (t < introMapDropDuration)
        {
            t += Time.deltaTime;
            float progress = t / introMapDropDuration;
            float smooth = 1f - (1f - progress) * (1f - progress); // Ease Out Quad
            mapCanvasRect.anchoredPosition = Vector2.Lerp(startMapPos, baseMapPos, smooth);
            yield return null;
        }
        mapCanvasRect.anchoredPosition = baseMapPos;

        // 2. 節點與連線逐層彈出
        int maxLayer = currentMapData.allNodes.Count > 0 ? currentMapData.allNodes[currentMapData.allNodes.Count - 1].layer : 0;
        for (int l = 0; l <= maxLayer; l++)
        {
            List<PerspectiveNode> nodesInLayer = new List<PerspectiveNode>();
            foreach (var node in spawnedNodeUIs.Values)
            {
                if (node.runtimeData.layer == l) nodesInLayer.Add(node);
            }
            List<RectTransform> linesInLayer = linesByLayer.ContainsKey(l) ? linesByLayer[l] : new List<RectTransform>();

            float popT = 0;
            while (popT < nodePopDuration)
            {
                popT += Time.deltaTime;
                float progress = popT / nodePopDuration;
                
                // 節點彈出放大 (稍微 Overshoot 模擬下棋)
                float scale = progress < 0.7f ? Mathf.Lerp(0, 1.3f, progress / 0.7f) : Mathf.Lerp(1.3f, 1f, (progress - 0.7f) / 0.3f);
                foreach (var n in nodesInLayer) n.transform.localScale = Vector3.one * scale;

                // 改為：箭頭等比例放大彈出
                foreach (var line in linesInLayer) line.localScale = Vector3.one * progress;
                
                yield return null;
            }

            foreach (var n in nodesInLayer) n.transform.localScale = Vector3.one;
            foreach (var line in linesInLayer) line.localScale = Vector3.one;

            yield return new WaitForSeconds(0.1f); // 層與層之間的微小停頓
        }

        // 3. 恢復節點的顏色與正確大小，並顯示玩家棋子
        SyncMapState();
        if (playerAvatar != null)
        {
            playerAvatar.gameObject.SetActive(true);
            playerAvatar.SetAsLastSibling(); // 確保玩家棋子畫在最上層
        }

        visualCurrentNodeId = currentMapData.currentNodeId; // 同步視覺狀態
        if (transitionFade != null) transitionFade.blocksRaycasts = false;
    }

    public void OnNodeClicked(RunNodeData clickedNode)
    {
        if (!string.IsNullOrEmpty(currentMapData.currentNodeId))
        {
            currentMapData.historyNodeIds.Add(currentMapData.currentNodeId);
        }
        
        currentMapData.currentNodeId = clickedNode.nodeId;
        SyncMapState(); // 立即更新地圖節點明暗

        string targetScene = "ExploreScene";
        if (clickedNode.templateData != null && !string.IsNullOrEmpty(clickedNode.templateData.targetSceneName))
        {
            targetScene = clickedNode.templateData.targetSceneName;
        }

        Debug.Log($"[地圖] 玩家進入了節點: {clickedNode.templateData?.name}，準備疊加載入場景: {targetScene}");

        // 注意：此處我們先不移動棋子，保留到戰鬥結束回來時再動
        StartCoroutine(TransitionToSceneRoutine(targetScene));
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

        // ========================================================
        // 動態展演 2：醒來後，如果位置有變，執行棋子跳躍與地圖捲動
        // ========================================================
        if (visualCurrentNodeId != currentMapData.currentNodeId)
        {
            yield return StartCoroutine(MoveAvatarAndScrollMap(visualCurrentNodeId, currentMapData.currentNodeId));
            visualCurrentNodeId = currentMapData.currentNodeId;
        }
    }

    // ========================================================
    // 動態展演 3：玩家棋子移動 & 鏡頭(地圖)完美跟隨
    // ========================================================
    private IEnumerator MoveAvatarAndScrollMap(string fromId, string toId)
    {
        if (transitionFade != null) transitionFade.blocksRaycasts = true;

        Vector2 startLocalPos = GetNodeLocalPosition(fromId);
        Vector2 endLocalPos = GetNodeLocalPosition(toId);

        Vector2 mapStartPos = mapCanvasRect.anchoredPosition;
        
        // 核心運算：棋子往上走(Y變大)，地圖就要往下退(Y減去差值)，以確保棋子在螢幕上的相對位置不變
        float deltaY = endLocalPos.y - startLocalPos.y;
        float deltaX = endLocalPos.x - startLocalPos.x;
        Vector2 mapEndPos = mapStartPos - new Vector2(deltaX * 0.5f, deltaY); // X軸只跟隨一半，Y軸完全跟隨，畫面比較穩

        float t = 0f;
        while (t < avatarMoveDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.SmoothStep(0, 1, t / avatarMoveDuration);

            // 棋子移動 (加上一點拋物線的跳躍感)
            if (playerAvatar != null)
            {
                Vector2 currentPos = Vector2.Lerp(startLocalPos, endLocalPos, progress);
                float jumpHeight = Mathf.Sin(progress * Mathf.PI) * 50f; // 向上跳躍 50 像素
                playerAvatar.localPosition = currentPos + new Vector2(0, jumpHeight);
            }

            // 地圖反向捲動
            mapCanvasRect.anchoredPosition = Vector2.Lerp(mapStartPos, mapEndPos, progress);

            yield return null;
        }

        if (playerAvatar != null) playerAvatar.localPosition = endLocalPos;
        mapCanvasRect.anchoredPosition = mapEndPos;
        baseMapPos = mapEndPos; // 更新基底座標，讓下次操作從這裡開始

        if (transitionFade != null) transitionFade.blocksRaycasts = false;
    }

    // 輔助函式：取得節點相對於 MapContainer 的座標
    private Vector2 GetNodeLocalPosition(string id)
    {
        if (string.IsNullOrEmpty(id) || !spawnedNodeUIs.ContainsKey(id))
        {
            // 如果是起點(還沒選節點)，預設在第一層正下方的位置
            if (currentMapData.allNodes.Count > 0)
            {
                var firstNode = currentMapData.allNodes[0];
                return new Vector2(0, (firstNode.yPercent / 100f - 0.5f) * mapContainer.rect.height - 150f);
            }
            return new Vector2(0, -300f);
        }

        // 我們將棋子放在該節點的右下角一點點，避免完全擋住怪物圖示
        Vector2 nodePos = spawnedNodeUIs[id].GetComponent<RectTransform>().localPosition;
        return nodePos + new Vector2(40f, -20f); 
    }

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