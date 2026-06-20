using UnityEngine;

public class CardExplorationManager : MonoBehaviour
{
    [Header("Deck")]
    public ExplorationDeck explorationDeck;

    [Header("UI")]
    public ExplorationHandUIController handUIController; // 這是下一個階段會新增的腳本

    [Header("Camera")]
    public Camera playerCamera;

    [Header("Rules")]
    public int cardsOnEnterExploration = 5;
    // public bool useEnergy = false;
    // public EnergySystem energySystem; // 暫時註解避免編譯錯誤

    [Header("Runtime")]
    public bool isResolvingCard;

    private void Start()
    {
        // 為了方便測試，一開始就啟動卡牌探索
        StartExploration();
    }

    public void StartExploration()
    {
        if (explorationDeck == null)
        {
            Debug.LogWarning("[CardExplorationManager] explorationDeck 沒有指定");
            return;
        }
        explorationDeck.InitializeDeck();
        
        // if (useEnergy && energySystem != null) energySystem.ResetEnergy();
        
        DrawCards(cardsOnEnterExploration);
        Debug.Log("[CardExplorationManager] 探索卡牌系統開始");
    }

    public bool TryPlayCard(CardInstance card, ExplorationInteractableTarget target)
    {
        if (isResolvingCard)
        {
            Debug.Log("卡牌正在結算中");
            return false;
        }

        if (card == null || card.data == null) return false;

        if (explorationDeck == null || !explorationDeck.IsCardInHand(card))
        {
            Debug.LogWarning("[CardExplorationManager] 這張卡不在手牌中");
            return false;
        }

        if (card.data.explorationTargetMode == ExplorationTargetMode.SceneInteractable)
        {
            if (target == null)
            {
                Debug.Log("這張探索卡需要拖到可互動物件上");
                return false;
            }
            if (!target.CanAccept(card.data))
            {
                Debug.Log($"目標 {target.name} 不接受卡牌 {card.data.cardName}");
                return false;
            }
        }

        // if (useEnergy && energySystem != null) ... 能量判定略

        isResolvingCard = true;

        ExplorationCardResolveContext context = new ExplorationCardResolveContext(
            this,
            explorationDeck,
            card,
            target,
            playerCamera
        );

        Debug.Log($"[CardExplorationManager] 使用探索卡：{card.data.cardName}");

        foreach (ExplorationCardEffectData effect in card.data.explorationEffects)
        {
            if (effect == null) continue;
            effect.Execute(context);
        }

        explorationDeck.OnCardPlayed(card);
        RefreshHandUI();
        
        isResolvingCard = false;
        return true;
    }

    public void DrawCards(int amount)
    {
        if (explorationDeck == null) return;
        explorationDeck.DrawCards(amount);
        RefreshHandUI();
    }

    public void DiscardHand()
    {
        if (explorationDeck == null) return;
        explorationDeck.DiscardHand();
        RefreshHandUI();
    }

    public void RefreshHandUI()
    {
        if (handUIController != null) handUIController.RefreshHandUI();
    }
}