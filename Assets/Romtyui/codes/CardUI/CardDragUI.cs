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
         * 重新判斷目前這張牌要使用哪一種出牌方式。
         */
        useTargetArrowMode =
            ShouldUseTargetArrowMode();

        useDirectDragMode =
            ShouldUseDirectDragMode();

        dragSignalSent = false;

        pointerDownScreenPos =
            eventData.position;

        startAnchoredPosition =
            rectTransform.anchoredPosition;

        /*
         * 只有直接拖牌模式才需要顯示出牌線。
         *
         * SingleEnemy 使用箭頭，
         * 不需要出牌線。
         */
        if (useDirectDragMode)
        {
            ShowThresholdLine();
        }
        else
        {
            HideThresholdLine();
        }

        /*
         * 原本 HandFanLayout 功能保留。
         */
        if (handLayout != null &&
            hoverUI != null)
        {
            handLayout.SetHover(hoverUI);
            handLayout.SetLockedCard(hoverUI);
            handLayout.RefreshLayout();
        }

        rectTransform.SetAsLastSibling();

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }

        EnsureTargetArrow();

        /*
         * SingleEnemy：
         * 顯示箭頭。
         */
        if (useTargetArrowMode)
        {
            if (targetArrow != null)
            {
                targetArrow.Show(rectTransform);

                targetArrow.UpdateArrow(
                    eventData.position
                );
            }
        }
        else
        {
            /*
             * 其他牌不需要箭頭。
             */
            if (targetArrow != null)
            {
                targetArrow.Hide();
            }
        }

        /*
         * 原本 Tutorial 功能保留。
         */
        TutorialEventBus.Raise(
            "Battle_CardGrabStarted"
        );
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsDragging)
            return;

        /*
         * =====================================================
         * SingleEnemy
         *
         * 卡牌留在原本的位置，
         * TargetArrow 跟著滑鼠。
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
         * Direct Drag
         *
         * Self
         * None
         * RandomEnemy
         * AllEnemies
         * AllCharacters
         *
         * 卡牌本體跟著滑鼠。
         * =====================================================
         */
        if (useDirectDragMode)
        {
            /*
             * 原本的新手教學 Signal 保留。
             */
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

        /*
         * Direct Drag 才需要判斷
         * 有沒有拖過出牌線。
         */
        bool draggedToPlayArea =
            IsDraggedToPlayArea(
                eventData.position
            );

        /*
         * SingleEnemy：
         * 不需要經過出牌線。
         *
         * DirectDrag：
         * 必須經過出牌線。
         */
        bool canAttemptPlay =
            useTargetArrowMode ||
            (
                useDirectDragMode &&
                draggedToPlayArea
            );

        if (canAttemptPlay &&
            battleManager != null &&
            cardViewUI != null &&
            cardViewUI.CardInstance != null)
        {
            CardInstance card =
                cardViewUI.CardInstance;

            BattleTargetUI hoveredTarget =
                BattleTargetUI.CurrentHoveredTarget;

            BattleUnit targetUnit =
                hoveredTarget != null
                    ? hoveredTarget.battleUnit
                    : null;

            string targetName =
                targetUnit != null
                    ? targetUnit.name
                    : "null";

            Debug.Log(
                $"[Release] card = {card.data.cardName}, " +
                $"targetType = {card.data.targetType}, " +
                $"target = {targetName}, " +
                $"ArrowMode = {useTargetArrowMode}, " +
                $"DirectDragMode = {useDirectDragMode}, " +
                $"DraggedToPlayArea = {draggedToPlayArea}"
            );

            switch (card.data.targetType)
            {
                /*
                 * =================================================
                 * 單體敵人
                 * =================================================
                 */
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
                            "單體牌必須指定敵人才會打出"
                        );

                        played = false;
                    }

                    break;


                /*
                 * =================================================
                 * 全體敵人
                 * =================================================
                 */
                case TargetType.AllEnemies:

                    played =
                        battleManager.TryPlayCard(
                            card,
                            null,
                            cardViewUI
                        );

                    break;


                /*
                 * =================================================
                 * 自己
                 * =================================================
                 */
                case TargetType.Self:

                    played =
                        battleManager.TryPlayCard(
                            card,
                            null,
                            cardViewUI
                        );

                    break;


                /*
                 * =================================================
                 * 無指定目標
                 * =================================================
                 */
                case TargetType.None:

                    played =
                        battleManager.TryPlayCard(
                            card,
                            null,
                            cardViewUI
                        );

                    break;


                /*
                 * =================================================
                 * 隨機敵人
                 * =================================================
                 */
                case TargetType.RandomEnemy:

                    played =
                        battleManager.TryPlayCard(
                            card,
                            null,
                            cardViewUI
                        );

                    break;


                /*
                 * AllCharacters 目前 BattleManager
                 * 還沒有真正實作完整的全角色結算。
                 *
                 * 所以這次先維持不支援，
                 * 不偷偷改你的卡牌效果。
                 */
                case TargetType.AllCharacters:

                    Debug.LogWarning(
                        "AllCharacters 目前尚未完成出牌結算邏輯"
                    );

                    played = false;

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
            if (useDirectDragMode &&
                !draggedToPlayArea)
            {
                Debug.Log(
                    "卡牌沒有拖過出牌線，不出牌"
                );
            }
            else
            {
                Debug.Log(
                    "目前無法嘗試出牌"
                );
            }
        }

        /*
         * 原本 HandLayout 功能保留。
         */
        if (handLayout != null)
        {
            handLayout.ClearAllSelection();
        }

        /*
         * 出牌失敗：
         * 保留原本 Tutorial Invalid Signal。
         */
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

        /*
         * 這次拖牌結束，
         * 清除 Runtime 模式。
         */
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