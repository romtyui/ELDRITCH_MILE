using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace EldritchMile.Map
{
    // ⚠️ using 必須寫在 namespace 內部，而且要在所有型別宣告之前。
    // 尚未封存的舊 PerspectiveMapGenerator.cs 在全域命名空間也宣告了 MapData / RunNodeData，
    // 而檔案最上方的 using 是註冊在「全域層」—— 同一層的「宣告」永遠贏過「using 匯入」，
    // 所以放外面會綁到舊型別（而且編譯得過，只在型別轉換時才爆）。
    // 寫在 namespace 內部，這一層就會先採用 using，永遠指向 Core。
    using EldritchMile.Core;

    /// <summary>
    /// 連線顯示方式。三種各有取捨，沒有絕對正確的答案。
    /// </summary>
    public enum LineDisplayMode
    {
        /// 全部連線都畫出來。玩家看得到整張路網、能提前規劃（Slay the Spire 那種）。
        /// 代價是失去未知感，而且節點多時畫面很雜。
        AllConnections,

        /// **只畫走過的路徑**。畫面乾淨、保留未知感，符合恐怖探索的調性。
        /// 代價是玩家對前方一無所知 —— 但「可前往」的節點本來就是亮的，
        /// 所以資訊沒有真的消失，只是不用線表達。
        VisitedPathOnly,

        /// 走過的路徑 + 從當前節點通往可去節點的線。（預設）
        /// 保留未知感，同時用線明確指出「你現在能去哪」，
        /// 不必依賴玩家看得懂節點的明暗差異。
        VisitedPlusReachable,
    }

/// <summary>
/// 地圖的畫面層。取代舊的 PerspectiveMapGenerator。
///
/// 【職責大幅縮小】舊版一人分飾五角：資料層 + 地圖生成 + UI 繪製 + 場景載入卸載 + 黑幕。
/// 現在只剩「把 RunContext.mapData 畫出來」與「棋子移動動畫」：
///   · 資料 → EldritchMile.Core.MapData（Core）
///   · 生成 → MapGenerator（Core，純邏輯）
///   · 下拉收起 → MapOverlayController（Core，本類別的父類別）
///   · 場景轉換 → GameFlowManager（Core）
///   · 黑幕 → ScreenFader（Core）
///
/// 【C1】整場 run 只建一次節點。反覆下拉收起不會重生成，因此進度不會遺失 ——
/// 這正是舊架構「狀態隨場景死亡」的根治點。
/// </summary>
public class MapView : MapOverlayController
{
    [Header("容器")]
    [Tooltip("節點與連線的父物件")]
    public RectTransform mapContainer;

    [Header("節點 Prefab")]
    public GameObject eventNodePrefab;
    public GameObject combatNodePrefab;
    public GameObject bossNodePrefab;
    [Tooltip("留空則沿用 eventNodePrefab")]
    public GameObject shopNodePrefab;
    [Tooltip("留空則沿用 eventNodePrefab")]
    public GameObject specialEventNodePrefab;

    [Header("連線")]
    public GameObject linePrefab;
    public Vector2 lineSize = new Vector2(7f, 25f);

    [Tooltip("連線顯示方式。所有線在建圖時就都生成好，這裡只控制哪些顯示")]
    public LineDisplayMode lineDisplay = LineDisplayMode.VisitedPlusReachable;

    [Header("玩家棋子")]
    public RectTransform playerAvatar;
    [Tooltip("棋子相對節點中心的偏移")]
    public Vector2 avatarOffset = new Vector2(40f, -20f);
    public float avatarMoveDuration = 0.8f;
    [Tooltip("模擬走路起伏的高度")]
    public float bobbingHeight = 25f;
    [Tooltip("移動過程上下起伏的次數")]
    public float bobbingFrequency = 2f;

    [Header("進場動畫（由底層往上逐層淡入）")]
    [Tooltip("單一層淡入所需時間")]
    [FormerlySerializedAs("nodePopDuration")]
    public float layerFadeDuration = 0.35f;

    [Tooltip("層與層之間的間隔。設 0 則所有層同時淡入")]
    [FormerlySerializedAs("layerPopInterval")]
    public float layerFadeInterval = 0.12f;

    [Tooltip("淡入的緩動曲線")]
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Banner")]
    public MapBannerUI mapBannerUI;
    public string mapEnterText = "<color=#FFFFFF>地圖</color>";

    /// 一條連線的資料。記住 from/to 才能依 lineDisplay 決定顯示與否。
    private class MapLine
    {
        public RectTransform rect;
        public CanvasGroup group;   // 淡入用
        public string fromId;
        public string toId;
        public int layer;
    }

    private readonly Dictionary<string, MapNodeUI> spawnedNodes = new Dictionary<string, MapNodeUI>();
    private readonly List<MapLine> lines = new List<MapLine>();

    private MapData boundMap;
    private bool hasBuilt;
    private bool isMovingAvatar;
    private bool pendingIntro;

    // ==========================================
    // MapOverlayController 覆寫
    // ==========================================

    public override void Refresh(RunContext run)
    {
        if (run == null || run.mapData == null) return;

        // 換了一張地圖（新的一場 run）才重建
        if (!hasBuilt || boundMap != run.mapData)
        {
            Build(run.mapData);
            pendingIntro = true;
        }

        SyncState();
    }

    public override IEnumerator OnOpened()
    {
        if (mapBannerUI != null)
        {
            StartCoroutine(mapBannerUI.ShowMapTitle(mapEnterText));
        }

        if (pendingIntro)
        {
            pendingIntro = false;
            yield return FadeInByLayer();
        }

        SyncState();

        if (playerAvatar != null)
        {
            playerAvatar.gameObject.SetActive(true);
            playerAvatar.SetAsLastSibling();
            playerAvatar.anchoredPosition = GetNodePosition(boundMap?.currentNodeId);
        }
    }

    // ==========================================
    // 建圖
    // ==========================================

    private void Build(MapData map)
    {
        Clear();

        boundMap = map;
        hasBuilt = true;

        if (mapContainer == null)
        {
            Debug.LogError("[地圖] 沒有指定 mapContainer，無法繪製");
            return;
        }

        // 先全部生成，連線才有位置可算
        foreach (RunNodeData node in map.allNodes)
        {
            SpawnNode(node);
        }

        foreach (RunNodeData node in map.allNodes)
        {
            foreach (string nextId in node.nextNodeIds)
            {
                if (map.GetNode(nextId) != null)
                {
                    DrawLine(node, map.GetNode(nextId));
                }
            }
        }

        Debug.Log($"[地圖] 已繪製 {spawnedNodes.Count} 個節點");
    }

    private void Clear()
    {
        foreach (MapNodeUI node in spawnedNodes.Values)
        {
            if (node != null) Destroy(node.gameObject);
        }
        spawnedNodes.Clear();

        foreach (MapLine line in lines)
        {
            if (line.rect != null) Destroy(line.rect.gameObject);
        }
        lines.Clear();

        hasBuilt = false;
    }

    private void SpawnNode(RunNodeData data)
    {
        GameObject prefab = PrefabFor(data.kind);
        if (prefab == null)
        {
            Debug.LogWarning($"[地圖] {data.kind} 沒有指定 prefab，跳過節點 {data.nodeId}");
            return;
        }

        GameObject obj = Instantiate(prefab, mapContainer);
        obj.name = $"Node_{data.layer}_{data.kind}";

        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = PercentToLocal(data.xPercent, data.yPercent);

        var nodeUI = obj.GetComponent<MapNodeUI>();
        if (nodeUI == null)
        {
            Debug.LogError($"[地圖] {prefab.name} 缺少 MapNodeUI 元件");
            return;
        }

        nodeUI.Init(data, this);
        spawnedNodes[data.nodeId] = nodeUI;
    }

    private GameObject PrefabFor(MapNodeKind kind)
    {
        switch (kind)
        {
            case MapNodeKind.Combat: return combatNodePrefab;
            case MapNodeKind.Boss: return bossNodePrefab;
            case MapNodeKind.Shop: return shopNodePrefab != null ? shopNodePrefab : eventNodePrefab;
            case MapNodeKind.SpecialEvent: return specialEventNodePrefab != null ? specialEventNodePrefab : eventNodePrefab;
            default: return eventNodePrefab;
        }
    }

    private void DrawLine(RunNodeData from, RunNodeData to)
    {
        if (linePrefab == null) return;

        GameObject obj = Instantiate(linePrefab, mapContainer);
        obj.transform.SetAsFirstSibling();   // 連線壓在節點底下

        var rect = obj.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);

        Vector2 a = PercentToLocal(from.xPercent, from.yPercent);
        Vector2 b = PercentToLocal(to.xPercent, to.yPercent);
        Vector2 dir = b - a;

        rect.anchoredPosition = (a + b) * 0.5f;
        rect.sizeDelta = lineSize;
        rect.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f);
        rect.localScale = Vector3.one;   // 改用淡入後，scale 一律維持 1

        // 連線靠 CanvasGroup 淡入。linePrefab 通常沒有，這裡自動補上。
        var group = obj.GetComponent<CanvasGroup>();
        if (group == null) group = obj.AddComponent<CanvasGroup>();
        group.alpha = 1f;

        lines.Add(new MapLine
        {
            rect = rect,
            group = group,
            fromId = from.nodeId,
            toId = to.nodeId,
            layer = from.layer,
        });
    }

    /// <summary>
    /// 依 lineDisplay 決定哪些線要顯示。
    /// 所有線在建圖時就生成好了，這裡只切 SetActive —— 不重新生成，效能與狀態都穩定。
    /// </summary>
    private void RefreshLineVisibility()
    {
        if (boundMap == null) return;

        // 玩家實際走過的順序：歷史 + 當前
        var walked = new List<string>(boundMap.historyNodeIds);
        if (!string.IsNullOrEmpty(boundMap.currentNodeId))
        {
            walked.Add(boundMap.currentNodeId);
        }

        RunNodeData current = boundMap.CurrentNode;

        foreach (MapLine line in lines)
        {
            if (line.rect == null) continue;

            bool visible;

            switch (lineDisplay)
            {
                case LineDisplayMode.AllConnections:
                    visible = true;
                    break;

                case LineDisplayMode.VisitedPathOnly:
                    visible = IsWalkedPair(walked, line);
                    break;

                default: // VisitedPlusReachable
                    bool reachableFromHere =
                        current != null &&
                        line.fromId == current.nodeId &&
                        current.nextNodeIds.Contains(line.toId);

                    visible = IsWalkedPair(walked, line) || reachableFromHere;
                    break;
            }

            line.rect.gameObject.SetActive(visible);

            // 之後才變可見的線（例如玩家走到新節點）必須補滿透明度，
            // 否則會沿用進場動畫留下的 alpha 0 而看不見。
            if (visible && line.group != null) line.group.alpha = 1f;
        }
    }

    /// from → to 是否為玩家走過的相鄰兩步
    private static bool IsWalkedPair(List<string> walked, MapLine line)
    {
        for (int i = 0; i < walked.Count - 1; i++)
        {
            if (walked[i] == line.fromId && walked[i + 1] == line.toId) return true;
        }
        return false;
    }

    /// 百分比座標 → mapContainer 內的本地座標
    private Vector2 PercentToLocal(float xPercent, float yPercent)
    {
        Rect r = mapContainer.rect;
        return new Vector2(
            (xPercent / 100f - 0.5f) * r.width,
            (yPercent / 100f - 0.5f) * r.height
        );
    }

    // ==========================================
    // 狀態同步
    // ==========================================

    private void SyncState()
    {
        if (boundMap == null) return;

        RunNodeData current = boundMap.CurrentNode;
        List<string> reachable = current != null
            ? current.nextNodeIds
            : new List<string>();

        bool atStart = string.IsNullOrEmpty(boundMap.currentNodeId);

        foreach (var kvp in spawnedNodes)
        {
            MapNodeUI ui = kvp.Value;
            if (ui == null) continue;

            bool isCurrent = kvp.Key == boundMap.currentNodeId;
            bool isVisited = ui.Data.visited;
            bool selectable = atStart
                ? ui.Data.layer == 0
                : reachable.Contains(kvp.Key);

            ui.UpdateVisual(isCurrent, selectable, isVisited);
        }

        RefreshLineVisibility();
    }

    // ==========================================
    // 進場動畫
    // ==========================================

    /// <summary>
    /// 由底層往上逐層淡入。
    ///
    /// 【為什麼是由下往上】layer 0 的 yPercent 最小（畫面下方），maxLayer 最大（上方），
    /// 所以照 layer 遞增跑，視覺上就是從腳下往前方展開 —— 與玩家前進的方向一致。
    ///
    /// 【與舊版彈跳的差別】舊版靠 localScale 0→1.3→1 做彈跳。改成淡入後 scale 全程維持 1，
    /// 節點的大小差異就完全交給 UpdateVisual 表達狀態（當前 1.2 / 可選 1 / 其他 0.8），
    /// 兩者不再互相覆蓋。
    /// </summary>
    private IEnumerator FadeInByLayer()
    {
        foreach (MapNodeUI node in spawnedNodes.Values)
        {
            if (node != null) node.SetIntroAlpha(0f);
        }

        foreach (MapLine line in lines)
        {
            if (line.group != null) line.group.alpha = 0f;
        }

        if (playerAvatar != null) playerAvatar.gameObject.SetActive(false);

        int maxLayer = boundMap != null ? boundMap.MaxLayer : 0;

        for (int layer = 0; layer <= maxLayer; layer++)
        {
            var nodes = new List<MapNodeUI>();
            foreach (MapNodeUI n in spawnedNodes.Values)
            {
                if (n != null && n.Data.layer == layer) nodes.Add(n);
            }

            // 只動這一層目前有顯示的線
            var layerGroups = new List<CanvasGroup>();
            foreach (MapLine l in lines)
            {
                if (l.layer == layer && l.group != null && l.rect.gameObject.activeSelf)
                {
                    layerGroups.Add(l.group);
                }
            }

            float t = 0f;
            while (t < layerFadeDuration)
            {
                t += Time.unscaledDeltaTime;
                float a = fadeCurve.Evaluate(Mathf.Clamp01(t / layerFadeDuration));

                foreach (MapNodeUI n in nodes) n.SetIntroAlpha(a);
                foreach (CanvasGroup g in layerGroups) g.alpha = a;

                yield return null;
            }

            foreach (MapNodeUI n in nodes) n.SetIntroAlpha(1f);
            foreach (CanvasGroup g in layerGroups) g.alpha = 1f;

            if (layerFadeInterval > 0f)
            {
                yield return new WaitForSecondsRealtime(layerFadeInterval);
            }
        }
    }

    // ==========================================
    // 節點點擊 → 棋子移動 → 交給總管
    // ==========================================

    public void OnNodeClicked(RunNodeData node)
    {
        if (node == null || isMovingAvatar) return;

        if (GameFlowManager.Instance == null)
        {
            Debug.LogWarning("[地圖] 場上沒有 GameFlowManager");
            return;
        }

        if (GameFlowManager.Instance.IsTransitioning) return;

        StartCoroutine(MoveThenEnter(node));
    }

    private IEnumerator MoveThenEnter(RunNodeData node)
    {
        isMovingAvatar = true;

        string fromId = boundMap != null ? boundMap.currentNodeId : null;
        yield return MoveAvatar(fromId, node.nodeId);

        isMovingAvatar = false;

        // 收地圖、載入 Stage 全交給總管（鐵則 1：畫面層不做流程決策）
        GameFlowManager.Instance.EnterNode(node);
    }

    private IEnumerator MoveAvatar(string fromId, string toId)
    {
        if (playerAvatar == null) yield break;

        Vector2 start = GetNodePosition(fromId);
        Vector2 end = GetNodePosition(toId);

        float t = 0f;
        while (t < avatarMoveDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / avatarMoveDuration));

            Vector2 pos = Vector2.Lerp(start, end, p);

            // 用 Abs(Sin) 模擬腳步點地彈起
            float bounce = Mathf.Abs(Mathf.Sin(p * Mathf.PI * bobbingFrequency)) * bobbingHeight;
            playerAvatar.anchoredPosition = pos + new Vector2(0f, bounce);

            yield return null;
        }

        playerAvatar.anchoredPosition = end;
    }

    private Vector2 GetNodePosition(string nodeId)
    {
        if (!string.IsNullOrEmpty(nodeId) && spawnedNodes.TryGetValue(nodeId, out MapNodeUI ui) && ui != null)
        {
            return ui.GetComponent<RectTransform>().anchoredPosition + avatarOffset;
        }

        // 還沒出發：站在第 0 層下方
        if (boundMap != null && boundMap.allNodes.Count > 0)
        {
            RunNodeData first = boundMap.allNodes[0];
            return PercentToLocal(first.xPercent, first.yPercent) + new Vector2(0f, -120f);
        }

        return Vector2.zero;
    }
}
}
