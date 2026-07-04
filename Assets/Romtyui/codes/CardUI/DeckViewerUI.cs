using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static BattleDeck;

public class DeckViewerUI : MonoBehaviour
{
    [Header("Refs")]
    public BattleDeck battleDeck;
    public GameObject panelRoot;
    public RectTransform contentRoot;
    public CardViewUI cardPrefab;

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

    [Header("Button Text")]
    public TMP_Text drawPileButtonText;
    public TMP_Text discardPileButtonText;
    public TMP_Text exhaustPileButtonText;
    public TMP_Text handButtonText;

    [Header("Button Image")]
    public Image drawPileButtonImage;
    public Image discardPileButtonImage;
    public Image exhaustPileButtonImage;
    public Image handButtonImage;

    [Header("Tab Text Label")]
    public string drawPileLabel = "牌組區";
    public string discardPileLabel = "棄牌區";
    public string exhaustPileLabel = "消耗區";
    public string handLabel = "手牌區";

    [Header("Tab Colors")]
    public Color activeTabColor = Color.white;
    public Color inactiveTabColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    public Color activeTextColor = Color.black;
    public Color inactiveTextColor = new Color(0.18f, 0.18f, 0.18f, 1f);

    [Header("Card Size In Viewer")]
    public bool overrideCardSize = false;
    public Vector2 viewerCardSize = new Vector2(180f, 260f);
    public Vector3 viewerCardScale = Vector3.one;

    private DeckViewMode currentMode = DeckViewMode.DrawPile;
    private readonly List<CardViewUI> spawnedCards = new();

    private void Awake()
    {
        AutoBindButtonRefs();

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

        if (panelRoot != null)
            panelRoot.SetActive(false);

        RefreshTabUI();
        UpdateCenterTitle();
    }

    private void AutoBindButtonRefs()
    {
        if (drawPileButton != null)
        {
            if (drawPileButtonImage == null)
                drawPileButtonImage = drawPileButton.image;

            if (drawPileButtonText == null)
                drawPileButtonText = drawPileButton.GetComponentInChildren<TMP_Text>(true);
        }

        if (discardPileButton != null)
        {
            if (discardPileButtonImage == null)
                discardPileButtonImage = discardPileButton.image;

            if (discardPileButtonText == null)
                discardPileButtonText = discardPileButton.GetComponentInChildren<TMP_Text>(true);
        }

        if (exhaustPileButton != null)
        {
            if (exhaustPileButtonImage == null)
                exhaustPileButtonImage = exhaustPileButton.image;

            if (exhaustPileButtonText == null)
                exhaustPileButtonText = exhaustPileButton.GetComponentInChildren<TMP_Text>(true);
        }

        if (handButton != null)
        {
            if (handButtonImage == null)
                handButtonImage = handButton.image;

            if (handButtonText == null)
                handButtonText = handButton.GetComponentInChildren<TMP_Text>(true);
        }
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

        if (panelRoot != null)
            panelRoot.SetActive(true);

        Debug.Log($"[DeckViewerUI] Open {currentMode}");

        Refresh();
    }

    public void Close()
    {
        ClearCards();

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void Refresh()
    {
        if (battleDeck == null)
        {
            Debug.LogWarning("[DeckViewerUI] battleDeck 沒有指定");
            return;
        }

        if (cardPrefab == null)
        {
            Debug.LogWarning("[DeckViewerUI] cardPrefab 沒有指定");
            return;
        }

        if (contentRoot == null)
        {
            Debug.LogWarning("[DeckViewerUI] contentRoot 沒有指定");
            return;
        }

        RefreshTabUI();
        UpdateCenterTitle();

        ClearCards();

        IReadOnlyList<CardInstance> cards = GetCurrentCards();

        if (cards == null)
        {
            Debug.LogWarning($"[DeckViewerUI] {currentMode} cards 是 null");
            return;
        }

        Debug.Log($"[DeckViewerUI] Refresh {currentMode}, count = {cards.Count}");

        for (int i = 0; i < cards.Count; i++)
        {
            CardInstance card = cards[i];

            if (card == null || card.data == null)
            {
                Debug.LogWarning($"[DeckViewerUI] 第 {i} 張卡是 null");
                continue;
            }

            CardViewUI view = Instantiate(cardPrefab, contentRoot);
            view.SetTooltipSide(TooltipAnchorSide.Left);
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

            CardDragUI drag = view.GetComponent<CardDragUI>();

            if (drag != null)
                drag.enabled = false;

            CardHoverUI hover = view.GetComponent<CardHoverUI>();

            if (hover != null)
                hover.enabled = false;

            spawnedCards.Add(view);

            Debug.Log($"[DeckViewerUI] 生成卡牌 UI：{card.data.cardName}");
        }
    }

    private void RefreshTabUI()
    {
        int drawCount = GetPileCount(DeckViewMode.DrawPile);
        int discardCount = GetPileCount(DeckViewMode.DiscardPile);
        int exhaustCount = GetPileCount(DeckViewMode.ExhaustPile);
        int handCount = GetPileCount(DeckViewMode.Hand);

        if (drawPileButtonText != null)
            drawPileButtonText.text = $"{drawPileLabel} ({drawCount})";

        if (discardPileButtonText != null)
            discardPileButtonText.text = $"{discardPileLabel} ({discardCount})";

        if (exhaustPileButtonText != null)
            exhaustPileButtonText.text = $"{exhaustPileLabel} ({exhaustCount})";

        if (handButtonText != null)
            handButtonText.text = $"{handLabel} ({handCount})";

        SetTabVisual(
            DeckViewMode.DrawPile,
            drawPileButtonImage,
            drawPileButtonText
        );

        SetTabVisual(
            DeckViewMode.DiscardPile,
            discardPileButtonImage,
            discardPileButtonText
        );

        SetTabVisual(
            DeckViewMode.ExhaustPile,
            exhaustPileButtonImage,
            exhaustPileButtonText
        );

        SetTabVisual(
            DeckViewMode.Hand,
            handButtonImage,
            handButtonText
        );
    }

    private void SetTabVisual(DeckViewMode mode, Image buttonImage, TMP_Text buttonText)
    {
        bool isActive = currentMode == mode;

        if (buttonImage != null)
            buttonImage.color = isActive ? activeTabColor : inactiveTabColor;

        if (buttonText != null)
            buttonText.color = isActive ? activeTextColor : inactiveTextColor;
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
}