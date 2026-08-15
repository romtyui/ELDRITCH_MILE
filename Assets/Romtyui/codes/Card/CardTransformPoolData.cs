using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Transform/Transform Pool")]
public class CardTransformPoolData : ScriptableObject
{
    [Header("Pool Identity")]
    public string transformId;

    [Header("Transform Entries")]
    public List<CardTransformEntry> entries = new();

    public bool TryGetTransformResult(CardData originalCard, out CardData resultCard)
    {
        resultCard = null;

        if (originalCard == null)
            return false;

        for (int i = 0; i < entries.Count; i++)
        {
            CardTransformEntry entry = entries[i];

            if (entry == null)
                continue;

            if (entry.originalCard == originalCard)
            {
                resultCard = entry.resultCard;
                return resultCard != null;
            }

            if (!string.IsNullOrEmpty(entry.originalCardId) &&
                !string.IsNullOrEmpty(originalCard.cardId) &&
                entry.originalCardId == originalCard.cardId)
            {
                resultCard = entry.resultCard;
                return resultCard != null;
            }
        }

        return false;
    }
}

[System.Serializable]
public class CardTransformEntry
{
    [Header("Original")]
    public CardData originalCard;

    [Tooltip("可選。若有填，會用 cardId 比對。")]
    public string originalCardId;

    [Header("Result")]
    public CardData resultCard;
}