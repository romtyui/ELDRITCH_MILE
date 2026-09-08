using System.Collections.Generic;
using UnityEngine;

// ⚠️ 封存的舊碼。包進 namespace 是為了**不要污染全域命名空間** ——
//    這裡宣告的 RunNodeData / MapData / ICardInteractable 等等
//    與正式碼同名，放在全域會蓋掉正式那組，症狀是新檔案裡出現
//    「MapData 沒有 GetNode」這種完全不會指向這裡的錯誤。
namespace EldritchMile.Archive
{

public class ExplorationHandUIController : MonoBehaviour
{
    [Header("Refs")]
    public ExplorationDeck explorationDeck;
    public CardExplorationManager explorationManager; // 已更新為新名稱
    public CardViewUIExplore cardPrefab;
    public RectTransform handRoot;
    public HandFanLayout handFanLayout;

    private readonly Dictionary<CardInstanceExplore, CardViewUIExplore> cardViews = new();

    public void RefreshHandUI()
    {
        ClearHandViews();
        if (explorationDeck == null)
        {
            Debug.LogWarning("[UIController] explorationDeck 沒有指定");
            return;
        }

        foreach (CardInstanceExplore card in explorationDeck.Hand)
        {
            CreateCardView(card);
        }

        if (handFanLayout != null) handFanLayout.RefreshLayout();
    }

    private CardViewUIExplore CreateCardView(CardInstanceExplore card)
    {
        if (card == null) return null;

        CardViewUIExplore view = Instantiate(cardPrefab, handRoot);
        view.gameObject.SetActive(true);
        view.Bind(card);

        RectTransform rect = view.GetComponent<RectTransform>();
        if (handFanLayout != null && rect != null && !handFanLayout.cards.Contains(rect))
        {
            handFanLayout.cards.Add(rect);
        }

        // 動態掛載探索專用的拖曳腳本
        ExplorationCardDragUI drag = view.GetComponent<ExplorationCardDragUI>();
        if (drag == null) drag = view.gameObject.AddComponent<ExplorationCardDragUI>();
        
        drag.manager = explorationManager;
        drag.cardViewUI = view;
        drag.handLayout = handFanLayout;

        CanvasGroup canvasGroup = view.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = view.gameObject.AddComponent<CanvasGroup>();
        
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        if (!cardViews.ContainsKey(card)) cardViews.Add(card, view);
        return view;
    }

    private void ClearHandViews()
    {
        foreach (KeyValuePair<CardInstanceExplore, CardViewUIExplore> pair in cardViews)
        {
            if (pair.Value != null) Destroy(pair.Value.gameObject);
        }
        cardViews.Clear();
        if (handFanLayout != null) handFanLayout.cards.Clear();
    }
}
}
