using System.Collections.Generic;
using UnityEngine;

public class FixedHandUIController : MonoBehaviour
{
    public BattleDeck battleDeck;
    public HandFanLayout handFanLayout;

    [Header("自動抓 handFanLayout 底下所有 CardViewUI")]
    public List<CardViewUI> cardViews = new List<CardViewUI>();

    private void Awake()
    {
        AutoCollectCardViews();
    }

    private void Start()
    {
        AutoCollectCardViews();
        RefreshHandUI();
    }

    [ContextMenu("Auto Collect Card Views")]
    public void AutoCollectCardViews()
    {
        cardViews.Clear();

        if (handFanLayout == null)
        {
            Debug.LogError("[FixedHandUIController] handFanLayout 沒有指定");
            return;
        }

        CardViewUI[] views = handFanLayout.GetComponentsInChildren<CardViewUI>(true);

        foreach (CardViewUI view in views)
        {
            cardViews.Add(view);
        }

        Debug.Log($"[FixedHandUIController] 自動找到 {cardViews.Count} 張 CardViewUI");
    }

    public void RefreshHandUI()
    {
        if (battleDeck == null)
        {
            Debug.LogError("[FixedHandUIController] battleDeck 沒有指定");
            return;
        }

        if (handFanLayout == null)
        {
            Debug.LogError("[FixedHandUIController] handFanLayout 沒有指定");
            return;
        }

        if (cardViews == null || cardViews.Count == 0)
        {
            AutoCollectCardViews();
        }

        var hand = battleDeck.Hand;

        handFanLayout.cards.Clear();

        for (int i = 0; i < cardViews.Count; i++)
        {
            CardViewUI cardView = cardViews[i];
            if (cardView == null) continue;

            bool hasCard = i < hand.Count;

            cardView.gameObject.SetActive(hasCard);

            if (!hasCard)
                continue;

            cardView.Bind(hand[i]);

            RectTransform rect = cardView.GetComponent<RectTransform>();
            if (rect != null)
            {
                handFanLayout.cards.Add(rect);
            }
        }

        handFanLayout.RefreshLayout();

        Debug.Log($"[RefreshHandUI] 手牌:{hand.Count} / CardViewUI:{cardViews.Count} / 排列:{handFanLayout.cards.Count}");
    }
}