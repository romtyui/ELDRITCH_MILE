using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapUIManager : MonoBehaviour
{
    [Header("Map")]
    public RectTransform mapContainer;
    public GameObject    nodePrefab;
    [Tooltip("可選：將 mapContainer 掛在 ScrollRect 的 Content 下以支援捲動")]
    public ScrollRect    mapScrollRect;

    [Header("Stats UI")]
    public TextMeshProUGUI hullText;
    public TextMeshProUGUI sanityText;
    public TextMeshProUGUI fuelText;
    public TextMeshProUGUI scrapText;

    [Header("Log UI")]
    public TextMeshProUGUI logText;
    public int maxLogLines = 8;

    [Header("Tooltip UI")]
    [Tooltip("Tooltip 根物件（初始隱藏）")]
    public GameObject      tooltipPanel;
    [Tooltip("節點類型名稱文字")]
    public TextMeshProUGUI tooltipTypeName;
    [Tooltip("節點說明文字")]
    public TextMeshProUGUI tooltipDescription;

    private readonly List<NodeUI> spawnedNodes = new List<NodeUI>();
    private readonly Queue<string> logLines    = new Queue<string>();
    private Dictionary<string, MapNode> nodeById;
    private Coroutine hideCoroutine;

    private void Awake()
    {
        if (tooltipPanel)
        {
            tooltipPanel.SetActive(false);
            var cg = tooltipPanel.GetComponent<CanvasGroup>() ?? tooltipPanel.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable   = false;
        }
    }

    // ── 地圖繪製 ────────────────────────────────────────────────

    public void DrawMap(List<MapNode> nodes, Dictionary<string, MapNode> lookup)
    {
        nodeById = lookup;

        foreach (Transform child in mapContainer) Destroy(child.gameObject);
        spawnedNodes.Clear();

        LayoutRebuilder.ForceRebuildLayoutImmediate(mapContainer);
        Vector2 containerSize = mapContainer.rect.size;

        // 先畫連線（渲染在節點圖標之下）
        foreach (var node in nodes)
            foreach (var childId in node.children)
                if (nodeById.TryGetValue(childId, out var child))
                    DrawLine(node, child, containerSize);

        // 再生成節點
        foreach (var node in nodes)
        {
            var obj  = Instantiate(nodePrefab, mapContainer);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin        = new Vector2(node.x / 100f, node.y / 100f);
            rect.anchorMax        = new Vector2(node.x / 100f, node.y / 100f);
            rect.anchoredPosition = Vector2.zero;

            var nodeUI = obj.GetComponent<NodeUI>();
            nodeUI.Setup(node, this);
            spawnedNodes.Add(nodeUI);
        }

        if (mapScrollRect != null)
            StartCoroutine(InitScrollPosition());
    }

    private IEnumerator InitScrollPosition()
    {
        yield return null;
        if (mapScrollRect) mapScrollRect.verticalNormalizedPosition = 0.5f;
    }

    private void DrawLine(MapNode start, MapNode end, Vector2 containerSize)
    {
        var lineObj = new GameObject($"Line_{start.id}_{end.id}", typeof(Image));
        lineObj.transform.SetParent(mapContainer, false);
        lineObj.transform.SetAsFirstSibling();

        var img = lineObj.GetComponent<Image>();
        img.color = new Color(0.2f, 0.7f, 0.9f, 0.25f);

        var rect = lineObj.GetComponent<RectTransform>();
        rect.pivot     = new Vector2(0.5f, 0f);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;

        Vector2 startPos = new Vector2(start.x / 100f * containerSize.x, start.y / 100f * containerSize.y);
        Vector2 endPos   = new Vector2(end.x   / 100f * containerSize.x, end.y   / 100f * containerSize.y);
        Vector2 dir      = endPos - startPos;

        rect.anchoredPosition = startPos;
        rect.sizeDelta        = new Vector2(3f, dir.magnitude);
        rect.localRotation    = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
    }

    // ── 狀態更新 ────────────────────────────────────────────────

    public void UpdateMapVisuals()
    {
        foreach (var nodeUI in spawnedNodes) nodeUI.UpdateVisualStatus();
    }

    public void UpdateStatsUI()
    {
        var gm = GameManager.Instance;
        if (hullText)   hullText.text   = $"HULL    {gm.hull}%";
        if (sanityText) sanityText.text = $"SANITY  {gm.sanity}";
        if (fuelText)   fuelText.text   = $"FUEL    {gm.fuel}";
        if (scrapText)  scrapText.text  = $"SCRAP   {gm.scrap}";
    }

    public void AddLog(string msg)
    {
        logLines.Enqueue(msg);
        if (logLines.Count > maxLogLines) logLines.Dequeue();
        if (logText) logText.text = string.Join("\n", logLines);
        Debug.Log($"> {msg}");
    }

    // ── 捲動到當前節點 ──────────────────────────────────────────

    public void FocusOnCurrentNode()
    {
        if (mapScrollRect == null || GameManager.Instance.currentNode == null) return;
        // mapNode.y 是 0~100，底部 = 0，頂部 = 100
        // ScrollRect.verticalNormalizedPosition: 0=底, 1=頂
        float target = GameManager.Instance.currentNode.y / 100f;
        mapScrollRect.verticalNormalizedPosition = target;
    }

    // ── Tooltip ─────────────────────────────────────────────────

    public void ShowTooltip(MapNode node, Vector2 screenPos)
    {
        if (tooltipPanel == null) return;
        if (hideCoroutine != null) { StopCoroutine(hideCoroutine); hideCoroutine = null; }

        tooltipPanel.SetActive(true);
        if (tooltipTypeName)    tooltipTypeName.text    = NodeTypeDisplay.GetName(node.type);
        if (tooltipDescription) tooltipDescription.text = NodeTypeDisplay.GetDescription(node.type);
        tooltipPanel.transform.position = new Vector3(screenPos.x + 90f, screenPos.y - 20f, 0f);
    }

    public void HideTooltip()
    {
        if (tooltipPanel == null) return;
        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideNextFrame());
    }

    private IEnumerator HideNextFrame()
    {
        yield return null;
        if (tooltipPanel) tooltipPanel.SetActive(false);
        hideCoroutine = null;
    }
}
