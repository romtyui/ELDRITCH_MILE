using System.Collections.Generic;
using UnityEngine;

public class ExplorationDeck : MonoBehaviour
{
    [Header("Deck")]
    public List<CardData> startingDeck = new();
    private List<CardInstance> drawPile = new();
    private List<CardInstance> hand = new();
    private List<CardInstance> discardPile = new();
    private List<CardInstance> exhaustPile = new();

    public IReadOnlyList<CardInstance> Hand => hand;
    public IReadOnlyList<CardInstance> DrawPile => drawPile;
    public IReadOnlyList<CardInstance> DiscardPile => discardPile;
    public IReadOnlyList<CardInstance> ExhaustPile => exhaustPile;

    public void InitializeDeck()
    {
        drawPile.Clear();
        hand.Clear();
        discardPile.Clear();
        exhaustPile.Clear();

        foreach (CardData card in startingDeck)
        {
            if (card == null) continue;
            drawPile.Add(new CardInstance(card));
        }
        Shuffle(drawPile);
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

    public void DrawCards(int amount)
    {
        for (int i = 0; i < amount; i++) DrawOneCard();
    }

    public void OnCardPlayed(CardInstance card)
    {
        if (card == null) return;
        if (hand.Remove(card))
        {
            if (card.data != null && card.data.exhaust)
                exhaustPile.Add(card);
            else
                discardPile.Add(card);
        }
    }

    public void DiscardHand()
    {
        foreach (CardInstance card in hand)
        {
            if (card == null || card.data == null) continue;
            if (card.data.retain) continue;
            discardPile.Add(card);
        }
        hand.RemoveAll(card => card == null || card.data == null || !card.data.retain);
    }

    public bool IsCardInHand(CardInstance card)
    {
        return hand.Contains(card);
    }

    public void AddCardToDiscardPile(CardData cardData)
    {
        if (cardData == null) return;
        discardPile.Add(new CardInstance(cardData));
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