using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class ExplorationCardDragUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Refs")]
    public CardExplorationManager manager; // 已更新為新名稱
    public CardViewUIExplore cardViewUI;
    public HandFanLayout handLayout;

    [Header("Drag")]
    public float playThresholdPixels = 80f;
    public LayerMask worldInteractableLayerMask = ~0;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas rootCanvas;
    private Camera canvasCamera;

    private Vector2 startAnchoredPosition;
    private Vector2 pointerDownScreenPosition;
    private Transform startParent;
    private bool isDragging;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        rootCanvas = GetComponentInParent<Canvas>();
        
        if (cardViewUI == null) cardViewUI = GetComponent<CardViewUIExplore>();
        
        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            canvasCamera = rootCanvas.worldCamera;
        else
            canvasCamera = null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (cardViewUI == null || cardViewUI.CardInstance == null) return;
        
        isDragging = true;
        pointerDownScreenPosition = eventData.position;
        startAnchoredPosition = rectTransform.anchoredPosition;
        startParent = transform.parent;
        canvasGroup.blocksRaycasts = false;

        // 如果你原本的專案沒有 CardHoverUI 腳本，這裡編譯會報錯。
        // 請確保你的專案裡有 CardHoverUI 腳本，因為 HandFanLayout 會用到它。
        if (handLayout != null) 
            handLayout.SetLockedCard(GetComponent<CardHoverUI>());
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        RectTransform parentRect = rectTransform.parent as RectTransform;
        if (parentRect == null) return;
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, eventData.position, canvasCamera, out Vector2 localPoint);
            
        rectTransform.anchoredPosition = localPoint;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = Vector3.one * 1.1f;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        isDragging = false;
        canvasGroup.blocksRaycasts = true;
        
        bool draggedEnough = IsDraggedEnough(eventData.position);
        if (!draggedEnough)
        {
            ReturnToHand();
            return;
        }

        CardInstanceExplore card = cardViewUI.CardInstance;
        ExplorationInteractableTarget target = GetExplorationTargetUnderPointer(eventData);
        bool played = false;

        if (manager != null)
        {
            played = manager.TryPlayCard(card, target);
        }

        if (!played) ReturnToHand();
        
        if (handLayout != null) handLayout.ClearAllSelection();
    }

    private bool IsDraggedEnough(Vector2 pointerUpScreenPosition)
    {
        float deltaY = pointerUpScreenPosition.y - pointerDownScreenPosition.y;
        return deltaY >= playThresholdPixels;
    }

    private ExplorationInteractableTarget GetExplorationTargetUnderPointer(PointerEventData eventData)
    {
        ExplorationInteractableTarget uiTarget = GetUITargetUnderPointer(eventData);
        if (uiTarget != null) return uiTarget;
        
        ExplorationInteractableTarget worldTarget = GetWorldTargetUnderPointer(eventData.position);
        return worldTarget;
    }

    private ExplorationInteractableTarget GetUITargetUnderPointer(PointerEventData eventData)
    {
        if (EventSystem.current == null) return null;
        
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        
        for (int i = 0; i < results.Count; i++)
        {
            ExplorationInteractableTarget target = results[i].gameObject.GetComponentInParent<ExplorationInteractableTarget>();
            if (target != null) return target;
        }
        return null;
    }

    private ExplorationInteractableTarget GetWorldTargetUnderPointer(Vector2 screenPosition)
    {
        if (manager == null || manager.playerCamera == null) return null;
        
        Ray ray = manager.playerCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, worldInteractableLayerMask))
        {
            ExplorationInteractableTarget target = hit.collider.GetComponentInParent<ExplorationInteractableTarget>();
            return target;
        }
        return null;
    }

    private void ReturnToHand()
    {
        rectTransform.SetParent(startParent, true);
        rectTransform.anchoredPosition = startAnchoredPosition;
        rectTransform.localScale = Vector3.one;
        if (handLayout != null) handLayout.RefreshLayout();
    }
}