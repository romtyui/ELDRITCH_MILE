using System.Collections.Generic;
using UnityEngine;

public class ExplorationDeck : MonoBehaviour
{
    [Header("Deck")]
    public List<CardDataExplore> startingDeck = new();
    private List<CardInstanceExplore> drawPile = new();
    private List<CardInstanceExplore> hand = new();
    private List<CardInstanceExplore> discardPile = new();
    private List<CardInstanceExplore> exhaustPile = new();

    public IReadOnlyList<CardInstanceExplore> Hand => hand;
    public IReadOnlyList<CardInstanceExplore> DrawPile => drawPile;
    public IReadOnlyList<CardInstanceExplore> DiscardPile => discardPile;
    public IReadOnlyList<CardInstanceExplore> ExhaustPile => exhaustPile;

    public void InitializeDeck()
    {
        drawPile.Clear();
        hand.Clear();
        discardPile.Clear();
        exhaustPile.Clear();

        foreach (CardDataExplore card in startingDeck)
        {
            if (card == null) continue;
            drawPile.Add(new CardInstanceExplore(card));
        }
        Shuffle(drawPile);
    }

    public CardInstanceExplore DrawOneCard()
    {
        if (drawPile.Count == 0)
            ReshuffleDiscardIntoDraw();
        if (drawPile.Count == 0)
            return null;

        CardInstanceExplore top = drawPile[0];
        drawPile.RemoveAt(0);
        hand.Add(top);
        return top;
    }

    public void DrawCards(int amount)
    {
        for (int i = 0; i < amount; i++) DrawOneCard();
    }

    public void OnCardPlayed(CardInstanceExplore card)
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
        foreach (CardInstanceExplore card in hand)
        {
            if (card == null || card.data == null) continue;
            if (card.data.retain) continue;
            discardPile.Add(card);
        }
        hand.RemoveAll(card => card == null || card.data == null || !card.data.retain);
    }

    public bool IsCardInHand(CardInstanceExplore card)
    {
        return hand.Contains(card);
    }

    public void AddCardToDiscardPile(CardDataExplore cardData)
    {
        if (cardData == null) return;
        discardPile.Add(new CardInstanceExplore(cardData));
    }

    private void ReshuffleDiscardIntoDraw()
    {
        drawPile.AddRange(discardPile);
        discardPile.Clear();
        Shuffle(drawPile);
    }

    private void Shuffle(List<CardInstanceExplore> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}