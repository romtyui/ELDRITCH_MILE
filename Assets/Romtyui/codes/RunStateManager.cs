using System;

using UnityEngine;
using System.Collections.Generic;

[Serializable]
public class BattleStartDeckSnapshot
{
    public bool hasSnapshot;

    public int playerMaxHp;
    public int playerCurrentHp;

    public int maxEnergy;
    public int currentEnergy;

    public List<StatusSnapshotEntry> playerStatuses = new();

    public List<CardData> drawPileOrder = new();
}

[Serializable]
public class StatusSnapshotEntry
{
    public StatusType statusType;
    public int amount;

    public StatusSnapshotEntry(StatusType statusType, int amount)
    {
        this.statusType = statusType;
        this.amount = amount;
    }
}



public class RunStateManager : MonoBehaviour
{
    public static RunStateManager Instance { get; private set; }

    [Header("Player State")]
    public int savedPlayerMaxHp;
    public int savedPlayerCurrentHp;

    public int savedMaxEnergy;
    public int savedCurrentEnergy;

    [Header("Deck State")]
    public List<CardData> savedDeck = new();

    [Header("Runtime")]
    public bool hasSavedRunState;

    [Header("Enemy Encounter Save")]
    public EnemyDatabase enemyDatabase;

    public bool hasReservedEncounter;
    public List<string> reservedEnemyIds = new();

    [Header("Reserved Formation")]
    public EnemyFormationData reservedFormation;
    public bool hasReservedFormation;

    private const string SaveKeyHasReservedEncounter = "Run_HasReservedEncounter";
    private const string SaveKeyReservedEnemyIds = "Run_ReservedEnemyIds";

    [Header("Battle Start Deck Snapshot")]
    public BattleStartDeckSnapshot battleStartDeckSnapshot = new BattleStartDeckSnapshot();

    [Tooltip("F6 使用。重新載入場景後，是否要套用戰鬥開始前的牌組順序")]
    public bool pendingRestoreBattleStartDeckSnapshot;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public void ReserveFormation(EnemyFormationData formation)
    {
        reservedFormation = formation;
        hasReservedFormation = formation != null;

        if (formation != null)
            Debug.Log($"[RunStateManager] 保留怪物組：{formation.name}");
        else
            Debug.LogWarning("[RunStateManager] ReserveFormation 傳入 null");
    }

    public bool TryGetReservedFormation(out EnemyFormationData formation)
    {
        formation = null;

        if (!hasReservedFormation)
            return false;

        if (reservedFormation == null)
        {
            hasReservedFormation = false;
            return false;
        }

        formation = reservedFormation;
        return true;
    }

    public void ClearReservedFormation()
    {
        reservedFormation = null;
        hasReservedFormation = false;

        Debug.Log("[RunStateManager] 清除保留怪物組");
    }
    public void SaveFromBattle(
        BattleUnit playerUnit,
        EnergySystem energySystem,
        BattleDeck battleDeck
    )
    {
        if (playerUnit != null)
        {
            savedPlayerMaxHp = playerUnit.maxHp;
            savedPlayerCurrentHp = playerUnit.currentHp;
        }

        if (energySystem != null)
        {
            savedMaxEnergy = energySystem.maxEnergy;
            savedCurrentEnergy = energySystem.currentEnergy;
        }

        SaveDeck(battleDeck);

        hasSavedRunState = true;

        Debug.Log(
            $"[RunStateManager] 已保存狀態：HP {savedPlayerCurrentHp}/{savedPlayerMaxHp}, " +
            $"SAN {savedCurrentEnergy}/{savedMaxEnergy}, Deck {savedDeck.Count}"
        );
    }

    public void ApplyToBattle(
        BattleUnit playerUnit,
        EnergySystem energySystem,
        BattleDeck battleDeck
    )
    {
        if (!hasSavedRunState)
        {
            Debug.Log("[RunStateManager] 沒有已保存狀態，不套用");
            return;
        }

        if (playerUnit != null)
        {
            playerUnit.maxHp = savedPlayerMaxHp;
            playerUnit.currentHp = Mathf.Clamp(savedPlayerCurrentHp, 0, savedPlayerMaxHp);
        }

        if (energySystem != null)
        {
            energySystem.maxEnergy = savedMaxEnergy;
            energySystem.currentEnergy = Mathf.Clamp(savedCurrentEnergy, 0, savedMaxEnergy);
        }

        ApplyDeck(battleDeck);

        Debug.Log(
            $"[RunStateManager] 已套用狀態：HP {savedPlayerCurrentHp}/{savedPlayerMaxHp}, " +
            $"SAN {savedCurrentEnergy}/{savedMaxEnergy}, Deck {savedDeck.Count}"
        );
    }

    private void SaveDeck(BattleDeck battleDeck)
    {
        savedDeck.Clear();

        if (battleDeck == null)
            return;

        AddCardsFromPile(battleDeck.DrawPile);
        AddCardsFromPile(battleDeck.Hand);
        AddCardsFromPile(battleDeck.DiscardPile);
        AddCardsFromPile(battleDeck.ExhaustPile);
    }

    private void AddCardsFromPile(IReadOnlyList<CardInstance> cards)
    {
        if (cards == null)
            return;

        for (int i = 0; i < cards.Count; i++)
        {
            CardInstance card = cards[i];

            if (card == null || card.data == null)
                continue;

            savedDeck.Add(card.data);
        }
    }

    private void ApplyDeck(BattleDeck battleDeck)
    {
        if (battleDeck == null)
            return;

        battleDeck.startingDeck.Clear();

        for (int i = 0; i < savedDeck.Count; i++)
        {
            CardData cardData = savedDeck[i];

            if (cardData == null)
                continue;

            battleDeck.startingDeck.Add(cardData);
        }
    }

    public void ClearRunState()
    {
        savedPlayerMaxHp = 0;
        savedPlayerCurrentHp = 0;

        savedMaxEnergy = 0;
        savedCurrentEnergy = 0;

        savedDeck.Clear();

        hasSavedRunState = false;

        Debug.Log("[RunStateManager] 已清除保存狀態");
    }
    public void ReserveEncounterByEnemyData(List<EnemyData> enemies)
    {
        reservedEnemyIds.Clear();

        if (enemies != null)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyData enemy = enemies[i];

                if (enemy == null)
                    continue;

                if (string.IsNullOrWhiteSpace(enemy.enemyId))
                {
                    Debug.LogWarning($"[RunStateManager] EnemyData {enemy.name} 沒有 enemyId，無法保存");
                    continue;
                }

                reservedEnemyIds.Add(enemy.enemyId);
            }
        }

        hasReservedEncounter = reservedEnemyIds.Count > 0;

        SaveReservedEncounterToDisk();

        Debug.Log($"[RunStateManager] 保留怪物組：{string.Join(",", reservedEnemyIds)}");
    }
    public bool TryGetReservedEncounter(out List<EnemyData> enemies)
    {
        enemies = new List<EnemyData>();

        if (!hasReservedEncounter)
        {
            LoadReservedEncounterFromDisk();
        }

        if (!hasReservedEncounter)
            return false;

        if (enemyDatabase == null)
        {
            Debug.LogWarning("[RunStateManager] enemyDatabase 沒有指定，無法還原怪物組");
            return false;
        }

        for (int i = 0; i < reservedEnemyIds.Count; i++)
        {
            string id = reservedEnemyIds[i];

            EnemyData enemy = enemyDatabase.GetById(id);

            if (enemy == null)
            {
                Debug.LogWarning($"[RunStateManager] 找不到 enemyId = {id} 的 EnemyData");
                continue;
            }

            enemies.Add(enemy);
        }

        return enemies.Count > 0;
    }
    public void ClearReservedEncounter()
    {
        hasReservedEncounter = false;
        reservedEnemyIds.Clear();

        PlayerPrefs.DeleteKey(SaveKeyHasReservedEncounter);
        PlayerPrefs.DeleteKey(SaveKeyReservedEnemyIds);
        PlayerPrefs.Save();

        Debug.Log("[RunStateManager] 已清除保留怪物組");
    }
    private void SaveReservedEncounterToDisk()
    {
        PlayerPrefs.SetInt(SaveKeyHasReservedEncounter, hasReservedEncounter ? 1 : 0);
        PlayerPrefs.SetString(SaveKeyReservedEnemyIds, string.Join("|", reservedEnemyIds));
        PlayerPrefs.Save();
    }
    private void LoadReservedEncounterFromDisk()
    {
        int hasValue = PlayerPrefs.GetInt(SaveKeyHasReservedEncounter, 0);

        hasReservedEncounter = hasValue == 1;

        reservedEnemyIds.Clear();

        if (!hasReservedEncounter)
            return;

        string raw = PlayerPrefs.GetString(SaveKeyReservedEnemyIds, "");

        if (string.IsNullOrWhiteSpace(raw))
        {
            hasReservedEncounter = false;
            return;
        }

        string[] ids = raw.Split('|');

        for (int i = 0; i < ids.Length; i++)
        {
            string id = ids[i];

            if (string.IsNullOrWhiteSpace(id))
                continue;

            reservedEnemyIds.Add(id);
        }

        hasReservedEncounter = reservedEnemyIds.Count > 0;

        Debug.Log($"[RunStateManager] 從硬碟讀取保留怪物組：{string.Join(",", reservedEnemyIds)}");
    }
    public void SaveBattleStartDeckSnapshot(
        BattleUnit playerUnit,
        EnergySystem energySystem,
        BattleDeck battleDeck
    )
    {
        if (battleStartDeckSnapshot == null)
            battleStartDeckSnapshot = new BattleStartDeckSnapshot();

        battleStartDeckSnapshot.hasSnapshot = true;

        if (playerUnit != null)
        {
            battleStartDeckSnapshot.playerMaxHp = playerUnit.maxHp;
            battleStartDeckSnapshot.playerCurrentHp = playerUnit.currentHp;

            battleStartDeckSnapshot.playerStatuses.Clear();

            List<StatusSnapshotEntry> statusEntries = playerUnit.CaptureStatusSnapshot();

            for (int i = 0; i < statusEntries.Count; i++)
            {
                battleStartDeckSnapshot.playerStatuses.Add(statusEntries[i]);
            }
        }

        if (energySystem != null)
        {
            battleStartDeckSnapshot.maxEnergy = energySystem.maxEnergy;
            battleStartDeckSnapshot.currentEnergy = energySystem.currentEnergy;
        }

        battleStartDeckSnapshot.drawPileOrder.Clear();

        if (battleDeck != null && battleDeck.DrawPile != null)
        {
            for (int i = 0; i < battleDeck.DrawPile.Count; i++)
            {
                CardInstance card = battleDeck.DrawPile[i];

                if (card == null || card.data == null)
                    continue;

                battleStartDeckSnapshot.drawPileOrder.Add(card.data);
            }
        }

        Debug.Log(
            $"[RunStateManager] 已保存戰鬥開始牌組快照：" +
            $"HP {battleStartDeckSnapshot.playerCurrentHp}/{battleStartDeckSnapshot.playerMaxHp}, " +
            $"Energy {battleStartDeckSnapshot.currentEnergy}/{battleStartDeckSnapshot.maxEnergy}, " +
            $"DrawPileOrder {battleStartDeckSnapshot.drawPileOrder.Count}"
        );
    }
    //private void SaveDeckSnapshot(BattleDeck battleDeck)
    //{
    //    battleStartDeckSnapshot.drawPile.Clear();
    //    battleStartDeckSnapshot.hand.Clear();
    //    battleStartDeckSnapshot.discardPile.Clear();
    //    battleStartDeckSnapshot.exhaustPile.Clear();

    //    if (battleDeck == null)
    //        return;

    //    CopyCardsToDataList(battleDeck.DrawPile, battleStartDeckSnapshot.drawPile);
    //    CopyCardsToDataList(battleDeck.Hand, battleStartDeckSnapshot.hand);
    //    CopyCardsToDataList(battleDeck.DiscardPile, battleStartDeckSnapshot.discardPile);
    //    CopyCardsToDataList(battleDeck.ExhaustPile, battleStartDeckSnapshot.exhaustPile);
    //}

    //private void CopyCardsToDataList(
    //    IReadOnlyList<CardInstance> source,
    //    List<CardData> target
    //)
    //{
    //    if (source == null || target == null)
    //        return;

    //    for (int i = 0; i < source.Count; i++)
    //    {
    //        CardInstance card = source[i];

    //        if (card == null || card.data == null)
    //            continue;

    //        target.Add(card.data);
    //    }
    //}
    public void ApplyBattleStartDeckSnapshot(
        BattleUnit playerUnit,
        EnergySystem energySystem,
        BattleDeck battleDeck
    )
    {
        if (battleStartDeckSnapshot == null || !battleStartDeckSnapshot.hasSnapshot)
        {
            Debug.LogWarning("[RunStateManager] 沒有戰鬥開始牌組快照，無法還原");
            return;
        }

        if (playerUnit != null)
        {
            playerUnit.maxHp = battleStartDeckSnapshot.playerMaxHp;
            playerUnit.currentHp = Mathf.Clamp(
                battleStartDeckSnapshot.playerCurrentHp,
                0,
                battleStartDeckSnapshot.playerMaxHp
            );

            playerUnit.RestoreStatusSnapshot(battleStartDeckSnapshot.playerStatuses);
            playerUnit.NotifyUnitChanged();
        }

        if (energySystem != null)
        {
            energySystem.maxEnergy = battleStartDeckSnapshot.maxEnergy;
            energySystem.currentEnergy = Mathf.Clamp(
                battleStartDeckSnapshot.currentEnergy,
                0,
                battleStartDeckSnapshot.maxEnergy
            );
        }

        if (battleDeck != null)
        {
            battleDeck.RestoreDrawPileOrderOnly(
                battleStartDeckSnapshot.drawPileOrder
            );
        }

        pendingRestoreBattleStartDeckSnapshot = false;

        Debug.Log(
            $"[RunStateManager] 已還原戰鬥開始牌組快照：" +
            $"HP {battleStartDeckSnapshot.playerCurrentHp}/{battleStartDeckSnapshot.playerMaxHp}, " +
            $"Energy {battleStartDeckSnapshot.currentEnergy}/{battleStartDeckSnapshot.maxEnergy}, " +
            $"DrawPileOrder {battleStartDeckSnapshot.drawPileOrder.Count}"
        );
    }
    public void ClearAllRunData()
    {
        // 玩家進度
        savedPlayerMaxHp = 0;
        savedPlayerCurrentHp = 0;

        savedMaxEnergy = 0;
        savedCurrentEnergy = 0;

        if (savedDeck != null)
            savedDeck.Clear();

        hasSavedRunState = false;

        // 保留怪物組
        ClearReservedFormation();

        // 戰鬥開始快照
        if (battleStartDeckSnapshot != null)
        {
            battleStartDeckSnapshot.hasSnapshot = false;
            battleStartDeckSnapshot.playerMaxHp = 0;
            battleStartDeckSnapshot.playerCurrentHp = 0;
            battleStartDeckSnapshot.maxEnergy = 0;
            battleStartDeckSnapshot.currentEnergy = 0;

            if (battleStartDeckSnapshot.playerStatuses != null)
                battleStartDeckSnapshot.playerStatuses.Clear();

            if (battleStartDeckSnapshot.drawPileOrder != null)
                battleStartDeckSnapshot.drawPileOrder.Clear();
        }

        pendingRestoreBattleStartDeckSnapshot = false;

        Debug.Log("[RunStateManager] 已清除所有 Run 紀錄，下一次會以新遊戲開始");
    }
}