using System.Collections.Generic;
using UnityEngine;

public class BattleDeck : MonoBehaviour
{
    public List<CardData> startingDeck = new();

    private List<CardInstance> drawPile = new();
    private List<CardInstance> hand = new();
    private List<CardInstance> discardPile = new();
    private List<CardInstance> exhaustPile = new();

    public IReadOnlyList<CardInstance> Hand => hand;

    public void InitializeDeck()
    {
        drawPile.Clear();
        hand.Clear();
        discardPile.Clear();
        exhaustPile.Clear();

        foreach (var card in startingDeck)
            drawPile.Add(new CardInstance(card));

        Shuffle(drawPile);
    }
    public bool TransformRandomCardInDrawPileByPool(CardTransformPoolData transformPool)
    {
        if (transformPool == null)
        {
            Debug.LogWarning("[BattleDeck] transformPool 是 null，無法變換牌");
            return false;
        }

        if (drawPile == null || drawPile.Count == 0)
        {
            Debug.Log("[BattleDeck] 抽牌堆沒有牌可以變換");
            return false;
        }

        List<int> validIndexes = new List<int>();

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

        if (validIndexes.Count == 0)
        {
            Debug.Log($"[BattleDeck] 抽牌堆中沒有任何牌可以被變化卡池 {transformPool.transformId} 變換");
            return false;
        }

        int selectedListIndex = Random.Range(0, validIndexes.Count);
        int selectedDrawPileIndex = validIndexes[selectedListIndex];

        CardInstance oldCard = drawPile[selectedDrawPileIndex];
        CardData oldCardData = oldCard.data;

        bool hasResult = transformPool.TryGetTransformResult(oldCardData, out CardData newCardData);

        if (!hasResult || newCardData == null)
        {
            Debug.LogWarning("[BattleDeck] 找到候選牌，但沒有取得變換結果");
            return false;
        }

        drawPile[selectedDrawPileIndex] = new CardInstance(newCardData);

        Debug.Log($"[BattleDeck] 變化卡池 {transformPool.transformId}：{oldCardData.cardName} → {newCardData.cardName}");

        return true;
    }
    public void DrawCards(int amount)
    {
        for (int i = 0; i < amount; i++)
            DrawOneCard();
    }

    public CardInstance DrawOneCard()
    {
        if (drawPile.Count == 0)
            ReshuffleDiscardIntoDraw();

        if (drawPile.Count == 0)
            return null;

        CardInstance top = drawPile[0];
        drawPile.RemoveAt(0);
        hand.Add(top);

        return top;
    }

    public void OnCardPlayed(CardInstance card)
    {
        if (hand.Remove(card))
        {
            if (card.data.exhaust)
                exhaustPile.Add(card);
            else
                discardPile.Add(card);
        }
    }

    public void DiscardHandAtEndTurn()
    {
        foreach (var card in hand)
        {
            if (card.data.retain) continue;
            discardPile.Add(card);
        }

        hand.RemoveAll(c => !c.data.retain);
    }

    private void ReshuffleDiscardIntoDraw()
    {
        drawPile.AddRange(discardPile);
        discardPile.Clear();
        Shuffle(drawPile);
    }

    private void Shuffle(List<CardInstance> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}