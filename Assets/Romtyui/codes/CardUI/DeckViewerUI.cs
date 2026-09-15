using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static BattleDeck;

public enum DeckViewerDisplayMode
{
    ClassicDragScroll,
    BookPaged
}

public class DeckViewerUI : MonoBehaviour
{
    [Header("Refs")]
    public BattleDeck battleDeck;
    public GameObject panelRoot;

    [Header("Display Mode")]
    [Tooltip("ClassicDragScroll = 原版全部生成到 ContentRoot。BookPaged = 新版書本固定 8 格分頁")]
    public DeckViewerDisplayMode displayMode = DeckViewerDisplayMode.ClassicDragScroll;

    [Header("Classic Drag Scroll Mode")]
    [Tooltip("原版顯示模式使用。ClassicDragScroll 會把所有卡牌生成到這裡")]
    public RectTransform contentRoot;

    [Tooltip("原版顯示模式使用的卡牌 Prefab")]
    public CardViewUI cardPrefab;

    [Tooltip("原版列表 Root。使用 BookPaged 時會自動關閉。可以不指定，預設會用 contentRoot")]
    public GameObject classicRoot;

    [Header("Book Paged Mode")]
    [Tooltip("新版書本模式 Root。使用 ClassicDragScroll 時會自動關閉")]
    public GameObject bookRoot;

    [Tooltip("新版一頁顯示幾張。你目前圖上是 8 張")]
    public int cardsPerPage = 8;

    [Tooltip("新版固定卡牌位置。請拖 8 個已經擺好位置的 CardViewUI")]
    public List<CardViewUI> bookCardSlots = new List<CardViewUI>();

    [Tooltip("上一頁按鈕。如果在第一頁按，會跳到最後一頁")]
    public Button previousPageButton;

    [Tooltip("下一頁按鈕。如果在最後一頁按，會跳到第一頁")]
    public Button nextPageButton;

    [Tooltip("頁數文字，例如 1 / 3。可不指定")]
    public TMP_Text pageText;

    [Tooltip("如果目前分類沒有牌，要顯示的物件。可不指定")]
    public GameObject emptyMessageRoot;

    [Tooltip("舊設定。目前 BookPaged 模式下翻頁按鈕會固定顯示，此欄位暫時保留")]
    public bool hidePageButtonsWhenSinglePage = true;

    [Header("Page Button Visual")]
    [Tooltip("上一頁按鈕圖片。沒指定時會自動使用 Button.image")]
    public Image previousPageButtonImage;

    [Tooltip("下一頁按鈕圖片。沒指定時會自動使用 Button.image")]
    public Image nextPageButtonImage;

    [Tooltip("有兩頁以上時的分頁按鈕顏色")]
    public Color pageButtonActiveColor = Color.white;

    [Tooltip("只有一頁或沒有牌時的分頁按鈕顏色")]
    public Color pageButtonInactiveColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    [Tooltip("切換分類時是否回到第一頁")]
    public bool resetPageWhenSwitchTab = true;

    [Header("Center Title")]
    [Tooltip("如果不想顯示中間標題，就關掉")]
    public bool showCenterTitle = false;

    public TMP_Text titleText;
    public TMP_Text countText;

    [Header("Buttons")]
    public Button drawPileButton;
    public Button discardPileButton;
    public Button exhaustPileButton;
    public Button handButton;
    public Button closeButton;

    [Header("Tab Button Animation")]
    [Tooltip("目前選中的按鈕 Y 位移量。正數往上，負數往下")]
    public float activeTabYOffset = 15f;

    [Tooltip("頁籤按鈕移動動畫時間")]
    public float tabMoveDuration = 0.15f;

    [Tooltip("是否使用 Unscaled Time，建議開啟")]
    public bool tabAnimationUseUnscaledTime = true;

    private Vector2 drawPileButtonBasePosition;
    private Vector2 discardPileButtonBasePosition;
    private Vector2 exhaustPileButtonBasePosition;
    private Vector2 handButtonBasePosition;

    private Coroutine drawPileButtonMoveCoroutine;
    private Coroutine discardPileButtonMoveCoroutine;
    private Coroutine exhaustPileButtonMoveCoroutine;
    private Coroutine handButtonMoveCoroutine;

    [Header("Button Text")]
    [Tooltip("抽牌堆標籤名稱文字")]
    public TMP_Text drawPileButtonText;

    [Tooltip("棄牌堆標籤名稱文字")]
    public TMP_Text discardPileButtonText;

    [Tooltip("消耗區標籤名稱文字")]
    public TMP_Text exhaustPileButtonText;

    [Tooltip("手牌區標籤名稱文字")]
    public TMP_Text handButtonText;

    [Header("Button Count Text")]
    [Tooltip("抽牌堆目前數量文字")]
    public TMP_Text drawPileButtonCountText;

    [Tooltip("棄牌堆目前數量文字")]
    public TMP_Text discardPileButtonCountText;

    [Tooltip("消耗區目前數量文字")]
    public TMP_Text exhaustPileButtonCountText;

    [Tooltip("手牌目前數量文字")]
    public TMP_Text handButtonCountText;

    [Header("Tab Text Label")]
    public string drawPileLabel = "牌組區";
    public string discardPileLabel = "棄牌區";
    public string exhaustPileLabel = "消耗區";
    public string handLabel = "手牌區";

    [Header("Card Size In Viewer")]
    public bool overrideCardSize = false;
    public Vector2 viewerCardSize = new Vector2(180f, 260f);
    public Vector3 viewerCardScale = Vector3.one;

    [Header("Viewer Interaction")]
    [Tooltip("查看視窗中的卡牌是否禁用出牌拖曳。建議打開")]
    public bool disableDragInViewer = true;

    [Tooltip("查看視窗中的卡牌是否禁用 Hover 放大。建議打開")]
    public bool disableHoverInViewer = true;

    [Tooltip("查看視窗中的卡牌 Tooltip 顯示方向")]
    public TooltipAnchorSide viewerCardTooltipSide = TooltipAnchorSide.Left;

    [Header("Runtime")]
    [SerializeField] private DeckViewMode currentMode = DeckViewMode.DrawPile;
    [SerializeField] private int currentPageIndex = 0;
    [SerializeField] private int totalPageCount = 1;

    private readonly List<CardViewUI> spawnedCards = new();

    private void Awake()
    {
        AutoBindButtonRefs();
        CacheTabButtonBasePositions();

        if (drawPileButton != null)
            drawPileButton.onClick.AddListener(OpenDrawPile);

        if (discardPileButton != null)
            discardPileButton.onClick.AddListener(OpenDiscardPile);

        if (exhaustPileButton != null)
            exhaustPileButton.onClick.AddListener(OpenExhaustPile);

        if (handButton != null)
            handButton.onClick.AddListener(OpenHand);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (previousPageButton != null)
            previousPageButton.onClick.AddListener(PreviousPage);

        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(NextPage);

        if (panelRoot != null)
            panelRoot.SetActive(false);

        RefreshTabUI();
        UpdateCenterTitle();
        RefreshModeRootVisibility();
        ClearBookSlots();
    }

    private void CacheTabButtonBasePositions()
    {
        if (drawPileButton != null)
            drawPileButtonBasePosition =
                ((RectTransform)drawPileButton.transform).anchoredPosition;

        if (discardPileButton != null)
            discardPileButtonBasePosition =
                ((RectTransform)discardPileButton.transform).anchoredPosition;

        if (exhaustPileButton != null)
            exhaustPileButtonBasePosition =
                ((RectTransform)exhaustPileButton.transform).anchoredPosition;

        if (handButton != null)
            handButtonBasePosition =
                ((RectTransform)handButton.transform).anchoredPosition;
    }

    private void AutoBindButtonRefs()
    {
        if (previousPageButton != null && previousPageButtonImage == null)
            previousPageButtonImage = previousPageButton.image;

        if (nextPageButton != null && nextPageButtonImage == null)
            nextPageButtonImage = nextPageButton.image;
    }

    [ContextMenu("Test Open Draw Pile")]
    public void TestOpenDrawPile()
    {
        OpenDrawPile();
    }

    [ContextMenu("Test Open Hand")]
    public void TestOpenHand()
    {
        OpenHand();
    }

    [ContextMenu("Test Open Discard Pile")]
    public void TestOpenDiscardPile()
    {
        OpenDiscardPile();
    }

    public void OpenDrawPile()
    {
        Open(DeckViewMode.DrawPile);
    }

    public void OpenDiscardPile()
    {
        Open(DeckViewMode.DiscardPile);
    }

    public void OpenExhaustPile()
    {
        Open(DeckViewMode.ExhaustPile);
    }

    public void OpenHand()
    {
        Open(DeckViewMode.Hand);
    }

    public void Open(DeckViewMode mode)
    {
        currentMode = mode;

        if (resetPageWhenSwitchTab)
            currentPageIndex = 0;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        Debug.Log($"[DeckViewerUI] Open {currentMode}");

        Refresh();
    }

    public void Close()
    {
        ClearCards();
        ClearBookSlots();

        if (emptyMessageRoot != null)
            emptyMessageRoot.SetActive(false);

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void SetDisplayMode(DeckViewerDisplayMode mode)
    {
        displayMode = mode;
        currentPageIndex = 0;
        Refresh();
    }

    public void NextPage()
    {
        if (displayMode != DeckViewerDisplayMode.BookPaged)
            return;

        IReadOnlyList<CardInstance> cards = GetCurrentCards();
        int pageCount = CalculatePageCount(cards);

        if (pageCount <= 1)
            return;

        currentPageIndex++;

        if (currentPageIndex >= pageCount)
            currentPageIndex = 0;

        Refresh();
    }

    public void PreviousPage()
    {
        if (displayMode != DeckViewerDisplayMode.BookPaged)
            return;

        IReadOnlyList<CardInstance> cards = GetCurrentCards();
        int pageCount = CalculatePageCount(cards);

        if (pageCount <= 1)
            return;

        currentPageIndex--;

        if (currentPageIndex < 0)
            currentPageIndex = pageCount - 1;

        Refresh();
    }

    public void Refresh()
    {
        if (battleDeck == null)
        {
            Debug.LogWarning("[DeckViewerUI] battleDeck 沒有指定");
            return;
        }

        if (cardPrefab == null && displayMode == DeckViewerDisplayMode.ClassicDragScroll)
        {
            Debug.LogWarning("[DeckViewerUI] cardPrefab 沒有指定，ClassicDragScroll 無法生成卡牌");
            return;
        }

        if (contentRoot == null && displayMode == DeckViewerDisplayMode.ClassicDragScroll)
        {
            Debug.LogWarning("[DeckViewerUI] contentRoot 沒有指定，ClassicDragScroll 無法生成卡牌");
            return;
        }

        RefreshModeRootVisibility();
        RefreshTabUI();
        UpdateCenterTitle();

        IReadOnlyList<CardInstance> cards = GetCurrentCards();

        if (cards == null)
        {
            Debug.LogWarning($"[DeckViewerUI] {currentMode} cards 是 null");
            return;
        }

        totalPageCount = Mathf.Max(1, CalculatePageCount(cards));
        currentPageIndex = Mathf.Clamp(currentPageIndex, 0, totalPageCount - 1);

        Debug.Log(
            $"[DeckViewerUI] Refresh {currentMode}, count = {cards.Count}, mode = {displayMode}"
        );

        switch (displayMode)
        {
            case DeckViewerDisplayMode.ClassicDragScroll:
                RefreshClassicDragScroll(cards);
                break;

            case DeckViewerDisplayMode.BookPaged:
                RefreshBookPaged(cards);
                break;
        }

        RefreshPageUI(cards);
    }

    private void RefreshClassicDragScroll(IReadOnlyList<CardInstance> cards)
    {
        ClearBookSlots();
        ClearCards();

        if (cards == null)
            return;

        if (cardPrefab == null)
            return;

        if (contentRoot == null)
            return;

        for (int i = 0; i < cards.Count; i++)
        {
            CardInstance card = cards[i];

            if (card == null || card.data == null)
            {
                Debug.LogWarning($"[DeckViewerUI] 第 {i} 張卡是 null");
                continue;
            }

            CardViewUI view = Instantiate(cardPrefab, contentRoot);

            SetupViewerCard(view, card);

            spawnedCards.Add(view);

            Debug.Log($"[DeckViewerUI] 生成卡牌 UI：{card.data.cardName}");
        }

        if (emptyMessageRoot != null)
            emptyMessageRoot.SetActive(cards.Count == 0);
    }

    private void RefreshBookPaged(IReadOnlyList<CardInstance> cards)
    {
        ClearCards();
        ClearBookSlots();

        if (cards == null)
            return;

        if (bookCardSlots == null || bookCardSlots.Count == 0)
        {
            Debug.LogWarning("[DeckViewerUI] bookCardSlots 沒有指定，BookPaged 無法顯示卡牌");
            return;
        }

        int safeCardsPerPage = Mathf.Max(1, cardsPerPage);
        int startIndex = currentPageIndex * safeCardsPerPage;

        for (int i = 0; i < bookCardSlots.Count; i++)
        {
            CardViewUI slot = bookCardSlots[i];

            if (slot == null)
                continue;

            int cardIndex = startIndex + i;

            bool hasCard =
                i < safeCardsPerPage &&
                cardIndex >= 0 &&
                cardIndex < cards.Count &&
                cards[cardIndex] != null &&
                cards[cardIndex].data != null;

            if (!hasCard)
            {
                slot.gameObject.SetActive(false);
                continue;
            }

            SetupViewerCard(slot, cards[cardIndex]);

            Debug.Log(
                $"[DeckViewerUI] 書本頁 Slot {i} 顯示：{cards[cardIndex].data.cardName}"
            );
        }

        if (emptyMessageRoot != null)
            emptyMessageRoot.SetActive(cards.Count == 0);
    }

    private void SetupViewerCard(CardViewUI view, CardInstance card)
    {
        if (view == null)
            return;

        view.SetTooltipSide(viewerCardTooltipSide);
        view.gameObject.SetActive(true);
        view.Bind(card);

        RectTransform rect = view.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.localRotation = Quaternion.identity;

            if (overrideCardSize)
                rect.sizeDelta = viewerCardSize;

            rect.localScale = viewerCardScale;
        }

        if (disableDragInViewer)
        {
            CardDragUI drag = view.GetComponent<CardDragUI>();

            if (drag != null)
                drag.enabled = false;
        }

        if (disableHoverInViewer)
        {
            CardHoverUI hover = view.GetComponent<CardHoverUI>();

            if (hover != null)
                hover.enabled = false;
        }
    }

    private void RefreshModeRootVisibility()
    {
        if (classicRoot != null)
            classicRoot.SetActive(displayMode == DeckViewerDisplayMode.ClassicDragScroll);
        else if (contentRoot != null)
            contentRoot.gameObject.SetActive(displayMode == DeckViewerDisplayMode.ClassicDragScroll);

        if (bookRoot != null)
            bookRoot.SetActive(displayMode == DeckViewerDisplayMode.BookPaged);
    }

    private void RefreshPageUI(IReadOnlyList<CardInstance> cards)
    {
        int pageCount = CalculatePageCount(cards);

        bool hasMultiplePages = pageCount > 1;
        bool isBookPaged = displayMode == DeckViewerDisplayMode.BookPaged;

        // BookPaged 模式下翻頁按鈕永遠顯示。
        if (previousPageButton != null)
            previousPageButton.gameObject.SetActive(isBookPaged);

        if (nextPageButton != null)
            nextPageButton.gameObject.SetActive(isBookPaged);

        // 沒有第二頁以上時，不隱藏按鈕，只改成較暗的顏色。
        Color pageButtonColor =
            hasMultiplePages
                ? pageButtonActiveColor
                : pageButtonInactiveColor;

        if (previousPageButtonImage != null)
            previousPageButtonImage.color = pageButtonColor;

        if (nextPageButtonImage != null)
            nextPageButtonImage.color = pageButtonColor;

        if (pageText != null)
        {
            if (isBookPaged && cards != null && cards.Count > 0)
                pageText.text = $"{currentPageIndex + 1} / {Mathf.Max(1, pageCount)}";
            else
                pageText.text = "";
        }
    }

    private int CalculatePageCount(IReadOnlyList<CardInstance> cards)
    {
        if (cards == null || cards.Count == 0)
            return 0;

        int safeCardsPerPage = Mathf.Max(1, cardsPerPage);

        return Mathf.CeilToInt(cards.Count / (float)safeCardsPerPage);
    }

    private void RefreshTabUI()
    {
        int drawCount = GetPileCount(DeckViewMode.DrawPile);
        int discardCount = GetPileCount(DeckViewMode.DiscardPile);
        int exhaustCount = GetPileCount(DeckViewMode.ExhaustPile);
        int handCount = GetPileCount(DeckViewMode.Hand);

        // =====================================================
        // 標籤名稱
        // =====================================================

        if (drawPileButtonText != null)
            drawPileButtonText.text = drawPileLabel;

        if (discardPileButtonText != null)
            discardPileButtonText.text = discardPileLabel;

        if (exhaustPileButtonText != null)
            exhaustPileButtonText.text = exhaustPileLabel;

        if (handButtonText != null)
            handButtonText.text = handLabel;

        // =====================================================
        // 標籤目前數量
        // =====================================================

        if (drawPileButtonCountText != null)
            drawPileButtonCountText.text = drawCount.ToString();

        if (discardPileButtonCountText != null)
            discardPileButtonCountText.text = discardCount.ToString();

        if (exhaustPileButtonCountText != null)
            exhaustPileButtonCountText.text = exhaustCount.ToString();

        if (handButtonCountText != null)
            handButtonCountText.text = handCount.ToString();

        // =====================================================
        // 標籤只控制 Y 位移動畫。
        // 不修改 Image / Name Text / Count Text 顏色。
        // =====================================================

        SetTabVisual(
            DeckViewMode.DrawPile,
            drawPileButton
        );

        SetTabVisual(
            DeckViewMode.DiscardPile,
            discardPileButton
        );

        SetTabVisual(
            DeckViewMode.ExhaustPile,
            exhaustPileButton
        );

        SetTabVisual(
            DeckViewMode.Hand,
            handButton
        );
    }

    private void SetTabVisual(
        DeckViewMode mode,
        Button button
    )
    {
        bool isActive = currentMode == mode;

        AnimateTabButton(
            mode,
            button,
            isActive
        );
    }

    private void AnimateTabButton(
        DeckViewMode mode,
        Button button,
        bool isActive
    )
    {
        if (button == null)
            return;

        RectTransform rect = button.transform as RectTransform;

        if (rect == null)
            return;

        Vector2 basePosition = GetTabButtonBasePosition(mode);

        Vector2 targetPosition = basePosition;

        if (isActive)
            targetPosition.y += activeTabYOffset;

        Coroutine currentCoroutine = GetTabMoveCoroutine(mode);

        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        Coroutine newCoroutine = StartCoroutine(
            AnimateTabButtonRoutine(
                rect,
                targetPosition,
                tabMoveDuration
            )
        );

        SetTabMoveCoroutine(mode, newCoroutine);
    }

    private IEnumerator AnimateTabButtonRoutine(
        RectTransform rect,
        Vector2 targetPosition,
        float duration
    )
    {
        if (rect == null)
            yield break;

        Vector2 startPosition = rect.anchoredPosition;

        if (duration <= 0f)
        {
            rect.anchoredPosition = targetPosition;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float deltaTime =
                tabAnimationUseUnscaledTime
                    ? Time.unscaledDeltaTime
                    : Time.deltaTime;

            elapsed += deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            t = Mathf.SmoothStep(0f, 1f, t);

            rect.anchoredPosition = Vector2.Lerp(
                startPosition,
                targetPosition,
                t
            );

            yield return null;
        }

        rect.anchoredPosition = targetPosition;
    }

    private Vector2 GetTabButtonBasePosition(DeckViewMode mode)
    {
        switch (mode)
        {
            case DeckViewMode.DrawPile:
                return drawPileButtonBasePosition;

            case DeckViewMode.DiscardPile:
                return discardPileButtonBasePosition;

            case DeckViewMode.ExhaustPile:
                return exhaustPileButtonBasePosition;

            case DeckViewMode.Hand:
                return handButtonBasePosition;

            default:
                return Vector2.zero;
        }
    }

    private Coroutine GetTabMoveCoroutine(DeckViewMode mode)
    {
        switch (mode)
        {
            case DeckViewMode.DrawPile:
                return drawPileButtonMoveCoroutine;

            case DeckViewMode.DiscardPile:
                return discardPileButtonMoveCoroutine;

            case DeckViewMode.ExhaustPile:
                return exhaustPileButtonMoveCoroutine;

            case DeckViewMode.Hand:
                return handButtonMoveCoroutine;

            default:
                return null;
        }
    }

    private void SetTabMoveCoroutine(
        DeckViewMode mode,
        Coroutine coroutine
    )
    {
        switch (mode)
        {
            case DeckViewMode.DrawPile:
                drawPileButtonMoveCoroutine = coroutine;
                break;

            case DeckViewMode.DiscardPile:
                discardPileButtonMoveCoroutine = coroutine;
                break;

            case DeckViewMode.ExhaustPile:
                exhaustPileButtonMoveCoroutine = coroutine;
                break;

            case DeckViewMode.Hand:
                handButtonMoveCoroutine = coroutine;
                break;
        }
    }

    private int GetPileCount(DeckViewMode mode)
    {
        if (battleDeck == null)
            return 0;

        IReadOnlyList<CardInstance> cards = GetCardsByMode(mode);

        if (cards == null)
            return 0;

        return cards.Count;
    }

    private IReadOnlyList<CardInstance> GetCurrentCards()
    {
        return GetCardsByMode(currentMode);
    }

    private IReadOnlyList<CardInstance> GetCardsByMode(DeckViewMode mode)
    {
        if (battleDeck == null)
            return null;

        switch (mode)
        {
            case DeckViewMode.DrawPile:
                return battleDeck.DrawPile;

            case DeckViewMode.DiscardPile:
                return battleDeck.DiscardPile;

            case DeckViewMode.ExhaustPile:
                return battleDeck.ExhaustPile;

            case DeckViewMode.Hand:
                return battleDeck.Hand;

            default:
                return battleDeck.DrawPile;
        }
    }

    private void UpdateCenterTitle()
    {
        if (showCenterTitle)
        {
            if (titleText != null)
                titleText.text = GetTitleText(currentMode);

            if (countText != null)
                countText.text = GetPileCount(currentMode).ToString();
        }
        else
        {
            if (titleText != null)
                titleText.text = "";

            if (countText != null)
                countText.text = "";
        }
    }

    private string GetTitleText(DeckViewMode mode)
    {
        switch (mode)
        {
            case DeckViewMode.DrawPile:
                return "抽牌堆";

            case DeckViewMode.DiscardPile:
                return "棄牌區";

            case DeckViewMode.ExhaustPile:
                return "消耗區";

            case DeckViewMode.Hand:
                return "目前手牌";

            default:
                return "牌堆";
        }
    }

    private void ClearCards()
    {
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            if (spawnedCards[i] != null)
                Destroy(spawnedCards[i].gameObject);
        }

        spawnedCards.Clear();

        if (contentRoot != null)
        {
            for (int i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(contentRoot.GetChild(i).gameObject);
            }
        }
    }

    private void ClearBookSlots()
    {
        if (bookCardSlots == null)
            return;

        for (int i = 0; i < bookCardSlots.Count; i++)
        {
            CardViewUI slot = bookCardSlots[i];

            if (slot == null)
                continue;

            slot.gameObject.SetActive(false);
        }
    }
}