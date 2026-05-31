using System.Collections.Generic;
using UnityEngine;

public class HandFanLayout : MonoBehaviour
{
    public List<RectTransform> cards = new List<RectTransform>();

    [Header("Normal Layout")]
    public float spacing = 160f;
    public float curveHeight = 60f;
    public float angleStep = 6f;
    public float centerYOffset = 0f;

    [Header("Hover / Drag")]
    public float hoverTargetY = 120f;
    public float hoverScale = 1.15f;

    private CardHoverUI currentHoverCard;
    private CardHoverUI lockedCard;

    public void SetHover(CardHoverUI hoverCard)
    {
        currentHoverCard = hoverCard;
        RefreshLayout();
    }

    public void ClearHover(CardHoverUI hoverCard)
    {
        if (currentHoverCard == hoverCard)
            currentHoverCard = null;

        RefreshLayout();
    }

    public void SetLockedCard(CardHoverUI card)
    {
        lockedCard = card;
        RefreshLayout();
    }

    public void ClearLockedCard(CardHoverUI card)
    {
        if (lockedCard == card)
            lockedCard = null;

        RefreshLayout();
    }

    public void RefreshLayout()
    {
        if (cards == null || cards.Count == 0)
            return;

        CardHoverUI activeCard = lockedCard != null ? lockedCard : currentHoverCard;

        int count = cards.Count;
        float centerIndex = (count - 1) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            RectTransform card = cards[i];
            if (card == null) continue;

            float offsetFromCenter = i - centerIndex;

            float x = offsetFromCenter * spacing;
            float y = -(offsetFromCenter * offsetFromCenter) * curveHeight / 10f + centerYOffset;
            float zRotation = -offsetFromCenter * angleStep;

            CardHoverUI hover = card.GetComponent<CardHoverUI>();
            bool isActive = activeCard != null && hover == activeCard;

            Vector2 targetPos = new Vector2(x, y);
            Vector3 targetScale = Vector3.one;
            Quaternion targetRot = Quaternion.Euler(0f, 0f, zRotation);

            if (isActive)
            {
                targetPos.y = hoverTargetY;
                targetScale = Vector3.one * hoverScale;
                targetRot = Quaternion.identity;
                card.SetAsLastSibling();
            }
            else
            {
                card.SetSiblingIndex(i);
            }

            card.anchoredPosition = targetPos;
            card.localScale = targetScale;
            card.localRotation = targetRot;
        }
    }
    public void ClearAllSelection()
    {
        currentHoverCard = null;
        lockedCard = null;
        RefreshLayout();
    }
    private void Start()
    {
        RefreshLayout();
    }

    //#if UNITY_EDITOR
    //    private void OnValidate()
    //    {
    //        if (!Application.isPlaying)
    //        {
    //            RefreshLayout();
    //        }
    //    }
    //#endif
}
