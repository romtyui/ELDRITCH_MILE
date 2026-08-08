using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

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
    [SerializeField] private TargetArrowUI targetArrow;
    private CanvasGroup canvasGroup;
    private CardViewUI cardViewUI;
    private BattleManager battleManager;
    private Canvas canvas;

    private Vector2 pointerDownScreenPos;
    private Vector2 startAnchoredPosition;

    private bool tutorialGrabStarted;


    [SerializeField] private bool useTargetArrowMode;
    private bool useDirectDragMode;

    [Header("Play Threshold Debug")]
    public float playThresholdY = 180f;
    public bool showPlayThresholdLine = true;
    public Color thresholdLineColor = new Color(1f, 0.2f, 0.2f, 0.8f);
    public float thresholdLineHeight = 4f;

    private GameObject thresholdLineObject;
    private RectTransform thresholdLineRect;
    private bool dragSignalSent;

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
        if (IsDragging)
            return;

        IsDragging = true;
        tutorialGrabStarted = true;

        /*
         * 每次開始拖牌時，
         * 根據目前這張卡的 TargetType 決定操作模式。
         */
        useTargetArrowMode =
            ShouldUseTargetArrowMode();

        useDirectDragMode =
            ShouldUseDirectDragMode();

        /*
         * 每次重新拖牌都要重設。
         */
        dragSignalSent = false;

        pointerDownScreenPos = eventData.position;
        startAnchoredPosition = rectTransform.anchoredPosition;

        /*
         * 只有直接拖曳型卡牌才顯示出牌門檻線。
         */
        if (useDirectDragMode)
        {
            ShowThresholdLine();
        }
        else
        {
            HideThresholdLine();
        }

        if (handLayout != null &&
            hoverUI != null)
        {
            handLayout.SetHover(hoverUI);
            handLayout.SetLockedCard(hoverUI);
            handLayout.RefreshLayout();
        }

        rectTransform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;

        EnsureTargetArrow();

        /*
         * 只有 SingleEnemy 才顯示 TargetArrow。
         */
        if (useTargetArrowMode)
        {
            if (targetArrow != null)
            {
                targetArrow.Show(
                    rectTransform
                );

                targetArrow.UpdateArrow(
                    eventData.position
                );
            }
        }
        else
        {
            /*
             * 防止上一張 SingleEnemy 卡留下箭頭。
             */
            if (targetArrow != null)
            {
                targetArrow.Hide();
            }
        }
        TutorialEventBus.Raise("Battle_CardGrabStarted");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsDragging)
            return;

        /*
         * =====================================================
         * 模式 1：
         * SingleEnemy
         *
         * 卡牌不移動，
         * 只有 TargetArrow 跟著滑鼠。
         * =====================================================
         */
        if (useTargetArrowMode)
        {
            EnsureTargetArrow();

            if (targetArrow != null)
            {
                targetArrow.UpdateArrow(
                    eventData.position
                );
            }

            return;
        }

        /*
         * =====================================================
         * 模式 2：
         * 不需要手動指定單體敵人的牌
         *
         * 卡牌本體直接跟隨滑鼠。
         * =====================================================
         */
        if (useDirectDragMode)
        {
            if (!dragSignalSent)
            {
                dragSignalSent = true;

                TutorialEventBus.Raise(
                    BattleTutorialSignals.CardDragStarted
                );
            }

            UpdateDirectDragPosition(
                eventData.position
            );
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsDragging)
            return;

        IsDragging = false;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        EnsureTargetArrow();

        HideThresholdLine();

        if (targetArrow != null)
        {
            targetArrow.Hide();
        }

        bool played = false;

        bool draggedToPlayArea =
            IsDraggedToPlayArea(
                eventData.position
            );

        /*
         * TargetArrow 模式：
         * 不需要經過出牌門檻，
         * 只需要最後有有效敵人。
         *
         * DirectDrag 模式：
         * 必須拖過出牌門檻。
         */
        bool canAttemptPlay = useTargetArrowMode || (useDirectDragMode && draggedToPlayArea);

        if (canAttemptPlay &&  battleManager != null && cardViewUI != null && cardViewUI.CardInstance != null)
        {
            CardInstance card = cardViewUI.CardInstance;

            BattleTargetUI hoveredTarget = BattleTargetUI.CurrentHoveredTarget;

            BattleUnit targetUnit = hoveredTarget != null? hoveredTarget.battleUnit: null;

            string targetName =targetUnit != null? targetUnit.name : "null";

            Debug.Log(
                $"[Release] card = {card.data.cardName}, " +
                $"targetType = {card.data.targetType}, " +
                $"target = {targetName}"
            );

            switch (card.data.targetType)
            {
                case TargetType.SingleEnemy:
                    if (targetUnit != null)
                    {
                        played =
                            battleManager.TryPlayCard(
                                card,
                                targetUnit,
                                cardViewUI
                            );
                    }
                    else
                    {
                        Debug.Log(
                            "攻擊牌必須拖到敵人身上才會打出"
                        );

                        played = false;
                    }
                    break;

                case TargetType.AllEnemies:
                    played =
                        battleManager.TryPlayCard(
                            card,
                            null,
                            cardViewUI
                        );
                    break;

                case TargetType.Self:
                case TargetType.None:
                    played =
                        battleManager.TryPlayCard(
                            card,
                            null,
                            cardViewUI
                        );
                    break;

                case TargetType.RandomEnemy:
                    played =
                        battleManager.TryPlayCard(
                            card,
                            null,
                            cardViewUI
                        );
                    break;

                default:
                    Debug.LogWarning(
                        $"尚未支援的 TargetType: " +
                        $"{card.data.targetType}"
                    );

                    played = false;
                    break;
            }
        }
        else
        {
            if (useDirectDragMode && !draggedToPlayArea)
            {
                Debug.Log("卡牌沒有拖過出牌範圍，不出牌");
            }
            else
            {
                Debug.Log(
                    "目前無法嘗試出牌"
                );
            }
        }

        if (handLayout != null)
        {
            handLayout.ClearAllSelection();
        }

        if (!played)
        {
            if (tutorialGrabStarted)
            {
                TutorialEventBus.Raise(
                    "Battle_CardPlayInvalid"
                );
            }

            ReturnToHand();
        }

        tutorialGrabStarted = false;

        useTargetArrowMode = false;
        useDirectDragMode = false;
        dragSignalSent = false;
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