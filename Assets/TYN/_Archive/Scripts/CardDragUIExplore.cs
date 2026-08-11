using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic; // 新增這行供 UI Raycast 使用

[RequireComponent(typeof(RectTransform))]
public class CardDragUIExplore : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public bool IsDragging { get; private set; }

    [Header("Play Rule")]
    public float playThresholdPixels = 140f;
    private RectTransform rectTransform;
    // private HandFanLayout handLayout; 
    private CardHoverUIExplore hoverUI;
    private TargetArrowUI targetArrow;
    private CanvasGroup canvasGroup;
    private CardViewUIExplore cardViewUI; 
    private CardExplorationManager explorationManager; 
    private Canvas canvas;

    private Vector2 pointerDownScreenPos;
    private Vector2 startAnchoredPosition;

    private bool useTargetArrowMode;
    private bool useDirectDragMode;

    [Header("Play Threshold Debug")]
    public float playThresholdY = 180f;
    public bool showPlayThresholdLine = true;
    public Color thresholdLineColor = new Color(1f, 0.2f, 0.2f, 0.8f);
    public float thresholdLineHeight = 4f;

    private GameObject thresholdLineObject;
    private RectTransform thresholdLineRect;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        // handLayout = GetComponentInParent<HandFanLayout>();
        hoverUI = GetComponent<CardHoverUIExplore>();
        canvasGroup = GetComponent<CanvasGroup>();
        cardViewUI = GetComponent<CardViewUIExplore>();
        
        explorationManager = FindFirstObjectByType<CardExplorationManager>();
        canvas = GetComponentInParent<Canvas>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        EnsureTargetArrow();
    }

    private void EnsureTargetArrow()
    {
        if (targetArrow != null) return;
        if (TargetArrowUI.Instance != null)
        {
            targetArrow = TargetArrowUI.Instance;
            return;
        }
        targetArrow = FindFirstObjectByType<TargetArrowUI>(FindObjectsInactive.Include);
    }

    private ExplorationTargetMode GetCurrentTargetMode()
    {
        if (cardViewUI == null) cardViewUI = GetComponent<CardViewUIExplore>();
        if (cardViewUI == null || cardViewUI.CardInstance == null || cardViewUI.CardInstance.data == null)
            return ExplorationTargetMode.None;

        return cardViewUI.CardInstance.data.targetMode;
    }

    private bool ShouldUseTargetArrowMode() { return GetCurrentTargetMode() == ExplorationTargetMode.SceneInteractable; }
    private bool ShouldUseDirectDragMode() { return GetCurrentTargetMode() == ExplorationTargetMode.None; }

    private void UpdateDirectDragPosition(Vector2 currentScreenPosition)
    {
        if (rectTransform == null) return;
        float scaleFactor = 1f;
        if (canvas != null) scaleFactor = Mathf.Max(0.01f, canvas.scaleFactor);
        Vector2 screenDelta = currentScreenPosition - pointerDownScreenPos;
        rectTransform.anchoredPosition = startAnchoredPosition + screenDelta / scaleFactor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        IsDragging = true;
        pointerDownScreenPos = eventData.position;
        startAnchoredPosition = rectTransform.anchoredPosition;

        useTargetArrowMode = ShouldUseTargetArrowMode();
        useDirectDragMode = ShouldUseDirectDragMode();

        ShowThresholdLine();

        rectTransform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;

        if (useTargetArrowMode)
        {
            EnsureTargetArrow();
            if (targetArrow != null)
            {
                targetArrow.Show(rectTransform);
                targetArrow.UpdateArrow(eventData.position);
            }
        }
        else
        {
            if (targetArrow != null) targetArrow.Hide();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsDragging) return;

        if (useTargetArrowMode)
        {
            EnsureTargetArrow();
            if (targetArrow != null) targetArrow.UpdateArrow(eventData.position);
            return;
        }

        if (useDirectDragMode) UpdateDirectDragPosition(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsDragging) return;

        IsDragging = false;
        canvasGroup.blocksRaycasts = true;

        EnsureTargetArrow();
        HideThresholdLine();

        if (targetArrow != null) targetArrow.Hide();

        bool played = false;
        bool draggedToPlayArea = IsDraggedToPlayArea(eventData.position);

        if (draggedToPlayArea && explorationManager != null && cardViewUI != null && cardViewUI.CardInstance != null)
        {
            CardInstanceExplore card = cardViewUI.CardInstance;
            ExplorationInteractableTarget targetUnit = null;

            if (useTargetArrowMode)
            {
                // ========================================================
                // 1. 優先檢查 UI 元素 (因為對話選項通常是 UI)
                // ========================================================
                PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = eventData.position };
                List<RaycastResult> uiHits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, uiHits);
                
                foreach (var hit in uiHits)
                {
                    ExplorationInteractableTarget tgt = hit.gameObject.GetComponent<ExplorationInteractableTarget>();
                    if (tgt != null)
                    {
                        targetUnit = tgt;
                        break;
                    }
                }

                // ========================================================
                // 2. 如果沒打到 UI，再檢查 3D 場景中的物件 (例如之前的怪物)
                // ========================================================
                if (targetUnit == null && explorationManager.playerCamera != null)
                {
                    Ray ray = explorationManager.playerCamera.ScreenPointToRay(eventData.position);
                    RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
                    foreach (var hit in hits)
                    {
                        ExplorationInteractableTarget tgt = hit.collider.GetComponent<ExplorationInteractableTarget>();
                        if (tgt != null)
                        {
                            targetUnit = tgt;
                            break;
                        }
                    }
                }
            }

            switch (card.data.targetMode)
            {
                case ExplorationTargetMode.SceneInteractable:
                    if (targetUnit != null) played = explorationManager.TryPlayCard(card, targetUnit);
                    else Debug.Log("未鎖定可互動目標，打出失敗");
                    break;
                case ExplorationTargetMode.None:
                    played = explorationManager.TryPlayCard(card, null);
                    break;
            }
        }

        if (!played) ReturnToHand();
    }

    private bool IsDraggedToPlayArea(Vector2 pointerUpScreenPos)
    {
        float deltaY = pointerUpScreenPos.y - pointerDownScreenPos.y;
        return deltaY >= playThresholdPixels;
    }

    private void ReturnToHand()
    {
        rectTransform.anchoredPosition = startAnchoredPosition;
    }

    private void ShowThresholdLine()
    {
        if (!showPlayThresholdLine) return;
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        RectTransform parentRect = rectTransform.parent as RectTransform;
        if (parentRect == null) return;

        if (thresholdLineObject == null)
        {
            thresholdLineObject = new GameObject("Play Threshold Line");
            thresholdLineObject.transform.SetParent(parentRect, false);
            Image image = thresholdLineObject.AddComponent<Image>();
            image.color = thresholdLineColor;
            image.raycastTarget = false;

            thresholdLineRect = thresholdLineObject.GetComponent<RectTransform>();
            thresholdLineRect.anchorMin = new Vector2(0f, 0.5f);
            thresholdLineRect.anchorMax = new Vector2(1f, 0.5f);
            thresholdLineRect.pivot = new Vector2(0.5f, 0.5f);
        }

        float lineY = startAnchoredPosition.y + playThresholdY;
        thresholdLineRect.anchoredPosition = new Vector2(0f, lineY);
        thresholdLineRect.sizeDelta = new Vector2(0f, thresholdLineHeight);
        thresholdLineObject.SetActive(true);
        thresholdLineObject.transform.SetAsLastSibling();
    }

    private void HideThresholdLine()
    {
        if (thresholdLineObject != null) thresholdLineObject.SetActive(false);
    }
}