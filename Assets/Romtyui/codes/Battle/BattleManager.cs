using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    [Header("Test Option Menu")]
    public OptionMenuUI optionMenuUI;

    [Header("Next Battle Test")]
    public bool autoStartNextBattleOnWin = true;
    public float nextBattleDelay = 1f;


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

    [Header("Enemy Spawning")]
    public EnemyFormationSpawner enemyFormationSpawner;

    private bool isChangingTurn;

    [Header("Special Animation")]
    public CardTransformAnimationController transformAnimationController;

    private bool isResolvingCard;
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

        if (energySystem != null)
            energySystem.ResetEnergy();

        SpawnEnemiesForBattle();

        StartPlayerTurn();

        if (playerDeck != null)
            Debug.Log($"戰鬥開始，手牌數量：{playerDeck.Hand.Count}");
    }

    private void SpawnEnemiesForBattle()
    {
        if (enemyFormationSpawner != null)
        {
            enemyFormationSpawner.battleManager = this;
            enemyFormationSpawner.SpawnRandomFormation();
        }
        else
        {
            AutoCollectEnemies();
        }

        currentEnemy = GetFirstAliveEnemy();
    }

    [ContextMenu("Auto Collect Enemies")]
    public void AutoCollectEnemies()
    {
        if (enemies == null)
            enemies = new List<EnemyUnit>();

        enemies.Clear();

        EnemyUnit[] foundEnemies = FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None);

        foreach (EnemyUnit enemy in foundEnemies)
        {
            if (enemy == null) continue;
            if (!enemy.gameObject.activeInHierarchy) continue;
            if (enemy.currentHp <= 0) continue;

            if (!enemies.Contains(enemy))
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
        {
            // 玩家回合開始：清格擋
            playerUnit.ResetBlock();

            // 玩家回合開始狀態結算，例如 Poison
            playerUnit.OnTurnStart();

            if (playerUnit.currentHp <= 0)
            {
                EndBattle(false);
                yield break;
            }
        }

        Debug.Log("玩家回合開始");

        if (handUIController != null)
            yield return handUIController.DrawCardsAnimatedWithBag(playerDeck, cardsPerTurn);
        else if (playerDeck != null)
            playerDeck.DrawCards(cardsPerTurn);
    }

    public void EndPlayerTurn()
    {
        if (currentPhase != BattlePhase.PlayerTurn)
            return;

        if (isChangingTurn)
            return;

        isChangingTurn = true;

        // 玩家回合結束狀態結算，例如 Weak / Vulnerable / Frail 層數 -1
        if (playerUnit != null)
        {
            playerUnit.OnTurnEnd();

            if (playerUnit.currentHp <= 0)
            {
                EndBattle(false);
                return;
            }
        }

        if (playerDeck != null)
            playerDeck.DiscardHandAtEndTurn();

        RefreshHandUI();

        Debug.Log("玩家回合結束");

        StartCoroutine(EnemyTurnRoutine());
    }

    private IEnumerator EnemyTurnRoutine()
    {
        EnsureEnemiesRegistered();

        StartEnemyTurn();

        if (currentPhase == BattlePhase.BattleEnded)
            yield break;

        yield return new WaitForSeconds(enemyActionDelay);

        if (enemies == null || enemies.Count == 0)
        {
            Debug.LogWarning("[EnemyTurnRoutine] 沒有任何怪物可以行動");
            EndEnemyTurn();
            yield break;
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyUnit enemy = enemies[i];

            if (enemy == null) continue;
            if (!enemy.gameObject.activeInHierarchy) continue;
            if (enemy.currentHp <= 0) continue;

            Debug.Log($"[EnemyTurnRoutine] 怪物行動：{enemy.unitName}");

            currentEnemy = enemy;

            // 怪物攻擊 / 防禦 / 上狀態
            // 傷害修正會在 EnemyDamageActionData → enemy.DealDamageTo(playerUnit, amount) 裡處理
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
    private void EnsureEnemiesRegistered()
    {
        if (enemies == null)
            enemies = new List<EnemyUnit>();

        enemies.RemoveAll(enemy => enemy == null);

        if (enemies.Count == 0)
        {
            AutoCollectEnemies();
        }

        NormalizeEnemyList();

        if (currentEnemy == null)
            currentEnemy = GetFirstAliveEnemy();

        Debug.Log($"[EnsureEnemiesRegistered] enemies.Count = {enemies.Count}, currentEnemy = {(currentEnemy != null ? currentEnemy.unitName : "null")}");
    }
    private void NormalizeEnemyList()
    {
        if (enemies == null)
            enemies = new List<EnemyUnit>();

        List<EnemyUnit> uniqueEnemies = new List<EnemyUnit>();

        foreach (EnemyUnit enemy in enemies)
        {
            if (enemy == null) continue;
            if (!enemy.gameObject.activeInHierarchy) continue;
            if (enemy.currentHp <= 0) continue;

            if (!uniqueEnemies.Contains(enemy))
                uniqueEnemies.Add(enemy);
        }

        enemies = uniqueEnemies;
    }

    public void StartEnemyTurn()
    {
        if (currentPhase == BattlePhase.BattleEnded)
            return;

        currentPhase = BattlePhase.EnemyTurn;

        EnsureEnemiesRegistered();

        // 怪物回合開始：所有怪物清格擋 + 狀態結算
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyUnit enemy = enemies[i];

            if (enemy == null) continue;
            if (!enemy.gameObject.activeInHierarchy) continue;
            if (enemy.currentHp <= 0) continue;

            enemy.ResetBlock();

            // 怪物回合開始狀態結算，例如 Poison
            enemy.OnTurnStart();
        }

        CheckBattleEnd();

        Debug.Log("怪物回合開始");
    }

    public void EndEnemyTurn()
    {
        if (currentPhase == BattlePhase.BattleEnded)
            return;

        // 怪物回合結束狀態結算，例如 Weak / Vulnerable / Frail 層數 -1
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyUnit enemy = enemies[i];

            if (enemy == null) continue;
            if (!enemy.gameObject.activeInHierarchy) continue;
            if (enemy.currentHp <= 0) continue;

            enemy.OnTurnEnd();
        }

        CheckBattleEnd();

        if (currentPhase == BattlePhase.BattleEnded)
            return;

        Debug.Log("怪物回合結束");

        StartPlayerTurn();
    }

    public bool TryPlayCard(CardInstance card, BattleUnit target, CardViewUI playedCardView)
    {
        if (isResolvingCard)
        {
            Debug.Log("卡牌正在結算中");
            return false;
        }

        if (currentPhase != BattlePhase.PlayerTurn)
        {
            Debug.Log("現在不是玩家回合，不能出牌");
            return false;
        }

        if (card == null || card.data == null)
        {
            Debug.LogWarning("[TryPlayCard] card 或 card.data 是 null");
            return false;
        }

        BattleUnit finalTarget = ResolveTarget(card.data.targetType, target);

        if (card.data.targetType == TargetType.SingleEnemy && finalTarget == null)
        {
            Debug.Log("沒有選到敵人");
            return false;
        }

        if (card.data.targetType == TargetType.RandomEnemy && finalTarget == null)
        {
            Debug.Log("沒有可用的隨機敵人");
            return false;
        }

        if (card.data.targetType == TargetType.AllEnemies && GetAliveEnemies().Count == 0)
        {
            Debug.Log("沒有任何敵人可以攻擊");
            return false;
        }

        if (energySystem == null)
        {
            Debug.LogWarning("[TryPlayCard] energySystem 沒有指定");
            return false;
        }

        if (!energySystem.CanSpend(card.currentCost))
        {
            Debug.Log("能量不足");
            return false;
        }

        StartCoroutine(PlayCardRoutine(card, finalTarget, playedCardView));
        return true;
    }
    private IEnumerator PlayCardRoutine(CardInstance card, BattleUnit finalTarget, CardViewUI playedCardView)
    {
        isResolvingCard = true;

        if (card == null || card.data == null)
        {
            isResolvingCard = false;
            yield break;
        }

        energySystem.Spend(card.currentCost);

        bool isTransformCard = HasTransformEffect(card);

        if (playedCardView != null && handUIController != null)
        {
            Transform parent = null;

            if (isTransformCard && transformAnimationController != null)
                parent = transformAnimationController.AnimationRoot;

            handUIController.DetachCardViewForPlay(card, parent);
        }

        if (playerDeck != null)
            playerDeck.OnCardPlayed(card);

        if (!isTransformCard)
            RefreshHandUI();

        Debug.Log($"打出卡牌: {card.data.cardName}");

        if (card.data.targetType == TargetType.AllEnemies)
        {
            List<EnemyUnit> aliveEnemies = GetAliveEnemies();

            for (int enemyIndex = 0; enemyIndex < aliveEnemies.Count; enemyIndex++)
            {
                EnemyUnit enemy = aliveEnemies[enemyIndex];

                if (enemy == null) continue;
                if (!enemy.gameObject.activeInHierarchy) continue;
                if (enemy.currentHp <= 0) continue;

                CardResolveContext enemyContext = new CardResolveContext(
                    playerUnit,
                    enemy,
                    card,
                    this
                );

                for (int effectIndex = 0; effectIndex < card.data.effects.Count; effectIndex++)
                {
                    CardEffectData effect = card.data.effects[effectIndex];

                    if (effect == null)
                        continue;

                    effect.Execute(enemyContext);
                }
            }
        }
        else
        {
            CardResolveContext context = new CardResolveContext(
                playerUnit,
                finalTarget,
                card,
                this
            );

            for (int i = 0; i < card.data.effects.Count; i++)
            {
                CardEffectData effect = card.data.effects[i];

                if (effect == null)
                    continue;

                if (effect is TransformRandomCardByPoolEffectData transformEffect)
                {
                    yield return ResolveTransformCardEffect(
                        transformEffect,
                        context,
                        playedCardView
                    );
                }
                else
                {
                    effect.Execute(context);
                }
            }
        }

        if (!isTransformCard && playedCardView != null)
        {
            Destroy(playedCardView.gameObject);
        }

        CheckBattleEnd();

        isResolvingCard = false;

        yield break;
    }

    private EnemyUnit GetRandomAliveEnemy()
    {
        List<EnemyUnit> aliveEnemies = new List<EnemyUnit>();

        if (enemies == null)
            return null;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyUnit enemy = enemies[i];

            if (enemy == null) continue;
            if (!enemy.gameObject.activeInHierarchy) continue;
            if (enemy.currentHp <= 0) continue;

            aliveEnemies.Add(enemy);
        }

        if (aliveEnemies.Count == 0)
            return null;

        int randomIndex = Random.Range(0, aliveEnemies.Count);
        return aliveEnemies[randomIndex];
    }
    public BattleUnit GetRandomAliveEnemyPublic()
    {
        return GetRandomAliveEnemy();
    }

    private List<EnemyUnit> GetAliveEnemies()
    {
        List<EnemyUnit> aliveEnemies = new List<EnemyUnit>();

        if (enemies == null)
            return aliveEnemies;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyUnit enemy = enemies[i];

            if (enemy == null) continue;
            if (!enemy.gameObject.activeInHierarchy) continue;
            if (enemy.currentHp <= 0) continue;

            aliveEnemies.Add(enemy);
        }

        return aliveEnemies;
    }

    private bool HasTransformEffect(CardInstance card)
    {
        if (card == null || card.data == null || card.data.effects == null)
            return false;

        for (int i = 0; i < card.data.effects.Count; i++)
        {
            if (card.data.effects[i] is TransformRandomCardByPoolEffectData)
                return true;
        }

        return false;
    }
    private IEnumerator ResolveTransformCardEffect(
    TransformRandomCardByPoolEffectData transformEffect,
    CardResolveContext context,
    CardViewUI playedCardView
)
    {
        if (transformAnimationController != null)
        {
            // 1. 變化牌移動到畫面中間
            // 2. 暫停 1 秒，之後可替換成 IK 專屬動畫
            yield return transformAnimationController.MovePlayedCardToCenterAndWait(playedCardView);
        }
        else
        {
            yield return new WaitForSeconds(1f);
        }

        // 3. 暫停後才真正執行變化
        CardTransformResult result = transformEffect.ExecuteTransform(context);

        if (transformAnimationController != null)
        {
            // 4. 包包上方出現被替換牌
            // 5. 向下移動進包包
            // 6. 消失
            yield return transformAnimationController.PlayBagTransformAnimation(result);

            // 7. 變化牌本身消失
            yield return transformAnimationController.FinishPlayedTransformCard(playedCardView);
        }
    }
    private BattleUnit ResolveTarget(TargetType targetType, BattleUnit selectedTarget)
    {
        switch (targetType)
        {
            case TargetType.Self:
                return playerUnit;

            case TargetType.SingleEnemy:
                return selectedTarget;

            case TargetType.RandomEnemy:
                return GetRandomAliveEnemy();

            case TargetType.AllEnemies:
                return null;

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
            yield return handUIController.DrawCardsAnimatedWithBag(playerDeck, amount);
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
        {
            Debug.Log("戰鬥勝利");

            if (autoStartNextBattleOnWin)
            {
                StartCoroutine(StartNextBattleAfterDelay());
            }
        }
        else
        {
            Debug.Log("戰鬥失敗");

            if (optionMenuUI != null)
                optionMenuUI.OpenDeathMenu();
        }
    }

    private IEnumerator StartNextBattleAfterDelay()
    {
        yield return new WaitForSeconds(nextBattleDelay);

        StartNextBattleKeepStateAndDeck();
    }
    public void StartNextBattleKeepStateAndDeck()
    {
        Debug.Log("[BattleManager] 開始下一場戰鬥：保留玩家狀態與牌組");

        StopAllCoroutines();

        currentPhase = BattlePhase.None;
        isChangingTurn = false;

        if (playerDeck != null)
            playerDeck.PrepareForNextBattleKeepDeck();

        if (energySystem != null)
            energySystem.ResetEnergy();

        SpawnEnemiesForBattle();

        RefreshHandUI();

        StartPlayerTurn();
    }
    public void RestartNewGame()
    {
        Debug.Log("[BattleManager] 重新開始新遊戲");

        StopAllCoroutines();

        currentPhase = BattlePhase.None;
        isChangingTurn = false;

        if (playerUnit != null)
            playerUnit.FullResetUnit();

        if (energySystem != null)
            energySystem.ResetEnergy();

        if (playerDeck != null)
            playerDeck.ResetForNewGame();

        SpawnEnemiesForBattle();

        RefreshHandUI();

        StartPlayerTurn();
    }
}