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

    [Header("Title")]
    public TMP_Text titleText;
    public TMP_Text countText;

    [Header("Buttons")]
    public Button drawPileButton;
    public Button discardPileButton;
    public Button exhaustPileButton;
    public Button handButton;
    public Button closeButton;

    [Header("Card Size In Viewer")]
    public bool overrideCardSize = false;
    public Vector2 viewerCardSize = new Vector2(180f, 260f);
    public Vector3 viewerCardScale = Vector3.one;

    private DeckViewMode currentMode = DeckViewMode.DrawPile;
    private readonly List<CardViewUI> spawnedCards = new();

    private void Awake()
    {
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

        ClearCards();

        IReadOnlyList<CardInstance> cards = GetCurrentCards();

        if (cards == null)
        {
            Debug.LogWarning($"[DeckViewerUI] {currentMode} cards 是 null");
            return;
        }

        Debug.Log($"[DeckViewerUI] Refresh {currentMode}, count = {cards.Count}");

        UpdateTitle(cards.Count);

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

    private IReadOnlyList<CardInstance> GetCurrentCards()
    {
        switch (currentMode)
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

    private void UpdateTitle(int count)
    {
        if (titleText != null)
            titleText.text = GetTitleText(currentMode);

        if (countText != null)
            countText.text = count.ToString();
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