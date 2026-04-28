using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    [Header("Units")]
    public BattleUnit playerUnit;

    [Header("Enemies")]
    public EnemyUnit currentEnemy;
    public List<EnemyUnit> enemies = new();

    [Header("Player Systems")]
    public BattleDeck playerDeck;
    public EnergySystem energySystem;

    [Header("Turn Settings")]
    public int cardsPerTurn = 5;
    public float enemyActionDelay = 0.5f;

    [Header("UI")]
    public HandUIController handUIController;

    [Header("Runtime")]
    public BattlePhase currentPhase = BattlePhase.None;

    private bool isChangingTurn;

    private void Start()
    {
        StartBattle();
    }

    public void StartBattle()
    {
        currentPhase = BattlePhase.None;
        isChangingTurn = false;

        if (playerDeck != null)
            playerDeck.InitializeDeck();

        if (enemies == null || enemies.Count == 0)
            AutoCollectEnemies();

        if (currentEnemy == null)
            currentEnemy = GetFirstAliveEnemy();

        StartPlayerTurn();

        if (playerDeck != null)
            Debug.Log($"戰鬥開始，手牌數量：{playerDeck.Hand.Count}");
    }

    [ContextMenu("Auto Collect Enemies")]
    public void AutoCollectEnemies()
    {
        enemies.Clear();

        EnemyUnit[] foundEnemies = FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None);

        foreach (EnemyUnit enemy in foundEnemies)
        {
            if (enemy == null) continue;
            if (!enemy.gameObject.activeInHierarchy) continue;

            enemies.Add(enemy);
        }

        currentEnemy = GetFirstAliveEnemy();

        Debug.Log($"[BattleManager] 自動找到 {enemies.Count} 隻怪物，目前目標：{(currentEnemy != null ? currentEnemy.unitName : "null")}");
    }

    public void StartPlayerTurn()
    {
        if (currentPhase == BattlePhase.BattleEnded)
            return;

        StartCoroutine(StartPlayerTurnRoutine());
    }

    private IEnumerator StartPlayerTurnRoutine()
    {
        currentPhase = BattlePhase.PlayerTurn;
        isChangingTurn = false;

        if (playerUnit != null)
            playerUnit.ResetBlock();

        if (energySystem != null)
            energySystem.ResetEnergy();

        Debug.Log("玩家回合開始");

        if (handUIController != null)
            yield return handUIController.DrawCardsAnimated(playerDeck, cardsPerTurn);
        else
            playerDeck.DrawCards(cardsPerTurn);
    }

    public void EndPlayerTurn()
    {
        if (currentPhase != BattlePhase.PlayerTurn)
            return;

        if (isChangingTurn)
            return;

        isChangingTurn = true;

        if (playerDeck != null)
            playerDeck.DiscardHandAtEndTurn();

        RefreshHandUI();

        Debug.Log("玩家回合結束");

        StartCoroutine(EnemyTurnRoutine());
    }

    private IEnumerator EnemyTurnRoutine()
    {
        StartEnemyTurn();

        yield return new WaitForSeconds(enemyActionDelay);

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyUnit enemy = enemies[i];

            if (enemy == null) continue;
            if (!enemy.gameObject.activeInHierarchy) continue;
            if (enemy.currentHp <= 0) continue;

            currentEnemy = enemy;

            enemy.ResetBlock();
            enemy.ExecuteTurn(playerUnit, this);

            yield return new WaitForSeconds(enemyActionDelay);

            if (playerUnit != null && playerUnit.currentHp <= 0)
            {
                EndBattle(false);
                yield break;
            }
        }

        currentEnemy = GetFirstAliveEnemy();

        EndEnemyTurn();
    }

    public void StartEnemyTurn()
    {
        if (currentPhase == BattlePhase.BattleEnded)
            return;

        currentPhase = BattlePhase.EnemyTurn;

        Debug.Log("怪物回合開始");
    }

    public void EndEnemyTurn()
    {
        if (currentPhase == BattlePhase.BattleEnded)
            return;

        Debug.Log("怪物回合結束");

        StartPlayerTurn();
    }

    public bool TryPlayCard(CardInstance card, BattleUnit target)
    {
        if (currentPhase != BattlePhase.PlayerTurn)
        {
            Debug.Log("現在不是玩家回合，不能出牌");
            return false;
        }

        if (card == null || card.data == null)
            return false;

        BattleUnit finalTarget = ResolveTarget(card.data.targetType, target);

        if (card.data.targetType == TargetType.SingleEnemy && finalTarget == null)
        {
            Debug.Log("沒有選到敵人，也沒有 currentEnemy 可以攻擊");
            return false;
        }

        if (energySystem == null)
        {
            Debug.LogWarning("[BattleManager] energySystem 沒有指定");
            return false;
        }

        if (!energySystem.Spend(card.currentCost))
        {
            Debug.Log("能量不足");
            return false;
        }

        if (playerDeck != null)
            playerDeck.OnCardPlayed(card);

        RefreshHandUI();

        CardResolveContext context = new CardResolveContext(playerUnit, finalTarget, card, this);

        Debug.Log($"打出卡牌: {card.data.cardName}");

        foreach (CardEffectData effect in card.data.effects)
        {
            if (effect == null) continue;
            effect.Execute(context);
        }

        Debug.Log($"[TryPlayCard] card = {card.data.cardName}, finalTarget = {(finalTarget != null ? finalTarget.unitName : "null")}");

        CheckBattleEnd();

        return true;
    }

    private BattleUnit ResolveTarget(TargetType targetType, BattleUnit selectedTarget)
    {
        switch (targetType)
        {
            case TargetType.Self:
                return playerUnit;

            case TargetType.SingleEnemy:
                if (selectedTarget != null)
                    return selectedTarget;

                if (currentEnemy != null && currentEnemy.gameObject.activeInHierarchy && currentEnemy.currentHp > 0)
                    return currentEnemy;

                currentEnemy = GetFirstAliveEnemy();
                return currentEnemy;

            case TargetType.None:
            default:
                return null;
        }
    }

    private EnemyUnit GetFirstAliveEnemy()
    {
        if (enemies == null)
            return null;

        foreach (EnemyUnit enemy in enemies)
        {
            if (enemy == null) continue;
            if (!enemy.gameObject.activeInHierarchy) continue;
            if (enemy.currentHp <= 0) continue;

            return enemy;
        }

        return null;
    }

    public void PlayerDrawCards(int amount)
    {
        if (amount <= 0)
            return;

        StartCoroutine(PlayerDrawCardsRoutine(amount));
    }

    private IEnumerator PlayerDrawCardsRoutine(int amount)
    {
        if (handUIController != null)
        {
            yield return handUIController.DrawCardsAnimated(playerDeck, amount);
        }
        else
        {
            playerDeck.DrawCards(amount);
            RefreshHandUI();
        }
    }

    public void GainEnergy(int amount)
    {
        if (energySystem != null)
            energySystem.GainEnergy(amount);
    }

    private void RefreshHandUI()
    {
        if (handUIController != null)
            handUIController.RefreshHandUI();
    }

    private void CheckBattleEnd()
    {
        if (playerUnit != null && playerUnit.currentHp <= 0)
        {
            EndBattle(false);
            return;
        }

        if (enemies == null)
            enemies = new List<EnemyUnit>();

        enemies.RemoveAll(enemy => enemy == null);

        if (enemies.Count == 0)
        {
            AutoCollectEnemies();
        }

        if (enemies.Count == 0)
        {
            Debug.LogWarning("[CheckBattleEnd] enemies 清單是空的，無法判斷戰鬥勝利。請把 EnemyUnit 加到 BattleManager.enemies。");
            return;
        }

        bool allEnemiesDead = true;

        foreach (EnemyUnit enemy in enemies)
        {
            if (enemy == null) continue;

            if (enemy.gameObject.activeInHierarchy && enemy.currentHp > 0)
            {
                allEnemiesDead = false;
                break;
            }
        }

        if (allEnemiesDead)
        {
            EndBattle(true);
            return;
        }

        currentEnemy = GetFirstAliveEnemy();
    }

    private void EndBattle(bool playerWin)
    {
        currentPhase = BattlePhase.BattleEnded;
        isChangingTurn = true;

        RefreshHandUI();

        if (playerWin)
            Debug.Log("戰鬥勝利");
        else
            Debug.Log("戰鬥失敗");
    }
}