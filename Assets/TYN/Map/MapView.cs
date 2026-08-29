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

    [Tooltip("菁英戰的節點。留空則沿用 combatNodePrefab。\n\n" +
             "美術是 `地圖物件_菁英怪物節點` —— 一般怪與菁英在地圖上長得不一樣，\n" +
             "玩家才能在**還沒點下去之前**就決定要不要繞路。\n\n" +
             "⚠️ 菁英不是一種 `MapNodeKind`，是 Combat 節點的 `enemyTier` ——\n" +
             "所以挑 prefab 要看整筆 RunNodeData，不能只看 kind")]
    public GameObject eliteNodePrefab;

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
    public float avatarMoveDuration = 0.9f;

    [Tooltip("位移的緩動曲線。\n\n" +
             "預設模擬「在磨砂石桌上推一顆棋子」：從靜止推出去（前段加速）→ 滑行 → " +
             "被摩擦力拖住、慢慢煞停（後段長而緩），**沒有回彈**。\n\n" +
             "手感是美術／企劃在調的東西，直接改這條曲線就好，不必動程式。\n" +
             "想更「重」就把後段拉更長；想更「輕快」就縮短前段的加速。")]
    public AnimationCurve avatarMoveCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f),
        new Keyframe(0.28f, 0.55f, 2.1f, 2.1f),
        new Keyframe(1f, 1f, 0f, 0f)
    );

    [Tooltip("滑行時垂直於行進方向的細微抖動（像素）。這是「磨砂材質」的觸感 ——\n" +
             "粗糙的檯面不會讓棋子滑得完美平順。\n\n" +
             "幅度會**跟著當下速度縮放**，所以快的時候明顯、快停下時自然消失。\n" +
             "設 0 則完全平滑。建議很小（1~3），大於 5 會變成在抖動而不是磨擦")]
    [Range(0f, 8f)] public float avatarGrainJitter = 2f;

    [Tooltip("抖動的顆粒密度。越大越細碎")]
    [Range(1f, 40f)] public float avatarGrainFrequency = 14f;

    [Header("進場動畫（由底層往上逐層淡入）")]
    [Tooltip("單一層淡入所需時間")]
    [FormerlySerializedAs("nodePopDuration")]
    public float layerFadeDuration = 0.35f;

    [Tooltip("層與層之間的間隔。設 0 則所有層同時淡入")]
    [FormerlySerializedAs("layerPopInterval")]
    public float layerFadeInterval = 0.12f;

    [Tooltip("淡入的緩動曲線")]
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("節點 Tooltip")]
    [Tooltip("hover 節點時顯示的說明框。留空則不顯示 tooltip（節點仍可點）")]
    public MapTooltipUI nodeTooltip;

    [Tooltip("各節點類型的說明文案。\n\n" +
             "**還沒實作的類型也先寫上** —— 玩家看到「商店：尚未開放」比看到什麼都沒有好，\n" +
             "而且之後功能做完只要改這裡的文字，不用動程式。\n\n" +
             "⚠️ 用 Inspector 的 + 新增時 Kind 一律是 Event（零填充），記得逐筆改。\n" +
             "沒有對應條目的類型會退回顯示 enum 名稱，不會是空白")]
    public List<NodeTooltipInfo> nodeTooltipTexts = new List<NodeTooltipInfo>();

    [Header("節點 Tooltip — 難度")]
    [Tooltip("戰鬥節點的難度標示。**一般雜魚刻意留空** ——\n" +
             "每一站都標「普通」的話，「菁英」那兩個字就不顯眼了；\n" +
             "只標特別的，玩家才會注意到")]
    public string tooltipTierMinion = "";
    public string tooltipTierElite = "<color=#FF9B6A>◆ 菁英 —— 這一站不好惹。</color>";
    public string tooltipTierBoss = "<color=#FF6A6A>◆◆ 首領。</color>";

    [Header("節點 Tooltip — 狀態附註")]
    [Tooltip("接在說明後面的一行，講「你現在能不能去」。留空則不附加")]
    public string tooltipStateCurrent = "<color=#FFD98A>你在這裡。</color>";
    public string tooltipStateSelectable = "<color=#9BE39B>可以前往。</color>";
    public string tooltipStateVisited = "<color=#8A8A8A>已經去過了。</color>";
    public string tooltipStateUnreachable = "<color=#8A8A8A>從這裡過不去。</color>";

    [Header("Banner")]
    public MapBannerUI mapBannerUI;
    public string mapEnterText = "<color=#FFFFFF>地圖</color>";

    /// <summary>一種節點類型的 tooltip 文案。</summary>
    [System.Serializable]
    public class NodeTooltipInfo
    {
        public MapNodeKind kind = MapNodeKind.Event;

        [Tooltip("標題，例如「戰鬥」")]
        public string title = "";

        [TextArea(2, 4)]
        [Tooltip("說明。未實作的功能就直接寫「尚未開放」之類")]
        public string body = "";
    }

    // ==========================================
    // 節點 Tooltip
    // ==========================================

    /// <summary>
    /// 由 `MapNodeUI` 在 hover 時呼叫。
    ///
    /// 【為什麼經過 MapView】節點只知道自己，不知道文案表在哪、也不該知道
    /// tooltip 是用哪一套元件畫的。集中在這裡的好處是：日後若要改成全遊戲共用一套
    /// tooltip（例如 Romtyui 那份），只要改這個方法，26 個節點的程式一行都不用動。
    /// </summary>
    public void ShowNodeTooltip(MapNodeUI node)
    {
        if (nodeTooltip == null || node == null || node.Data == null) return;

        NodeTooltipInfo info = FindTooltipInfo(node.Data.kind);

        // 找不到文案就退回顯示 enum 名稱 —— 醜，但比整個框空白好，
        // 而且一眼就看得出「這個類型的文案還沒寫」
        string title = info != null && !string.IsNullOrEmpty(info.title)
            ? info.title
            : node.Data.kind.ToString();

        string body = info != null ? info.body : "";

        // 難度先講 —— 「這站硬不硬」是玩家在分岔口最想知道的事，
        // 排在說明後面的話會被當成附註掃過去
        string tierNote = TierNote(node.Data);
        if (!string.IsNullOrEmpty(tierNote))
        {
            body = string.IsNullOrEmpty(body) ? tierNote : tierNote + "\n" + body;
        }

        string state = StateNote(node.State);
        if (!string.IsNullOrEmpty(state))
        {
            body = string.IsNullOrEmpty(body) ? state : body + "\n" + state;
        }

        nodeTooltip.Show(title, body, node.GetComponent<RectTransform>());
    }

    /// <summary>
    /// 離開節點時關閉。
    ///
    /// ⚠️ 移動中不關 —— 棋子在跑的時候整張地圖不該有互動回饋在閃。
    /// </summary>
    public void HideNodeTooltip(MapNodeUI node)
    {
        if (nodeTooltip != null) nodeTooltip.Hide();
    }

    /// <summary>
    /// 離開地圖時真的關掉說明框，連「閒置時保留框」也不留。
    ///
    /// 【為什麼要分兩支】固定在角落的面板勾了 `Keep Frame When Idle` 之後，
    /// 一般的 `Hide()` 只會換回閒置文字 —— 那在還看著地圖時是對的，
    /// 但地圖收起來、進了探索房間之後，那個框會孤零零浮在別的畫面上。
    /// </summary>
    public void ForceHideNodeTooltip()
    {
        if (nodeTooltip != null) nodeTooltip.ForceHide();
    }

    private NodeTooltipInfo FindTooltipInfo(MapNodeKind kind)
    {
        for (int i = 0; i < nodeTooltipTexts.Count; i++)
        {
            NodeTooltipInfo info = nodeTooltipTexts[i];
            if (info != null && info.kind == kind) return info;
        }
        return null;
    }

    /// <summary>
    /// 這一站的難度標示。只有戰鬥／Boss 節點有，而且**一般雜魚不標**。
    ///
    /// 【為什麼不標普通】每一站都掛一個「普通」的話，「菁英」就淹沒在裡面了。
    /// 標示的價值來自稀有 —— 這也是這種分岔地圖存在的意義：
    /// 玩家要能看出「那條路比較硬」，才有選擇可言。
    /// </summary>
    private string TierNote(RunNodeData data)
    {
        if (data == null) return "";
        if (data.kind != MapNodeKind.Combat && data.kind != MapNodeKind.Boss) return "";

        switch (data.enemyTier)
        {
            case EncounterPool.Tier.Elite: return tooltipTierElite;
            case EncounterPool.Tier.Boss: return tooltipTierBoss;
            default: return tooltipTierMinion;
        }
    }

    private string StateNote(MapNodeUI.NodeState state)
    {
        switch (state)
        {
            case MapNodeUI.NodeState.Current: return tooltipStateCurrent;
            case MapNodeUI.NodeState.Selectable: return tooltipStateSelectable;
            case MapNodeUI.NodeState.Visited: return tooltipStateVisited;
            default: return tooltipStateUnreachable;
        }
    }

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

    /// <summary>
    /// 地圖要收起來了 —— 說明框的總開關關掉。
    ///
    /// ⚠️ 這裡**不能只是 ForceHide()**。地圖是滑出畫面的，滑走的瞬間節點會離開游標，
    /// Unity 隨後送出 `OnPointerExit` → `Hide()` → 而 `Keep Frame When Idle`
    /// 又把框重新開起來。也就是關閉動作會被一個**比它晚到的滑鼠事件**復活。
    ///
    /// 所以用 `SetSuppressed(true)`：關掉之後所有顯示要求一律失效，直到地圖再次展開。
    ///
    /// 【為什麼不能靠節點的 OnDisable】覆蓋層從頭到尾沒有 `SetActive(false)`，
    /// 節點一直是啟用狀態。而說明框為了「不跟著地圖上下移動」放在滑動面板外面，
    /// 更不會被自動收掉。
    /// </summary>
    protected override void OnClosing()
    {
        if (nodeTooltip != null) nodeTooltip.SetSuppressed(true);
    }

    public override IEnumerator OnOpened()
    {
        // 地圖展開 → 說明框的總開關打開（固定面板會回到閒置文字）
        if (nodeTooltip != null) nodeTooltip.SetSuppressed(false);

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
        GameObject prefab = PrefabFor(data);
        if (prefab == null)
        {
            Debug.LogWarning($"[地圖] {data.kind}／{data.enemyTier} 沒有指定 prefab，跳過節點 {data.nodeId}");
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

    /// <summary>
    /// 這個節點要用哪一個 prefab。
    ///
    /// 【為什麼吃整筆 data 而不是只吃 kind】菁英**不是**一種 MapNodeKind ——
    /// 它是 Combat 節點加上 `enemyTier = Elite`（EncounterPlanner 決定的）。
    /// 只看 kind 的話菁英與雜兵會長得一模一樣，而 tooltip 早就分得出來了
    /// （`tooltipTierElite`），圖示卻分不出來，那是不一致。
    /// </summary>
    private GameObject PrefabFor(RunNodeData data)
    {
        switch (data.kind)
        {
            case MapNodeKind.Combat:
                return data.enemyTier == EncounterPool.Tier.Elite && eliteNodePrefab != null
                    ? eliteNodePrefab
                    : combatNodePrefab;

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

        // 重建地圖時舊的說明框會指向已經被 Destroy 的節點，先收掉
        if (nodeTooltip != null) nodeTooltip.HideImmediate();

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

        // 選定了就收掉說明框 —— 棋子開始移動時畫面上不該還有 hover 的殘留，
        // 而且節點馬上要被 UpdateVisual 改狀態，框裡的文字會過期
        if (nodeTooltip != null) nodeTooltip.HideImmediate();

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

    /// <summary>
    /// 棋子移動。
    ///
    /// 【手感】不是走路，是**在磨砂石桌上推一顆西洋棋**：
    ///   · 沒有上下彈跳 —— 棋子是滑的，不是走的（舊版用 Abs(Sin) 做腳步起伏，已移除）
    ///   · 位移交給 avatarMoveCurve：推出去 → 滑行 → 摩擦煞停，**不回彈**
    ///   · 滑行時垂直方向有極細的抖動，模擬粗糙檯面的觸感
    ///
    /// 【抖動為什麼綁速度】固定幅度的抖動在快停下時會變成「原地發抖」，很假。
    /// 綁著當下速度就會自然收斂 —— 停下的瞬間剛好歸零，不需要額外的收尾處理。
    /// </summary>
    private IEnumerator MoveAvatar(string fromId, string toId)
    {
        if (playerAvatar == null) yield break;

        Vector2 start = GetNodePosition(fromId);
        Vector2 end = GetNodePosition(toId);

        Vector2 delta = end - start;
        float distance = delta.magnitude;

        // 垂直於行進方向的單位向量。距離為 0 時不抖，避免除以零
        Vector2 perpendicular = distance > 0.001f
            ? new Vector2(-delta.y, delta.x) / distance
            : Vector2.zero;

        // 每次移動換一組雜訊起點，否則每一步的抖動花紋會一模一樣
        float noiseSeed = Random.value * 100f;

        float t = 0f;
        float previousProgress = 0f;

        while (t < avatarMoveDuration)
        {
            t += Time.deltaTime;

            float linear = Mathf.Clamp01(t / avatarMoveDuration);
            float p = avatarMoveCurve.Evaluate(linear);

            Vector2 pos = Vector2.Lerp(start, end, p);

            // 當下速度相對於「等速移動」的倍率。等速時 ≈ 1，煞停時 → 0
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            float speed01 = Mathf.Clamp01((p - previousProgress) / dt * avatarMoveDuration);
            previousProgress = p;

            if (avatarGrainJitter > 0f && perpendicular != Vector2.zero)
            {
                // 用 Perlin 而不是 Random：要的是連續的粗糙感，不是每幀亂跳的雜訊
                float noise = Mathf.PerlinNoise(noiseSeed + linear * avatarGrainFrequency, 0f) - 0.5f;
                pos += perpendicular * (noise * 2f * avatarGrainJitter * speed01);
            }

            playerAvatar.anchoredPosition = pos;

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
