using System.Collections.Generic;
using UnityEngine;

public class BattleDeck : MonoBehaviour
{
    [Header("Starting Deck")]
    public List<CardData> startingDeck = new();

    private List<CardInstance> drawPile = new();
    private List<CardInstance> hand = new();
    private List<CardInstance> discardPile = new();
    private List<CardInstance> exhaustPile = new();

    public IReadOnlyList<CardInstance> Hand => hand;

    [Header("Debug View")]
    [SerializeField] private List<string> debugDrawPile = new();
    [SerializeField] private List<string> debugHand = new();
    [SerializeField] private List<string> debugDiscardPile = new();
    [SerializeField] private List<string> debugExhaustPile = new();

    public void InitializeDeck()
    {
        drawPile.Clear();
        hand.Clear();
        discardPile.Clear();
        exhaustPile.Clear();

        foreach (CardData card in startingDeck)
        {
            drawPile.Add(new CardInstance(card));
        }

        Shuffle(drawPile);
        RefreshDebugView();
    }

    public void DrawCards(int amount)
    {
        for (int i = 0; i < amount; i++)
            DrawOneCard();

        RefreshDebugView();
    }

    public CardInstance DrawOneCard()
    {
        if (drawPile == null)
            drawPile = new List<CardInstance>();

        if (discardPile == null)
            discardPile = new List<CardInstance>();

        // 牌組空了，就把棄牌堆洗回牌組
        if (drawPile.Count == 0)
        {
            ReshuffleDiscardIntoDraw();
        }

        // 洗回後還是沒牌，代表抽牌堆和棄牌堆都空
        if (drawPile.Count == 0)
        {
            Debug.Log("[BattleDeck] 抽牌堆和棄牌堆都沒有牌，無法抽牌");
            RefreshDebugView();
            return null;
        }

        CardInstance top = drawPile[0];
        drawPile.RemoveAt(0);
        hand.Add(top);

        RefreshDebugView();

        return top;
    }

    public void OnCardPlayed(CardInstance card)
    {
        if (card == null)
            return;

        if (hand.Remove(card))
        {
            if (card.data != null && card.data.exhaust)
                exhaustPile.Add(card);
            else
                discardPile.Add(card);
        }

        RefreshDebugView();
    }

    public void DiscardHandAtEndTurn()
    {
        foreach (CardInstance card in hand)
        {
            if (card == null || card.data == null)
                continue;

            if (card.data.retain)
                continue;

            discardPile.Add(card);
        }

        hand.RemoveAll(card => card == null || card.data == null || !card.data.retain);

        RefreshDebugView();
    }

    public CardTransformResult TransformRandomCardInDrawPileByPool(CardTransformPoolData transformPool)
    {
        if (transformPool == null)
        {
            Debug.LogWarning("[BattleDeck] transformPool 是 null，無法變換牌");
            return new CardTransformResult(false);
        }

        if (drawPile == null)
            drawPile = new List<CardInstance>();

        List<int> validIndexes = GetValidTransformIndexes(transformPool);

        if (validIndexes.Count == 0)
        {
            Debug.Log("[BattleDeck] 抽牌堆沒有可變化的牌，嘗試將棄牌堆洗回抽牌堆");

            ReshuffleDiscardIntoDraw();

            validIndexes = GetValidTransformIndexes(transformPool);
        }

        if (validIndexes.Count == 0)
        {
            Debug.Log($"[BattleDeck] 抽牌堆與棄牌堆都沒有可以被變化卡池 {transformPool.transformId} 變換的牌");
            RefreshDebugView();
            return new CardTransformResult(false);
        }

        int selectedListIndex = Random.Range(0, validIndexes.Count);
        int selectedDrawPileIndex = validIndexes[selectedListIndex];

        CardInstance oldCardInstance = drawPile[selectedDrawPileIndex];

        if (oldCardInstance == null || oldCardInstance.data == null)
        {
            Debug.LogWarning("[BattleDeck] 選到的牌資料是 null");
            RefreshDebugView();
            return new CardTransformResult(false);
        }

        CardData oldCardData = oldCardInstance.data;

        bool hasResult = transformPool.TryGetTransformResult(oldCardData, out CardData newCardData);

        if (!hasResult || newCardData == null)
        {
            Debug.LogWarning("[BattleDeck] 找到候選牌，但沒有取得變換結果");
            RefreshDebugView();
            return new CardTransformResult(false);
        }

        drawPile[selectedDrawPileIndex] = new CardInstance(newCardData);

        Debug.Log($"[BattleDeck] 變化卡池 {transformPool.transformId}：{oldCardData.cardName} → {newCardData.cardName}");

        RefreshDebugView();

        return new CardTransformResult(oldCardData, newCardData, transformPool.transformId);
    }

    private List<int> GetValidTransformIndexes(CardTransformPoolData transformPool)
    {
        List<int> validIndexes = new List<int>();

        if (drawPile == null || drawPile.Count == 0)
            return validIndexes;

        for (int i = 0; i < drawPile.Count; i++)
        {
            CardInstance cardInstance = drawPile[i];

            if (cardInstance == null || cardInstance.data == null)
                continue;

            if (transformPool.TryGetTransformResult(cardInstance.data, out CardData resultCard))
            {
                if (resultCard != null)
                    validIndexes.Add(i);
            }
        }

        return validIndexes;
    }

    private void ReshuffleDiscardIntoDraw()
    {
        drawPile.AddRange(discardPile);
        discardPile.Clear();

        Shuffle(drawPile);

        RefreshDebugView();
    }

    private void Shuffle(List<CardInstance> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    [ContextMenu("Refresh Debug View")]
    public void RefreshDebugView()
    {
        FillDebugList(debugDrawPile, drawPile);
        FillDebugList(debugHand, hand);
        FillDebugList(debugDiscardPile, discardPile);
        FillDebugList(debugExhaustPile, exhaustPile);
    }

    private void FillDebugList(List<string> debugList, List<CardInstance> source)
    {
        debugList.Clear();

        if (source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            CardInstance card = source[i];

            if (card == null || card.data == null)
            {
                debugList.Add($"{i}: null");
                continue;
            }

            debugList.Add($"{i}: {card.data.cardName} / Cost {card.currentCost}");
        }
    }
}