using System.Collections.Generic;
using UnityEngine;

public class HandFanLayout : MonoBehaviour
{
    [Tooltip("參與手牌排列的卡片 RectTransform 清單。清單索引由小到大對應由左到右；排列時會設定卡片的位置、縮放、旋轉及顯示順序。")]
    public List<RectTransform> cards = new List<RectTransform>();

    [Header("Normal Layout")]
    [Tooltip("相鄰卡片中心的水平間距。數值越大，手牌排列越寬；設為 0 時，所有卡片的水平位置重疊。")]
    public float spacing = 160f;

    [Tooltip("手牌弧形排列的下彎程度。正值越大，兩側卡片越低；設為 0 時，所有卡片高度相同；負值會使兩側卡片向上排列。")]
    public float curveHeight = 60f;

    [Tooltip("相鄰卡片的 Z 軸旋轉角度差，單位為度。正值會使左側卡片逆時針旋轉、右側卡片順時針旋轉；設為 0 時，所有卡片保持直立。")]
    public float angleStep = 6f;

    [Tooltip("一般排列時的整體垂直偏移。正值向上移動，負值向下移動；懸停或鎖定卡片的高度由 Hover Target Y 決定。")]
    public float centerYOffset = 0f;
    
    [Tooltip("一般手牌及抽牌動畫結束時的縮放。X 控制寬度，Y 控制高度；1 表示 100%，0.8 表示 80%。懸停或鎖定時，兩個方向都會再乘上 Hover Scale。")]
    public Vector2 normalScale = Vector2.one;


    [Header("Hover / Drag")]
    [Tooltip("懸停或鎖定卡片的目標 anchoredPosition.y。這是直接指定的 Y 座標，不是原位置的上移距離；鎖定卡片優先於懸停卡片。")]
    public float hoverTargetY = 120f;

    [Tooltip("懸停或鎖定卡片的目標縮放倍率。1 為原始單位縮放，1.15 為放大至 115%；其他卡片會設為單位縮放。")]
    public float hoverScale = 1.15f;

    [Header("Hover Push")]
    [Tooltip("懸停或鎖定卡片時，其左側受影響卡片從一般排列位置向左移動的距離。每張受影響卡片使用相同距離；負值會改為向右移動。")]
    public float hoverPushLeftDistance = 100f;

    [Tooltip("懸停或鎖定卡片時，其右側受影響卡片從一般排列位置向右移動的距離。每張受影響卡片使用相同距離；負值會改為向左移動。")]
    public float hoverPushRightDistance = 100f;

    [Tooltip("是否限制推開卡片的範圍。啟用時，只推開左右各 Hover Push Card Count 張卡片；停用時，左右兩側所有卡片都會被推開。")]
    public bool limitHoverPushRange = false;

    [Min(1)]
    [Tooltip("啟用 Limit Hover Push Range 時，左右各最多推開幾張卡片。以 cards 清單的索引距離判斷，例如 2 表示左側最多 2 張、右側最多 2 張。")]
    public int hoverPushCardCount = 2;

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
        if (cards == null || cards.Count == 0) return;

        CardHoverUI activeCard = lockedCard != null ? lockedCard : currentHoverCard;

        int count = cards.Count;
        float centerIndex = (count - 1) * 0.5f;
        int activeIndex = GetCardIndex(activeCard);

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
            Vector3 targetScale = new Vector3(normalScale.x, normalScale.y, 1f);
            Quaternion targetRot = Quaternion.Euler(0f, 0f, zRotation);

            if (isActive)
            {
                targetPos.y = hoverTargetY;
                targetScale = new Vector3(normalScale.x * hoverScale, normalScale.y * hoverScale, 1f);
                targetRot = Quaternion.identity;
                card.SetAsLastSibling();
            }
            else
            {
                ApplyHoverPush(ref targetPos, i, activeIndex);
                card.SetSiblingIndex(i);
            }

            card.anchoredPosition = targetPos;
            card.localScale = targetScale;
            card.localRotation = targetRot;
        }
    }

    private int GetCardIndex(CardHoverUI activeCard)
    {
        if (activeCard == null)
            return -1;

        for (int i = 0; i < cards.Count; i++)
        {
            RectTransform card = cards[i];

            if (card == null)
                continue;

            CardHoverUI hover = card.GetComponent<CardHoverUI>();

            if (hover == activeCard)
                return i;
        }

        return -1;
    }

    private void ApplyHoverPush(ref Vector2 targetPos, int cardIndex, int activeIndex)
    {
        if (activeIndex < 0)
            return;

        if (cardIndex == activeIndex)
            return;

        int distanceFromActive = Mathf.Abs(cardIndex - activeIndex);

        if (limitHoverPushRange && distanceFromActive > hoverPushCardCount)
            return;

        if (cardIndex < activeIndex)
            targetPos.x -= hoverPushLeftDistance;
        else
            targetPos.x += hoverPushRightDistance;
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