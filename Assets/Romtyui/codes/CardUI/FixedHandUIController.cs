using System.Collections.Generic;
using UnityEngine;

public class FixedHandUIController : MonoBehaviour
{
    public BattleDeck battleDeck;
    public HandFanLayout handFanLayout;
    public List<CardViewUI> cardViews = new List<CardViewUI>();

    public void RefreshHandUI()
    {
        var hand = battleDeck.Hand;

        handFanLayout.cards.Clear();

        for (int i = 0; i < cardViews.Count; i++)
        {
            bool hasCard = i < hand.Count;

            cardViews[i].gameObject.SetActive(hasCard);

            if (!hasCard) continue;

            cardViews[i].Bind(hand[i]);

            RectTransform rect = cardViews[i].GetComponent<RectTransform>();
            if (rect != null)
                handFanLayout.cards.Add(rect);
        }

        handFanLayout.RefreshLayout();
    }
}