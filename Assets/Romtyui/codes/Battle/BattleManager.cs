using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public BattleUnit playerUnit;
    public BattleUnit currentEnemy;
    public BattleDeck playerDeck;
    public EnergySystem energySystem;

    public int cardsPerTurn = 5;

    // 二選一：
    // 如果你現在是固定10張手牌UI，就拖 FixedHandUIController
    public FixedHandUIController fixedHandUIController;

    // 如果你之後改成動態生成手牌，再改拖 HandUIController
    // public HandUIController handUIController;

    //遊戲流程
    private void Start()
    {
        StartBattle();
    }

    public void StartBattle()
    {
        playerDeck.InitializeDeck();
        energySystem.ResetEnergy();
        playerDeck.DrawCards(cardsPerTurn);

        RefreshHandUI();

        Debug.Log($"戰鬥開始，手牌數量：{playerDeck.Hand.Count}");
    }

    public void StartPlayerTurn()
    {
        energySystem.ResetEnergy();
        playerDeck.DrawCards(cardsPerTurn);
        RefreshHandUI();
    }

    public void EndPlayerTurn()
    {
        playerDeck.DiscardHandAtEndTurn();
        RefreshHandUI();

        // 之後這裡放敵人行動
        StartPlayerTurn();
    }
    //流程結束

    public bool TryPlayCard(CardInstance card, BattleUnit target)
    {
        if (card == null || card.data == null)
            return false;

        BattleUnit finalTarget = ResolveTarget(card.data.targetType, target);

        if (card.data.targetType == TargetType.SingleEnemy && finalTarget == null)
        {
            Debug.Log("沒有選到敵人");
            return false;
        }

        if (!energySystem.Spend(card.currentCost))
        {
            Debug.Log("能量不足");
            return false;
        }

        CardResolveContext context = new CardResolveContext(playerUnit, finalTarget, card, this);

        Debug.Log($"打出卡牌: {card.data.cardName}");

        foreach (var effect in card.data.effects)
        {
            if (effect == null) continue;
            effect.Execute(context);
        }

        if (card.data.targetType == TargetType.SingleEnemy && finalTarget == null)
        {
            Debug.Log("沒有選到敵人，攻擊牌不打出");
            return false;
        }

        playerDeck.OnCardPlayed(card);

        RefreshHandUI();
        Debug.Log($"[TryPlayCard] card = {card.data.cardName}, target = {(target != null ? target.unitName : "null")}");
        return true;
    }

    private BattleUnit ResolveTarget(TargetType targetType, BattleUnit selectedTarget)
    {
        switch (targetType)
        {
            case TargetType.Self:
                return playerUnit;

            case TargetType.SingleEnemy:
                return selectedTarget;

            case TargetType.None:
            default:
                return null;
        }
    }

    public void PlayerDrawCards(int amount)
    {
        playerDeck.DrawCards(amount);
        //RefreshHandUI();
    }

    public void GainEnergy(int amount)
    {
        energySystem.GainEnergy(amount);
    }

    private void RefreshHandUI()
    {
        if (fixedHandUIController != null)
            fixedHandUIController.RefreshHandUI();

        // 如果以後改用動態手牌UI，再打開這段
        // if (handUIController != null)
        //     handUIController.RefreshHandUI();
    }
}