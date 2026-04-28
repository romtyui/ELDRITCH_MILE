using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Card Data")]
public class CardData : ScriptableObject
{
    [Header("Basic")]
    public string cardId;
    public string cardName;
    [TextArea] public string description;

    [Header("Visual")]
    public CardVisualData visualData;

    [Header("Card Rules")]
    public CardType cardType;
    public CardRarity rarity;
    public TargetType targetType;
    public int baseCost = 1;

    [Header("Flags")]
    public bool exhaust;
    public bool ethereal;
    public bool retain;

    [Header("Effects")]
    public List<CardEffectData> effects = new();
}

public class CardInstance
{
    public CardData data;
    public int currentCost;
    public bool isUpgraded;
    public bool isExhaustedThisCombat;

    public CardInstance(CardData data)
    {
        this.data = data;
        currentCost = data.baseCost;
        isUpgraded = false;
        isExhaustedThisCombat = false;
    }
}

public enum CardType
{
    Attack,
    Skill,
    Power,
    Curse,
    Status
}

public enum CardRarity
{
    Basic,
    Common,
    Uncommon,
    Rare
}

public enum TargetType
{
    None,
    Self,
    SingleEnemy,
    AllEnemies,
    AllCharacters
}