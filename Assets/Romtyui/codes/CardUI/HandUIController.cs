using UnityEngine;

public class HandUIController : MonoBehaviour
{
    [Header("Data")]
    public BattleDeck battleDeck;

    [Header("UI")]
    public HandFanLayout handFanLayout;
    public CardViewUI cardPrefab;
    public Transform cardParent;

    public void RefreshHandUI()
    {
        if (battleDeck == null || handFanLayout == null || cardPrefab == null)
        {
            Debug.LogWarning("[HandUIController] 缺少必要參考");
            return;
        }

        if (cardParent == null)
            cardParent = handFanLayout.transform;

        ClearHandUI();

        var hand = battleDeck.Hand;

        for (int i = 0; i < hand.Count; i++)
        {
            CardViewUI cardView = Instantiate(cardPrefab, cardParent);
            cardView.gameObject.SetActive(true);
            cardView.Bind(hand[i]);

            RectTransform rect = cardView.GetComponent<RectTransform>();
            if (rect != null)
                handFanLayout.cards.Add(rect);
        }

        handFanLayout.RefreshLayout();
    }

    private void ClearHandUI()
    {
        handFanLayout.cards.Clear();

        if (cardParent == null)
            return;

        for (int i = cardParent.childCount - 1; i >= 0; i--)
        {
            Destroy(cardParent.GetChild(i).gameObject);
        }
    }
}