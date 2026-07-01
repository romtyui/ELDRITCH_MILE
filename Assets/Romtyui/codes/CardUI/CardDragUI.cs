using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class CardDragUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public bool IsDragging { get; private set; }

    [Header("Play Rule")]
    public float playThresholdPixels = 140f;
    // 滑鼠從按下位置往上拖超過這個距離，才算打出

    private RectTransform rectTransform;
    private HandFanLayout handLayout;
    private CardHoverUI hoverUI;
    private TargetArrowUI targetArrow;
    private CanvasGroup canvasGroup;
    private CardViewUI cardViewUI;
    private BattleManager battleManager;
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
        handLayout = GetComponentInParent<HandFanLayout>();
        hoverUI = GetComponent<CardHoverUI>();
        canvasGroup = GetComponent<CanvasGroup>();
        cardViewUI = GetComponent<CardViewUI>();
        battleManager = FindFirstObjectByType<BattleManager>();
        canvas = GetComponentInParent<Canvas>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        EnsureTargetArrow();
    }

    private void EnsureTargetArrow()
    {
        if (targetArrow != null)
            return;

        if (TargetArrowUI.Instance != null)
        {
            targetArrow = TargetArrowUI.Instance;
            return;
        }

        targetArrow = FindFirstObjectByType<TargetArrowUI>(FindObjectsInactive.Include);

        if (targetArrow == null)
            Debug.LogWarning("[CardDragUI] 找不到 TargetArrowUI");
    }
    private TargetType GetCurrentTargetType()
    {
        if (cardViewUI == null)
            cardViewUI = GetComponent<CardViewUI>();

        if (cardViewUI == null)
            return TargetType.None;

        if (cardViewUI.CardInstance == null || cardViewUI.CardInstance.data == null)
            return TargetType.None;

        return cardViewUI.CardInstance.data.targetType;
    }

    private bool ShouldUseTargetArrowMode()
    {
        return GetCurrentTargetType() == TargetType.SingleEnemy;
    }

    private bool ShouldUseDirectDragMode()
    {
        TargetType targetType = GetCurrentTargetType();

        return targetType == TargetType.None ||
               targetType == TargetType.Self ||
               targetType == TargetType.RandomEnemy ||
               targetType == TargetType.AllEnemies ||
               targetType == TargetType.AllCharacters;
    }

    private void UpdateDirectDragPosition(Vector2 currentScreenPosition)
    {
        if (rectTransform == null)
            return;

        float scaleFactor = 1f;

        if (canvas != null)
            scaleFactor = Mathf.Max(0.01f, canvas.scaleFactor);

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

        if (handLayout != null && hoverUI != null)
        {
            handLayout.SetHover(hoverUI);
            handLayout.SetLockedCard(hoverUI);
            handLayout.RefreshLayout();
        }

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
            else
            {
                Debug.LogWarning("[CardDragUI] 按下卡牌時沒有 TargetArrowUI，所以箭頭無法顯示");
            }
        }
        else
        {
            if (targetArrow != null)
                targetArrow.Hide();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsDragging)
            return;

        if (useTargetArrowMode)
        {
            EnsureTargetArrow();

            if (targetArrow != null)
                targetArrow.UpdateArrow(eventData.position);

            return;
        }

        if (useDirectDragMode)
        {
            UpdateDirectDragPosition(eventData.position);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsDragging) return;

        IsDragging = false;
        canvasGroup.blocksRaycasts = true;

        EnsureTargetArrow();

        HideThresholdLine();

        if (targetArrow != null)
            targetArrow.Hide();

        bool played = false;

        bool draggedToPlayArea = IsDraggedToPlayArea(eventData.position);

        if (draggedToPlayArea && battleManager != null && cardViewUI != null && cardViewUI.CardInstance != null)
        {
            CardInstance card = cardViewUI.CardInstance;

            BattleTargetUI hoveredTarget = BattleTargetUI.CurrentHoveredTarget;
            BattleUnit targetUnit = hoveredTarget != null ? hoveredTarget.battleUnit : null;

            Debug.Log($"[Release] card = {card.data.cardName}, targetType = {card.data.targetType}, target = {(targetUnit != null ? targetUnit.name : "null")}");

            switch (card.data.targetType)
            {
                case TargetType.SingleEnemy:
                    if (targetUnit != null)
                    {
                        played = battleManager.TryPlayCard(card, targetUnit, cardViewUI);
                    }
                    else
                    {
                        Debug.Log("攻擊牌必須拖到敵人身上才會打出");
                        played = false;
                    }
                    break;

                case TargetType.None:
                case TargetType.Self:
                case TargetType.RandomEnemy:
                case TargetType.AllEnemies:
                case TargetType.AllCharacters:
                    played = battleManager.TryPlayCard(card, null, cardViewUI);
                    break;

                default:
                    Debug.LogWarning($"尚未支援的 TargetType: {card.data.targetType}");
                    played = false;
                    break;
            }
        }
        else
        {
            Debug.Log("沒有拖出手牌區，不出牌");
        }

        if (handLayout != null)
            handLayout.ClearAllSelection();

        if (!played)
        {
            ReturnToHand();
        }
    }

    private bool IsDraggedToPlayArea(Vector2 pointerUpScreenPos)
    {
        float deltaY = pointerUpScreenPos.y - pointerDownScreenPos.y;
        return deltaY >= playThresholdPixels;
    }

    private void ReturnToHand()
    {
        if (handLayout != null)
        {
            handLayout.RefreshLayout();
        }
        else
        {
            rectTransform.anchoredPosition = startAnchoredPosition;
        }
    }
    private void ShowThresholdLine()
    {
        if (!showPlayThresholdLine)
            return;

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        RectTransform parentRect = rectTransform.parent as RectTransform;

        if (parentRect == null)
            return;

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
        if (thresholdLineObject != null)
        {
            thresholdLineObject.SetActive(false);
        }
    }
}