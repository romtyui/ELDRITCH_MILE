using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandUIController : MonoBehaviour
{
    [Header("Refs")]
    public BattleDeck battleDeck;
    public CardViewUI cardPrefab;
    public RectTransform handRoot;
    public HandFanLayout handFanLayout;

    //[Header("Draw Pre Animation")]
    //public Animator drawAnimator;
    //public string drawAnimationTrigger = "Open";
    //public float drawEventTimeout = 2f;
    [Header("Draw Pre Animation")]
    public BagDrawAnimationController bagDrawAnimationController;

    private bool drawAnimationEventReceived;

    [Header("Draw Animation")]
    public RectTransform drawStartPoint;
    public float drawDuration = 0.35f;
    public float delayBetweenCards = 0.08f;
    public float arcHeight = 220f;
    public float startScale = 0.45f;

    private readonly Dictionary<CardInstance, CardViewUI> cardViews = new();

    private Canvas rootCanvas;
    private Camera canvasCamera;

    private void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();

        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            canvasCamera = rootCanvas.worldCamera;
        else
            canvasCamera = null;
    }

    public void RefreshHandUI()
    {
        ClearHandViews();

        if (battleDeck == null)
        {
            Debug.LogWarning("[HandUIController] battleDeck 沒有指定");
            return;
        }

        foreach (CardInstance card in battleDeck.Hand)
        {
            CreateCardView(card);
        }

        if (handFanLayout != null)
            handFanLayout.RefreshLayout();
    }

    public IEnumerator DrawCardsAnimated(BattleDeck deck, int amount)
    {
        if (deck == null)
        {
            Debug.LogWarning("[HandUIController] DrawCardsAnimated deck 是 null");
            yield break;
        }

        if (cardPrefab == null)
        {
            Debug.LogWarning("[HandUIController] cardPrefab 沒有指定");
            yield break;
        }

        if (handRoot == null)
        {
            Debug.LogWarning("[HandUIController] handRoot 沒有指定");
            yield break;
        }

        if (handFanLayout == null)
        {
            Debug.LogWarning("[HandUIController] handFanLayout 沒有指定");
            yield break;
        }

        for (int i = 0; i < amount; i++)
        {
            CardInstance drawnCard = deck.DrawOneCard();

            if (drawnCard == null)
                yield break;

            CardViewUI cardView = CreateCardView(drawnCard);
            RectTransform cardRect = cardView.GetComponent<RectTransform>();

            handFanLayout.RefreshLayout();

            Vector2 targetPos = cardRect.anchoredPosition;
            Vector3 targetScale = cardRect.localScale;
            Quaternion targetRotation = cardRect.localRotation;

            Vector2 startPos = GetDrawStartLocalPosition();

            cardRect.anchoredPosition = startPos;
            cardRect.localScale = Vector3.one * startScale;
            cardRect.localRotation = Quaternion.identity;

            yield return AnimateCardToHand(
                cardRect,
                startPos,
                targetPos,
                targetScale,
                targetRotation
            );

            handFanLayout.RefreshLayout();

            if (delayBetweenCards > 0f)
                yield return new WaitForSeconds(delayBetweenCards);
        }
    }

    private CardViewUI CreateCardView(CardInstance card)
    {
        if (card == null)
            return null;

        CardViewUI view = Instantiate(cardPrefab, handRoot);
        view.Bind(card);

        RectTransform rect = view.GetComponent<RectTransform>();

        if (handFanLayout != null && !handFanLayout.cards.Contains(rect))
            handFanLayout.cards.Add(rect);

        if (!cardViews.ContainsKey(card))
            cardViews.Add(card, view);

        return view;
    }

    private void ClearHandViews()
    {
        foreach (var pair in cardViews)
        {
            if (pair.Value != null)
                Destroy(pair.Value.gameObject);
        }

        cardViews.Clear();

        if (handFanLayout != null)
            handFanLayout.cards.Clear();
    }

    private Vector2 GetDrawStartLocalPosition()
    {
        if (drawStartPoint == null)
            return Vector2.zero;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCamera, drawStartPoint.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            handRoot,
            screenPoint,
            canvasCamera,
            out Vector2 localPoint
        );

        return localPoint;
    }

    private IEnumerator AnimateCardToHand(
        RectTransform cardRect,
        Vector2 startPos,
        Vector2 targetPos,
        Vector3 targetScale,
        Quaternion targetRotation
    )
    {
        float timer = 0f;

        while (timer < drawDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / drawDuration);
            float smoothT = Smooth01(t);

            Vector2 pos = GetBezierPoint(startPos, targetPos, smoothT);

            cardRect.anchoredPosition = pos;
            cardRect.localScale = Vector3.Lerp(Vector3.one * startScale, targetScale, smoothT);
            cardRect.localRotation = Quaternion.Lerp(Quaternion.identity, targetRotation, smoothT);

            yield return null;
        }

        cardRect.anchoredPosition = targetPos;
        cardRect.localScale = targetScale;
        cardRect.localRotation = targetRotation;
    }
    public void DetachCardViewForPlay(CardInstance card, Transform newParent)
    {
        if (card == null)
            return;

        if (!cardViews.TryGetValue(card, out CardViewUI view))
            return;

        RectTransform rect = view.GetComponent<RectTransform>();

        if (handFanLayout != null && rect != null)
        {
            handFanLayout.cards.Remove(rect);
        }

        cardViews.Remove(card);

        if (rect != null && newParent != null)
        {
            rect.SetParent(newParent, true);
        }

        if (handFanLayout != null)
            handFanLayout.RefreshLayout();
    }
    private Vector2 GetBezierPoint(Vector2 start, Vector2 end, float t)
    {
        Vector2 middle = (start + end) * 0.5f;
        middle.y += arcHeight;

        Vector2 a = Vector2.Lerp(start, middle, t);
        Vector2 b = Vector2.Lerp(middle, end, t);

        return Vector2.Lerp(a, b, t);
    }

    private float Smooth01(float t)
    {
        return t * t * (3f - 2f * t);
    }

    public IEnumerator DrawCardsAnimatedWithBag(BattleDeck deck, int amount)
    {
        if (amount <= 0)
            yield break;

        if (bagDrawAnimationController != null)
        {
            yield return bagDrawAnimationController.PlayAndWaitForDraw(
                () => DrawCardsAnimated(deck, amount)
            );
        }
        else
        {
            yield return DrawCardsAnimated(deck, amount);
        }
    }
    //public IEnumerator DrawCardsAnimatedAfterEvent(BattleDeck deck, int amount)
    //{
    //    if (amount <= 0)
    //        yield break;

    //    drawAnimationEventReceived = false;

    //    if (drawAnimator != null)
    //    {
    //        drawAnimator.SetTrigger(drawAnimationTrigger);

    //        float timer = 0f;

    //        while (!drawAnimationEventReceived)
    //        {
    //            timer += Time.deltaTime;

    //            if (timer >= drawEventTimeout)
    //            {
    //                Debug.LogWarning("[HandUIController] 等待抽牌動畫事件逾時，直接開始抽牌");
    //                break;
    //            }

    //            yield return null;
    //        }
    //    }
    //    else
    //    {
    //        Debug.LogWarning("[HandUIController] drawAnimator 沒有指定，直接開始抽牌");
    //    }

    //    yield return DrawCardsAnimated(deck, amount);
    //}

    //public void OnDrawAnimationEvent()
    //{
    //    drawAnimationEventReceived = true;
    //}
}