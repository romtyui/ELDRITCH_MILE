using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

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
    public TurnPhaseBannerUI turnPhaseBannerUI;
    [Header("Player Bars UI")]
    public Image hpFillImage;
    public Image sanFillImage;
    // public EnemyStatusBarUI enemyStatusBarUI; // 之後再做

    //[Header("Damage Popup UI")]
    //public DamagePopupUI damagePopupPrefab;
    //public RectTransform damagePopupRoot;
    //public Vector2 damagePopupRandomRange = new Vector2(60f, 30f);

    //[Header("Battle Start Deck Snapshot")]
    //public BattleStartDeckSnapshot battleStartDeckSnapshot = new();

    //[Tooltip("F6 使用。重新載入場景後，是否要套用戰鬥開始前的牌組順序")]
    //public bool pendingRestoreBattleStartDeckSnapshot;

    [Header("Runtime")]
    public BattlePhase currentPhase = BattlePhase.None;

    private bool isCheckingBattleEndDelayed;
    [Header("Turn Count")]
    public int battleTurnNumber = 0;

    [Header("Enemy Spawning")]
    public EnemyFormationSpawner enemyFormationSpawner;

    private bool isChangingTurn;

    [Header("Relics")]
    public RelicsRuntime relicsRuntime;

    [Header("Special Animation")]
    public CardTransformAnimationController transformAnimationController;

    [Header("God Card Corruption Animation")]
    public GodCardCorruptionAnimationController godCardCorruptionAnimationController;

    [Header("General Card Play Animation")]
    public GeneralCardPlayAnimationController generalCardPlayAnimationController;

    [Header("Card Hit Effect")]
    public CardHitEffectController cardHitEffectController;

    private bool isResolvingCard;

    /*
     * 玩家回合的抽牌、回合提示等流程全部完成，
     * 正式可以開始操作時發送。
     */
    public event Action PlayerInputReady;
    private void Start()
    {
        StartBattle();
    }

    public void StartBattle()
    {
        currentPhase = BattlePhase.None;
        isChangingTurn = false;
        isResolvingCard = false;
        battleTurnNumber = 0;

        bool restoreBattleStartDeckSnapshot =
            RunStateManager.Instance != null &&
            RunStateManager.Instance.pendingRestoreBattleStartDeckSnapshot;

        bool hasRunState =
            RunStateManager.Instance != null &&
            RunStateManager.Instance.hasSavedRunState;

        if (restoreBattleStartDeckSnapshot)
        {
            RunStateManager.Instance.ApplyBattleStartDeckSnapshot(
                playerUnit,
                energySystem,
                playerDeck
            );
        }
        else
        {
            if (hasRunState)
            {
                // 先把 F5 保存的牌組塞回 startingDeck
                RunStateManager.Instance.ApplyToBattle(
                    playerUnit,
                    energySystem,
                    playerDeck
                );
            }

            if (playerDeck != null)
                playerDeck.InitializeDeck();

            if (!hasRunState)
            {
                if (energySystem != null)
                    energySystem.ResetEnergy();
            }

            if (hasRunState)
            {
                // InitializeDeck 之後再套一次，確保 HP / SAN 不被覆蓋
                RunStateManager.Instance.ApplyToBattle(
                    playerUnit,
                    energySystem,
                    playerDeck
                );
            }

            if (RunStateManager.Instance != null)
            {
                RunStateManager.Instance.SaveBattleStartDeckSnapshot(
                    playerUnit,
                    energySystem,
                    playerDeck
                );
            }
        }

        SpawnEnemiesForBattle();

        RefreshPlayerBarsUI();
        RefreshStatusUI();

        // =========================================================
        // Relics：Battle Start
        // =========================================================

        TriggerRelics(RelicsTriggerType.BattleStart);


        // =========================================================
        // 開始玩家回合
        // =========================================================

        StartPlayerTurn();


        TutorialEventBus.Raise(BattleTutorialSignals.BattleStarted);


        if (playerDeck != null)
        {
            Debug.Log($"{playerDeck.Hand.Count}");
        }
    }
    private void TriggerRelics(RelicsTriggerType triggerType,CardInstance playedCard = null)
    {
        if (relicsRuntime == null)
            return;


        RelicsUseContext context = new RelicsUseContext(this,playerUnit,triggerType,playedCard);


        relicsRuntime.Trigger(triggerType,context);


        // 遺物可能造成回血、護盾、狀態等變化。
        RefreshPlayerBarsUI();
        RefreshStatusUI();
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

        battleTurnNumber++;

        if (turnEndButtonAnimatorUI != null)
            turnEndButtonAnimatorUI.SetPlayerTurnIdle();

        if (turnPhaseBannerUI != null)
            yield return turnPhaseBannerUI.ShowPlayerTurn(battleTurnNumber);

        if (playerUnit != null)
        {
            // =====================================================
            // 原本玩家回合開始 Status
            // =====================================================

            playerUnit.ResetBlock();

            playerUnit.OnTurnStart();


            // =====================================================
            // Relics：Player Turn Start
            //
            // 放在 ResetBlock / OnTurnStart 後面。
            //
            // 例如：
            // 每回合開始獲得 5 Block
            //
            // 就不會剛獲得馬上被 ResetBlock 清掉。
            // =====================================================

            TriggerRelics(
                RelicsTriggerType.PlayerTurnStart
            );


            // =====================================================
            // 原本 UI
            // =====================================================

            RefreshPlayerBarsUI();
            RefreshStatusUI();


            // =====================================================
            // 原本死亡判定
            // =====================================================

            if (playerUnit.currentHp <= 0)
            {
                EndBattle(false);

                yield break;
            }
        }

        Debug.Log($"玩家回合開始，第 {battleTurnNumber} 回合");

        if (handUIController != null)
            yield return handUIController.DrawCardsAnimatedWithBag(playerDeck, cardsPerTurn);
        else if (playerDeck != null)
            playerDeck.DrawCards(cardsPerTurn);

        TutorialEventBus.Raise(BattleTutorialSignals.PlayerTurnStarted);

        if (turnPhaseBannerUI != null)
            yield return turnPhaseBannerUI.Hide();

        /*
         * 抽牌動畫完成、玩家回合提示關閉後，
         * 此刻才視為玩家正式可以操作。
         */
        PlayerInputReady?.Invoke();

        Debug.Log(
            $"[BattleManager] 玩家已可操作，第 {battleTurnNumber} 回合"
        );
    }
    public void EndPlayerTurn()
    {
        TutorialEventBus.Raise("Battle_EndTurnPressed");
        if (currentPhase != BattlePhase.PlayerTurn)
            return;

        if (isChangingTurn)
            return;

        if (isResolvingCard)
        {
            Debug.Log("[BattleManager] 卡牌或神牌動畫仍在結算中，不能切換到敵方回合");
            return;
        }
        TutorialEventBus.Raise(BattleTutorialSignals.TurnEndButtonPressed);
        StartCoroutine(EndPlayerTurnRoutine());


        //if (currentPhase != BattlePhase.PlayerTurn)
        //    return;

        //if (isChangingTurn)
        //    return;

        //isChangingTurn = true;

        //if (turnEndButtonAnimatorUI != null)
        //    turnEndButtonAnimatorUI.SetEnemyTurnIdle();

        //// ���a�^�X�������A����A�Ҧp Weak / Vulnerable / Frail �h�� -1
        //if (playerUnit != null)
        //{
        //    playerUnit.OnTurnEnd();

        //    RefreshPlayerBarsUI();


        //    RefreshStatusUI();

        //    if (playerUnit.currentHp <= 0)
        //    {
        //        EndBattle(false);
        //        return;
        //    }
        //}

        //if (playerDeck != null)
        //    playerDeck.DiscardHandAtEndTurn();

        //RefreshHandUI();

        //Debug.Log("���a�^�X����");

        //StartCoroutine(EnemyTurnRoutine());
    }
    private IEnumerator EndPlayerTurnRoutine()
    {
        isChangingTurn = true;


        if (turnEndButtonAnimatorUI != null)
        {
            turnEndButtonAnimatorUI.SetEnemyTurnIdle();
        }


        // =========================================================
        // Relics：Player Turn End
        // =========================================================
        //
        // 先觸發遺物的回合結束效果。
        //
        // 例如：
        // 回合結束回血
        // 回合結束加盾
        // 回合結束造成傷害
        //
        // =========================================================

        TriggerRelics( RelicsTriggerType.PlayerTurnEnd);


        // =========================================================
        // 原本玩家 Status 回合結束
        // =========================================================

        if (playerUnit != null)
        {
            playerUnit.OnTurnEnd();


            RefreshPlayerBarsUI();

            RefreshStatusUI();


            if (playerUnit.currentHp <= 0)
            {
                EndBattle(false);

                yield break;
            }
        }


        // =========================================================
        // 原本棄牌
        // =========================================================

        if (playerDeck != null)
        {
            playerDeck.DiscardHandAtEndTurn();
        }


        RefreshHandUI();


        Debug.Log( "玩家回合結束");


        // =========================================================
        // 你原本後面的 Tutorial / EnemyTurn 流程
        // 請全部保留
        // =========================================================

        TutorialEventBus.Raise(BattleTutorialSignals.TurnEnded);


        if (turnPhaseBannerUI != null)
        {
            yield return turnPhaseBannerUI.ShowEnemyTurn();
        }


        yield return EnemyTurnRoutine();
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

        TutorialEventBus.Raise(BattleTutorialSignals.EnemyTurnStarted);

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

    public bool TryPlayCard(CardInstance card, BattleUnit target, CardViewUI playedCardView, Vector3? releaseWorldPosition = null)
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
            Debug.Log("沒有指定敵人");
            return false;
        }

        if (card.data.targetType == TargetType.RandomEnemy && finalTarget == null)
        {
            Debug.Log("沒有可用的隨機敵人");
            return false;
        }

        if (card.data.targetType == TargetType.AllEnemies && GetAliveEnemies().Count == 0)
        {
            Debug.Log("沒有存活敵人可以攻擊");
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

        StartCoroutine(PlayCardRoutine(card, finalTarget, playedCardView, releaseWorldPosition));
        return true;
    }
    private IEnumerator PlayCardRoutine( CardInstance card,BattleUnit finalTarget, CardViewUI playedCardView,  Vector3? releaseWorldPosition)
    {
        /*
         * =========================================================
         * 開始結算卡牌
         * =========================================================
         */

        isResolvingCard = true;

        if (card == null || card.data == null)
        {
            isResolvingCard = false;
            yield break;
        }


        /*
         * =========================================================
         * 扣除卡牌費用
         * =========================================================
         */

        energySystem.Spend(card.currentCost);

        RefreshPlayerBarsUI();


        /*
         * =========================================================
         * 判斷是不是神牌 / 轉化牌
         * =========================================================
         */

        bool isTransformCard =  HasTransformEffect(card);


        /*
         * =========================================================
         * 成功打出的 CardView
         * 先從手牌 UI 脫離
         * =========================================================
         *
         * 注意：
         *
         * 一般卡和神牌都一定會進來。
         *
         * 差別只有它們要被移到哪個 AnimationRoot。
         * =========================================================
         */

        if (playedCardView != null &&
            handUIController != null)
        {
            Transform parent = null;


            /*
             * -----------------------------------------------------
             * 神牌 / 轉化牌
             * -----------------------------------------------------
             */

            if (isTransformCard)
            {
                GodCardAnimationData animationData = null;

                if (card != null &&
                    card.data != null)
                {
                    animationData =
                        card.data.godCardAnimation;
                }


                /*
                 * 有神牌專屬污染動畫。
                 */
                if (animationData != null &&
                    godCardCorruptionAnimationController != null)
                {
                    parent =
                        godCardCorruptionAnimationController
                            .AnimationRoot;
                }

                /*
                 * 沒有專屬神牌動畫，
                 * 使用原本的轉化動畫 Root。
                 */
                else if (
                    transformAnimationController != null
                )
                {
                    parent =
                        transformAnimationController
                            .AnimationRoot;
                }
            }


            /*
             * -----------------------------------------------------
             * 一般卡
             * -----------------------------------------------------
             */

            else
            {
                if (generalCardPlayAnimationController != null)
                {
                    parent =
                        generalCardPlayAnimationController
                            .AnimationRoot;
                }
            }


            /*
             * -----------------------------------------------------
             * 不論一般卡還是神牌，
             * 都必須先離開 HandUI。
             * -----------------------------------------------------
             */

            handUIController.DetachCardViewForPlay(
                card,
                parent
            );
        }


        /*
         * =========================================================
         * 原本牌組邏輯
         * =========================================================
         *
         * 從 hand 移除，
         * 並依卡牌規則進入棄牌 / 消耗區。
         * =========================================================
         */

        if (playerDeck != null)
        {
            playerDeck.OnCardPlayed(card);
        }


        Debug.Log(
            $"打出卡牌: {card.data.cardName}"
        );


        /*
         * =========================================================
         * 一般卡成功出牌 Intro
         * =========================================================
         *
         * 神牌 / 轉化牌不會播放這個。
         *
         * 一般卡：
         *
         * 飛到展示位置
         * → 放大
         * → Punch
         * → 停留
         * =========================================================
         */

        if (!isTransformCard && playedCardView != null && generalCardPlayAnimationController != null)
        {
            /*
             * SingleEnemy：
             * 完全照舊，使用 Inspector 固定的 playedCardPosition。
             */
            if (card.data.targetType == TargetType.SingleEnemy)
            {
                yield return generalCardPlayAnimationController.PlayIntro(playedCardView);
            }

            /*
             * Direct Drag：
             * 使用玩家放開牌時的位置。
             */
            else if (releaseWorldPosition.HasValue)
            {
                yield return generalCardPlayAnimationController.PlayIntroAtWorldPosition(
                    playedCardView,
                    releaseWorldPosition.Value
                );
            }

            /*
             * 保底：
             * 如果不是從 CardDragUI 出牌，
             * 沒有 Release Position，
             * 就照原本固定位置。
             */
            else
            {
                yield return generalCardPlayAnimationController.PlayIntro(playedCardView);
            }
        }


        /*
         * =========================================================
         * 更新剩餘手牌
         * =========================================================
         *
         * 原本 RefreshHandUI 功能保留。
         *
         * 但是改成一般卡 Intro 播完後才執行。
         *
         * 避免正在播放動畫的 CardView
         * 被手牌刷新提前處理掉。
         * =========================================================
         */

        if (!isTransformCard)
        {
            RefreshHandUI();
        }


        /*
         * =========================================================
         * 卡牌 Effects 結算
         * =========================================================
         */


        /*
         * ---------------------------------------------------------
         * AllEnemies
         * ---------------------------------------------------------
         *
         * 每一隻存活敵人
         * 都完整執行這張卡的 Effects。
         * ---------------------------------------------------------
         */

        if (card.data.targetType == TargetType.AllEnemies)
        {
            List<EnemyUnit> aliveEnemies = GetAliveEnemies();

            /*
             * =========================================================
             * AllEnemies 命中特效
             * =========================================================
             *
             * 所有敵人同時生成特效。
             * =========================================================
             */

            if (card.data.hitEffect != null && cardHitEffectController != null)
            {
                bool hitSoundPlayed = false;
                for (int effectTargetIndex = 0; effectTargetIndex < aliveEnemies.Count; effectTargetIndex++)
                {
                    EnemyUnit effectTarget = aliveEnemies[effectTargetIndex];

                    if (effectTarget == null)
                        continue;

                    if (!effectTarget.gameObject.activeInHierarchy)
                        continue;

                    if (effectTarget.currentHp <= 0)
                        continue;


                    /*
                     * 第一個有效敵人播放：
                     *
                     * VFX + SFX
                     *
                     * 其他敵人只播放：
                     *
                     * VFX
                     */
                    cardHitEffectController.SpawnEffectOnTarget(
                        card.data.hitEffect,
                        effectTarget,
                        !hitSoundPlayed
                    );


                    hitSoundPlayed = true;
                }


                /*
                 * 所有 VFX 都生成後，
                 * 統一等待命中時間。
                 */
                yield return
                    cardHitEffectController.WaitForImpact(
                        card.data.hitEffect
                    );
            }
            for (
                int enemyIndex = 0;
                enemyIndex < aliveEnemies.Count;
                enemyIndex++
            )
            {
                EnemyUnit enemy =
                    aliveEnemies[enemyIndex];


                if (enemy == null)
                    continue;

                if (!enemy.gameObject.activeInHierarchy)
                    continue;

                if (enemy.currentHp <= 0)
                    continue;


                CardResolveContext enemyContext =
                    new CardResolveContext(
                        playerUnit,
                        enemy,
                        card,
                        this
                    );


                /*
                 * 這隻敵人完整執行一次
                 * 卡牌全部 Effects。
                 */

                for (
                    int effectIndex = 0;
                    effectIndex < card.data.effects.Count;
                    effectIndex++
                )
                {
                    CardEffectData effect =
                        card.data.effects[effectIndex];


                    if (effect == null)
                        continue;


                    effect.Execute(
                        enemyContext
                    );
                }
            }
        }


        /*
         * ---------------------------------------------------------
         * 其他 TargetType
         * ---------------------------------------------------------
         */

        else
        {
            /*
             * =========================================================
             * 卡牌命中特效
             * =========================================================
             *
             * SingleEnemy
             * RandomEnemy
             * Self
             *
             * 都已經有 finalTarget。
             *
             * None 則沒有角色目標，
             * 改播在畫面中央。
             * =========================================================
             */

            if (card.data.hitEffect != null &&
                cardHitEffectController != null)
            {
                if (card.data.targetType == TargetType.None)
                {
                    yield return
                        cardHitEffectController.PlayAtCenter(
                            card.data.hitEffect
                        );
                }
                else if (
                    card.data.targetType == TargetType.SingleEnemy ||
                    card.data.targetType == TargetType.RandomEnemy ||
                    card.data.targetType == TargetType.Self
                )
                {
                    yield return
                        cardHitEffectController.PlayOnTarget(
                            card.data.hitEffect,
                            finalTarget
                        );
                }
            }

            /*
             * 原本 CardResolveContext 保留。
             */
            CardResolveContext context =
                new CardResolveContext(
                    playerUnit,
                    finalTarget,
                    card,
                    this
                );


            for (
                int i = 0;
                i < card.data.effects.Count;
                i++
            )
            {
                CardEffectData effect =
                    card.data.effects[i];


                if (effect == null)
                    continue;


                /*
                 * -------------------------------------------------
                 * 神牌 / 卡牌轉化效果
                 * -------------------------------------------------
                 *
                 * 保留你原本需要等待動畫完成的流程。
                 * -------------------------------------------------
                 */

                if (
                    effect is
                    TransformRandomCardByPoolEffectData
                        transformEffect
                )
                {
                    yield return
                        ResolveTransformCardEffect(
                            transformEffect,
                            context,
                            playedCardView
                        );
                }


                /*
                 * -------------------------------------------------
                 * 普通 CardEffect
                 * -------------------------------------------------
                 */

                else
                {
                    effect.Execute(
                        context
                    );
                }
            }
        }


        /*
         * =========================================================
         * 一般卡 Outro
         * =========================================================
         *
         * 神牌 / 轉化牌使用自己原本的收尾。
         *
         * 一般卡：
         *
         * Effects 完成
         * → 縮小
         * → 淡出
         * → Destroy
         * =========================================================
         */

        if (!isTransformCard &&
            playedCardView != null)
        {
            /*
             * 有一般卡動畫 Controller。
             */
            if (
                generalCardPlayAnimationController != null
            )
            {
                yield return
                    generalCardPlayAnimationController.PlayOutro(
                        playedCardView
                    );
            }


            /*
             * 原本 Destroy 功能保留。
             *
             * 只是從「直接 Destroy」
             * 改成「Outro 播完再 Destroy」。
             */
            if (playedCardView != null)
            {
                Destroy(
                    playedCardView.gameObject
                );
            }
        }


        /*
         * =========================================================
         * 卡牌結算完成後 UI
         * =========================================================
         */

        RefreshPlayerBarsUI();

        RefreshStatusUI();


        // =========================================================
        // Relics：Card Played
        //
        // 整張牌的效果與動畫都已經處理完後，
        // 才算真正的 CardPlayed。
        // =========================================================

        TriggerRelics( RelicsTriggerType.CardPlayed, card);


        /*
         * =========================================================
         * 勝負判定
         * =========================================================
         */

        RequestCheckBattleEnd();


        /*
         * =========================================================
         * 新手教學 Signal
         * =========================================================
         *
         * 你原本兩個 Signal 全部保留。
         * =========================================================
         */

        TutorialEventBus.Raise(BattleTutorialSignals.CardPlayed);

        TutorialEventBus.Raise("Battle_CardPlayed");


        /*
         * =========================================================
         * 最重要：
         * 卡牌完整結算結束
         * =========================================================
         *
         * 這個現在已經不會被
         *
         * playedCardView != null
         * handUIController != null
         *
         * 等 UI 條件包住。
         * =========================================================
         */

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

        if (handUIController != null)
            handUIController.RefreshCardDescriptionsOnly();
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
        battleTurnNumber = 0;

        RefreshHandUI();
        RefreshPlayerBarsUI();
        RefreshStatusUI();

        if (playerWin)
        {
            Debug.Log("戰鬥勝利");

            CommitCurrentFormation();

            if (autoStartNextBattleOnWin)
            {
                StartCoroutine(StartNextBattleAfterDelay());
            }
            else
            {
                Debug.Log("[BattleManager] autoStartNextBattleOnWin = false，戰鬥勝利後關閉 BattleManager 物件");

                gameObject.SetActive(false);
            }
            Debug.Log("[BattleManager] 戰鬥勝利");

            if (RunStateManager.Instance != null)
            {
                RunStateManager.Instance.SaveFromBattle(
                    playerUnit,
                    energySystem,
                    playerDeck
                );

                RunStateManager.Instance.ClearReservedFormation();

                Debug.Log("[BattleManager] 戰鬥勝利：已保存玩家進度，並清除保留怪物組");
            }

        }
        else
        {
            Debug.Log("戰鬥失敗");

            if (optionMenuUI != null)
                optionMenuUI.OpenDeathMenu();
        }
    }
    private void CommitCurrentFormation()
    {
        if (RunStateManager.Instance != null)
        {
            RunStateManager.Instance.ClearReservedFormation();
            Debug.Log("[BattleManager] 戰鬥勝利，清除保留怪物組，下次會重新抽怪");
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
        isResolvingCard = false;
        battleTurnNumber = 0;

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
        isResolvingCard = false;
        battleTurnNumber = 0;

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
    //public void ShowDamagePopup(int damage, RectTransform targetRect, Vector2 offset)
    //{
    //    if (damage <= 0)
    //        return;

    //    if (damagePopupPrefab == null)
    //    {
    //        Debug.LogWarning("[DamagePopup] damagePopupPrefab 沒有指定");
    //        return;
    //    }

    //    if (damagePopupRoot == null)
    //    {
    //        Debug.LogWarning("[DamagePopup] damagePopupRoot 沒有指定");
    //        return;
    //    }

    //    if (targetRect == null)
    //    {
    //        Debug.LogWarning("[DamagePopup] targetRect 是 null");
    //        return;
    //    }

    //    if (!damagePopupRoot.gameObject.scene.IsValid())
    //    {
    //        Debug.LogError("[DamagePopup] damagePopupRoot 不是場景中的物件，請拖 Hierarchy 裡 Canvas 底下的 DamagePopupRoot。");
    //        return;
    //    }

    //    Canvas rootCanvas = damagePopupRoot.GetComponentInParent<Canvas>();

    //    Camera uiCamera = null;

    //    if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
    //        uiCamera = rootCanvas.worldCamera;

    //    Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(
    //        uiCamera,
    //        targetRect.position
    //    );

    //    screenPos += offset;

    //    Vector2 randomOffset = new Vector2(
    //        Random.Range(-damagePopupRandomRange.x, damagePopupRandomRange.x),
    //        Random.Range(-damagePopupRandomRange.y, damagePopupRandomRange.y)
    //    );

    //    screenPos += randomOffset;

    //    DamagePopupUI popup = Instantiate(damagePopupPrefab, damagePopupRoot);
    //    popup.Setup(damage, screenPos);
    //}
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