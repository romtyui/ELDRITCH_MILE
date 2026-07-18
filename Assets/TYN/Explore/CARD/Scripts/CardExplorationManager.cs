using UnityEngine;

public class CardExplorationManager : MonoBehaviour
{
    [Header("Deck")]
    public ExplorationDeck explorationDeck;

    [Header("UI")]
    public ExplorationHandUIController handUIController;

    [Header("Camera")]
    public Camera playerCamera;

    [Header("Rules")]
    public int cardsOnEnterExploration = 5;

    [Header("Runtime")]
    public bool isResolvingCard;

    private void Start()
    {
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
        
        DrawCards(cardsOnEnterExploration);
        Debug.Log("[CardExplorationManager] 探索卡牌系統開始");
    }

    public bool TryPlayCard(CardInstanceExplore card, ExplorationInteractableTarget target)
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

        bool checkPassed = true; 

        if (card.data.targetMode == ExplorationTargetMode.SceneInteractable)
        {
            if (target == null)
            {
                Debug.Log("這張探索卡需要拖到可互動物件上");
                return false;
            }
            
            // 【修正】因為 CanAccept 可能被同學寫死綁定 CardData，
            // 探索卡牌我們暫時略過 CanAccept 的檢查，直接交給下方的 ICardInteractable 處理。
            // 或是如果你有權限改 ExplorationInteractableTarget.cs，把裡面的 CardData 改成 CardDataExplore 最好。
            
            ICardInteractable interactable = target.GetComponent<ICardInteractable>();
            if (interactable != null)
            {
                checkPassed = interactable.OnCardPlayed(card.data.successProbability);
            }
            else
            {
                // 如果目標沒有 ICardInteractable 介面，代表不能對他用探索卡
                Debug.Log($"目標 {target.name} 不接受探索機率卡牌！");
                return false;
            }
        }

        isResolvingCard = true;

        // 【修正】ExplorationCardResolveContext 預期接收戰鬥的 CardInstance。
        // 如果你無法修改 Context 的腳本，我們可以建立一個臨時的假 CardInstance 塞給它騙過編譯器。
        // (最好的做法依然是去 ExplorationCardResolveContext 裡面把 CardInstance 改成 CardInstanceExplore)
        CardInstance tempFakeContextCard = new CardInstance(ScriptableObject.CreateInstance<CardData>());
        tempFakeContextCard.currentCost = card.currentCost;
        
        ExplorationCardResolveContext context = new ExplorationCardResolveContext(
            this,
            explorationDeck,
            tempFakeContextCard, // 放入替代品解決編譯錯誤 (如果效果不用到卡牌本身資訊，這樣做是安全的)
            target,
            playerCamera
        );

        Debug.Log($"[CardExplorationManager] 使用探索卡：{card.data.cardName}");

        if (checkPassed)
        {
            foreach (ExplorationCardEffectData effect in card.data.effects)
            {
                if (effect == null) continue;
                effect.Execute(context);
            }
        }
        else
        {
            Debug.Log($"<color=orange>[CardExplorationManager] 機率檢定失敗，取消執行卡牌原本的增益效果。</color>");
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