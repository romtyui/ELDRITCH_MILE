using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Card Data Explore")]
public class CardDataExplore : ScriptableObject
{
    [Header("Basic")]
    public string cardId;
    public string cardName;
    [TextArea] public string description;

    [Header("Visual (探索卡牌專屬外觀)")]
    public CardVisualDataExplore visualData;

    [Header("Exploration Rules")]
    public ExplorationTargetMode targetMode = ExplorationTargetMode.None;
    public int baseCost = 0; 

    // ==========================================
    // 【補回遺失的卡牌標籤 Flags】
    // 解決 ExplorationDeck 報錯找不到 exhaust, retain 等屬性的問題
    // ==========================================
    [Header("Flags")]
    public bool exhaust;  // 消耗：打出後移出遊戲
    public bool ethereal; // 虛無：回合結束若在手牌則消耗
    public bool retain;   // 保留：回合結束不丟棄

    [Header("機率檢定設定")]
    [Tooltip("這張卡牌在探索時使用的基礎成功機率 (0.0 = 0%, 1.0 = 100%)")]
    [Range(0f, 1f)]
    public float successProbability = 1.0f;
    
    [Header("Exploration Effects")]
    public List<ExplorationCardEffectData> effects = new();
}

// 探索專用的卡牌實體 (執行時期使用)
public class CardInstanceExplore
{
    public CardDataExplore data;
    public int currentCost;
    
    // 【補回遺失的執行時期屬性】
    public bool isUpgraded;
    public bool isExhaustedThisCombat;

    public CardInstanceExplore(CardDataExplore data)
    {
        this.data = data;
        currentCost = data.baseCost;
        isUpgraded = false;
        isExhaustedThisCombat = false;
    }
}

public enum ExplorationTargetMode
{
    None,
    SceneInteractable
}