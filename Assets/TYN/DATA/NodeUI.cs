using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class NodeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public MapNode nodeData;
    public Image   nodeIcon;
    [Tooltip("選中光環圖片（可選，預設隱藏）")]
    public Image   hoverRing;

    // ── 顏色定義 ────────────────────────────────────────────────
    private static readonly Color ColorCurrent    = new Color(1.00f, 0.95f, 0.20f, 1.0f); // 金黃
    private static readonly Color ColorSelectable = new Color(0.20f, 0.90f, 0.50f, 1.0f); // 翠綠
    private static readonly Color ColorVisited    = new Color(0.35f, 0.35f, 0.40f, 1.0f); // 暗灰
    private static readonly Color ColorLocked     = new Color(0.12f, 0.12f, 0.15f, 0.5f); // 幾乎不可見
    private static readonly Color ColorHoverRing  = new Color(1.00f, 0.85f, 0.20f, 0.8f); // 金色光環

    private Button       button;
    private MapUIManager uiManager;
    private Vector3      baseScale;
    private bool         isHovered;
    private bool         isSelectable;
    private Coroutine    pulseCoroutine;

    private void Awake()
    {
        button    = GetComponent<Button>();
        baseScale = transform.localScale;

        // 把 click 邏輯移到 OnClicked guard，使 interactable 不影響 hover 事件
        button.onClick.AddListener(OnClicked);
        button.interactable = true;

        if (hoverRing) hoverRing.raycastTarget = false;
    }

    public void Setup(MapNode data, MapUIManager manager)
    {
        nodeData  = data;
        uiManager = manager;
        if (hoverRing) hoverRing.enabled = false;
        UpdateVisualStatus();
    }

    // ── 點擊 ────────────────────────────────────────────────────

    private void OnClicked()
    {
        if (isSelectable)
            GameManager.Instance.OnNodeClicked(nodeData);
    }

    // ── Hover 事件 ───────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        transform.localScale = baseScale * 1.18f;
        if (hoverRing)
        {
            hoverRing.enabled = true;
            hoverRing.color   = ColorHoverRing;
        }
        if (uiManager) uiManager.ShowTooltip(nodeData, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (!isSelectable) transform.localScale = baseScale;
        if (hoverRing) hoverRing.enabled = false;
        if (uiManager) uiManager.HideTooltip();
    }

    // ── 視覺狀態 ─────────────────────────────────────────────────

    public void UpdateVisualStatus()
    {
        if (nodeIcon == null) return;

        var  gm        = GameManager.Instance;
        bool isCurrent = gm.currentNode?.id == nodeData.id;
        isSelectable   = gm.selectableNodeIds.Contains(nodeData.id);
        bool isVisited = gm.visitedNodeIds.Contains(nodeData.id);

        if (isCurrent)
            nodeIcon.color = ColorCurrent;
        else if (isSelectable)
            nodeIcon.color = ColorSelectable;
        else if (isVisited)
            nodeIcon.color = ColorVisited;
        else
            nodeIcon.color = ColorLocked;

        // 可選節點做脈動動畫；其餘停止
        if (isSelectable && !isCurrent)
        {
            pulseCoroutine ??= StartCoroutine(PulseSelectable());
        }
        else
        {
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }
            if (!isHovered) transform.localScale = baseScale;
        }
    }

    // ── 脈動動畫 ─────────────────────────────────────────────────

    private IEnumerator PulseSelectable()
    {
        float elapsed = 0f;
        while (true)
        {
            elapsed += Time.deltaTime;
            // 0~1 之間的平滑呼吸感（1.0 ~ 1.08 之間）
            float t = (Mathf.Sin(elapsed * Mathf.PI * 1.4f) + 1f) * 0.5f;
            if (!isHovered)
                transform.localScale = baseScale * Mathf.Lerp(1f, 1.08f, t);
            yield return null;
        }
    }
}
