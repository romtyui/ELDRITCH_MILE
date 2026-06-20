using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    public PlayerStatusBarUI playerStatusBarUI;
    public TurnEndButtonAnimatorUI turnEndButtonAnimatorUI;
    [Header("Player Bars UI")]
    public Image hpFillImage;
    public Image sanFillImage;
    // public EnemyStatusBarUI enemyStatusBarUI; // 之後再做

    [Header("Runtime")]
    public BattlePhase currentPhase = BattlePhase.None;

    private bool isCheckingBattleEndDelayed;

    [Header("Enemy Spawning")]
    public EnemyFormationSpawner enemyFormationSpawner;

    private bool isChangingTurn;

    [Header("Special Animation")]
    public CardTransformAnimationController transformAnimationController;

    [Header("God Card Corruption Animation")]
    public GodCardCorruptionAnimationController godCardCorruptionAnimationController;

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

        RefreshStatusUI();

        StartPlayerTurn();

        if (playerDeck != null)
            Debug.Log($"{playerDeck.Hand.Count}");
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

        Debug.Log($"[BattleManager] �۰ʧ�� {enemies.Count} ���Ǫ��A�ثe�ؼСG{(currentEnemy != null ? currentEnemy.unitName : "null")}");
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
        if (turnEndButtonAnimatorUI != null)
            turnEndButtonAnimatorUI.SetPlayerTurnIdle();

        if (playerUnit != null)
        {
            playerUnit.ResetBlock();
            playerUnit.OnTurnStart();

            RefreshPlayerBarsUI();

            RefreshStatusUI();

            if (playerUnit.currentHp <= 0)
            {
                EndBattle(false);
                yield break;
            }
        }

        Debug.Log("���a�^�X�}�l");

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

        if (turnEndButtonAnimatorUI != null)
            turnEndButtonAnimatorUI.SetEnemyTurnIdle();

        // ���a�^�X�������A����A�Ҧp Weak / Vulnerable / Frail �h�� -1
        if (playerUnit != null)
        {
            playerUnit.OnTurnEnd();

            RefreshPlayerBarsUI();


            RefreshStatusUI();

            if (playerUnit.currentHp <= 0)
            {
                EndBattle(false);
                return;
            }
        }

        if (playerDeck != null)
            playerDeck.DiscardHandAtEndTurn();

        RefreshHandUI();

        Debug.Log("���a�^�X����");

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
            Debug.LogWarning("[EnemyTurnRoutine] �S������Ǫ��i�H���");
            EndEnemyTurn();
            yield break;
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyUnit enemy = enemies[i];

            if (enemy == null) continue;
            if (!enemy.gameObject.activeInHierarchy) continue;
            if (enemy.currentHp <= 0) continue;

            Debug.Log($"[EnemyTurnRoutine] �Ǫ���ʡG{enemy.unitName}");

            currentEnemy = enemy;

            EnemyAnimationType animationType = enemy.GetCurrentIntentAnimationType();

            yield return enemy.PlayActionAnimation(animationType);
            // �Ǫ����� / ���m / �W���A
            // �ˮ`�ץ��|�b EnemyDamageActionData �� enemy.DealDamageTo(playerUnit, amount) �̳B�z
            enemy.ExecuteTurn(playerUnit, this);

            RefreshPlayerBarsUI();


            RefreshStatusUI();

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

        // �Ǫ��^�X�}�l�G�Ҧ��Ǫ��M��� + ���A����
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyUnit enemy = enemies[i];

            if (enemy == null) continue;
            if (!enemy.gameObject.activeInHierarchy) continue;
            if (enemy.currentHp <= 0) continue;

            enemy.ResetBlock();

            // �Ǫ��^�X�}�l���A����A�Ҧp Poison
            enemy.OnTurnStart();
        }

        RefreshStatusUI();


        RequestCheckBattleEnd();

        Debug.Log("�Ǫ��^�X�}�l");
    }

    public void EndEnemyTurn()
    {
        if (currentPhase == BattlePhase.BattleEnded)
            return;

        // �Ǫ��^�X�������A����A�Ҧp Weak / Vulnerable / Frail �h�� -1
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyUnit enemy = enemies[i];

            if (enemy == null) continue;
            if (!enemy.gameObject.activeInHierarchy) continue;
            if (enemy.currentHp <= 0) continue;

            enemy.OnTurnEnd();
        }

        RequestCheckBattleEnd();

        if (currentPhase == BattlePhase.BattleEnded)
            return;

        Debug.Log("�Ǫ��^�X����");

        StartPlayerTurn();
    }

    public bool TryPlayCard(CardInstance card, BattleUnit target, CardViewUI playedCardView)
    {
        if (isResolvingCard)
        {
            Debug.Log("�d�P���b���⤤");
            return false;
        }

        if (currentPhase != BattlePhase.PlayerTurn)
        {
            Debug.Log("�{�b���O���a�^�X�A����X�P");
            return false;
        }

        if (card == null || card.data == null)
        {
            Debug.LogWarning("[TryPlayCard] card �� card.data �O null");
            return false;
        }

        BattleUnit finalTarget = ResolveTarget(card.data.targetType, target);

        if (card.data.targetType == TargetType.SingleEnemy && finalTarget == null)
        {
            Debug.Log("�S�����ĤH");
            return false;
        }

        if (card.data.targetType == TargetType.RandomEnemy && finalTarget == null)
        {
            Debug.Log("�S���i�Ϊ��H���ĤH");
            return false;
        }

        if (card.data.targetType == TargetType.AllEnemies && GetAliveEnemies().Count == 0)
        {
            Debug.Log("�S������ĤH�i�H����");
            return false;
        }

        if (energySystem == null)
        {
            Debug.LogWarning("[TryPlayCard] energySystem �S�����w");
            return false;
        }

        if (!energySystem.CanSpend(card.currentCost))
        {
            Debug.Log("��q����");
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

        RefreshPlayerBarsUI();

        bool isTransformCard = HasTransformEffect(card);

        if (playedCardView != null && handUIController != null)
        {
            Transform parent = null;

            if (isTransformCard)
            {
                GodCardAnimationData animationData = null;

                if (card != null && card.data != null)
                    animationData = card.data.godCardAnimation;

                if (animationData != null && godCardCorruptionAnimationController != null)
                    parent = godCardCorruptionAnimationController.AnimationRoot;
                else if (transformAnimationController != null)
                    parent = transformAnimationController.AnimationRoot;
            }

            handUIController.DetachCardViewForPlay(card, parent);
        }

        if (playerDeck != null)
            playerDeck.OnCardPlayed(card);

        if (!isTransformCard)
            RefreshHandUI();

        Debug.Log($"���X�d�P: {card.data.cardName}");

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

        RefreshPlayerBarsUI();

        RefreshStatusUI();

        RequestCheckBattleEnd();

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
        if (transformEffect == null)
            yield break;

        GodCardAnimationData animationData = null;

        if (context != null && context.card != null && context.card.data != null)
            animationData = context.card.data.godCardAnimation;

        // 有專屬動畫：走新的 IK 專屬動畫流程
        if (animationData != null && godCardCorruptionAnimationController != null)
        {
            yield return godCardCorruptionAnimationController.PlayGodCorruptionSequence(
                playedCardView,
                transformEffect,
                context,
                animationData
            );

            yield break;
        }

        // 沒有專屬動畫：走舊預設包包動畫流程
        yield return ResolveDefaultCorruptionAnimation(
            transformEffect,
            context,
            playedCardView
        );
    }
    private IEnumerator ResolveDefaultCorruptionAnimation(
    TransformRandomCardByPoolEffectData transformEffect,
    CardResolveContext context,
    CardViewUI playedCardView
)
    {
        if (transformAnimationController != null)
        {
            yield return transformAnimationController.MovePlayedCardToCenterAndWait(playedCardView);
        }
        else
        {
            yield return new WaitForSeconds(1f);
        }

        CardTransformResult result = transformEffect.ExecuteTransform(context);

        if (transformAnimationController != null)
        {
            yield return transformAnimationController.PlayBagTransformAnimation(result);
            yield return transformAnimationController.FinishPlayedTransformCard(playedCardView);
        }
        else
        {
            if (playedCardView != null)
                Destroy(playedCardView.gameObject);
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
        RefreshPlayerBarsUI();
    }

    private void RefreshHandUI()
    {
        if (handUIController != null)
            handUIController.RefreshHandUI();
    }
    private void RefreshPlayerBarsUI()
    {
        RefreshHpBarUI();
        RefreshSanBarUI();
    }

    private void RefreshHpBarUI()
    {
        if (hpFillImage == null)
            return;

        if (playerUnit == null)
        {
            hpFillImage.fillAmount = 0f;
            return;
        }

        float maxHp = Mathf.Max(1, playerUnit.maxHp);
        hpFillImage.fillAmount = Mathf.Clamp01(playerUnit.currentHp / maxHp);
    }

    private void RefreshSanBarUI()
    {
        if (sanFillImage == null)
            return;

        if (energySystem == null)
        {
            sanFillImage.fillAmount = 0f;
            return;
        }

        float maxSan = Mathf.Max(1, energySystem.maxEnergy);
        sanFillImage.fillAmount = Mathf.Clamp01(energySystem.currentEnergy / maxSan);
    }
    public void RefreshStatusUI()
    {
        if (playerStatusBarUI != null)
            playerStatusBarUI.Refresh();
    }
    public void RequestCheckBattleEnd()
    {
        if (currentPhase == BattlePhase.BattleEnded)
            return;

        if (isCheckingBattleEndDelayed)
            return;

        StartCoroutine(CheckBattleEndDelayedRoutine());
    }

    private IEnumerator CheckBattleEndDelayedRoutine()
    {
        isCheckingBattleEndDelayed = true;

        yield return null;

        isCheckingBattleEndDelayed = false;

        CheckBattleEnd();
    }
    private void CheckBattleEnd()
    {
        if (currentPhase == BattlePhase.BattleEnded)
            return;

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

        bool hasAliveEnemy = false;
        bool hasDeathAnimationPlaying = false;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyUnit enemy = enemies[i];

            if (enemy == null)
                continue;

            if (enemy.gameObject.activeInHierarchy && enemy.currentHp > 0)
            {
                hasAliveEnemy = true;
                break;
            }

            if (enemy.IsDeathAnimationPlaying)
            {
                hasDeathAnimationPlaying = true;
            }
        }

        if (hasAliveEnemy)
        {
            currentEnemy = GetFirstAliveEnemy();
            return;
        }

        if (hasDeathAnimationPlaying)
        {
            Debug.Log("[CheckBattleEnd] 所有怪物 HP 已歸 0，但仍有死亡動畫播放中，延後勝利檢查");
            RequestCheckBattleEnd();
            return;
        }

        EndBattle(true);
    }
    private void EndBattle(bool playerWin)
    {
        if (currentPhase == BattlePhase.BattleEnded && isChangingTurn)
            return;

        currentPhase = BattlePhase.BattleEnded;
        isChangingTurn = true;
        isResolvingCard = false;
        isCheckingBattleEndDelayed = false;

        RefreshHandUI();
        RefreshPlayerBarsUI();
        RefreshStatusUI();

        if (playerWin)
        {
            Debug.Log("戰鬥勝利");

            if (autoStartNextBattleOnWin)
            {
                StartCoroutine(StartNextBattleAfterDelay());
            }
            else
            {
                Debug.Log("[BattleManager] autoStartNextBattleOnWin = false，戰鬥勝利後關閉 BattleManager 物件");

                gameObject.SetActive(false);
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
        Debug.Log("[BattleManager] �}�l�U�@���԰��G�O�d���a���A�P�P��");

        StopAllCoroutines();

        currentPhase = BattlePhase.None;
        isChangingTurn = false;

        if (playerDeck != null)
            playerDeck.PrepareForNextBattleKeepDeck();

        if (energySystem != null)
            energySystem.ResetEnergy();

        SpawnEnemiesForBattle();

        RefreshPlayerBarsUI();

        RefreshHandUI();

        RefreshStatusUI();

        StartPlayerTurn();
    }

    public void StartNextBattleKeepHpSanAndDeck()
    {
        Debug.Log("[BattleManager] 重新開啟後開始下一場戰鬥：保留 HP / SAN / 牌組");

        StopAllCoroutines();

        currentPhase = BattlePhase.None;
        isChangingTurn = false;
        isResolvingCard = false;

        if (playerDeck != null)
            playerDeck.PrepareForNextBattleKeepDeck();

        // 注意：這裡不要 ResetEnergy
        // 因為你要求 SAN 值不重製
        // if (energySystem != null)
        //     energySystem.ResetEnergy();

        SpawnEnemiesForBattle();

        RefreshPlayerBarsUI();
        RefreshHandUI();
        RefreshStatusUI();

        StartPlayerTurn();
    }
    public void RestartNewGame()
    {
        Debug.Log("[BattleManager] ���s�}�l�s�C��");

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

        RefreshPlayerBarsUI();

        RefreshHandUI();

        RefreshStatusUI();

        StartPlayerTurn();
    }
    public void AddCardToHand(CardData cardData)
    {
        if (playerDeck == null)
            return;

        playerDeck.AddCardToHand(cardData);
        RefreshHandUI();
    }

    public int GetUsedTokenCount(string tokenId)
    {
        if (playerDeck == null)
            return 0;

        return playerDeck.GetUsedTokenCount(tokenId);
    }

    public int CountTokenInHand(string tokenId)
    {
        if (playerDeck == null)
            return 0;

        return playerDeck.CountTokenInHand(tokenId);
    }
}