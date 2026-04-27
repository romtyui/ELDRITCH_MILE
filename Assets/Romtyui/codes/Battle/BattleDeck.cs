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

    public void DrawCards(int amount)
    {
        for (int i = 0; i < amount; i++)
            DrawOne();
    }

    private void DrawOne()
    {
        if (drawPile.Count == 0)
            ReshuffleDiscardIntoDraw();

        if (drawPile.Count == 0)
            return;

        CardInstance top = drawPile[0];
        drawPile.RemoveAt(0);
        hand.Add(top);
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