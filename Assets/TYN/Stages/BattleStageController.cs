using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EldritchMile.Core;

/// <summary>
/// 戰鬥 Stage。**我方只負責「進去」與「出來」，戰鬥本身完全是戰鬥組的。**
///
/// ────────────────────────────────────────────────────────
/// 【三個接縫，各自為什麼長這樣】
///
/// ① **進去** —— `RunStateManager.ReserveEncounterByEnemyData()` 預約敵人，
///    再叫 `BattleManager.StartBattle()`。HP／SAN／牌組不用我方傳，
///    `StartBattle()` 自己會從 `RunStateManager` 撈（`hasSavedRunState` 是開關）。
///
/// ② **出來** —— 靠訊號，不是輪詢。
///    `BattleManager.EndBattle()` 會發 `BattleWon` / `BattleLost`，
///    **而且是在 `SaveFromBattle()` 之後發的** —— 所以我們收到的時候，
///    `RunStateManager` 裡的 HP／SAN／牌組已經是這場的結果了。
///
///    ⚠️ 曾經考慮過輪詢 `BattleManager.gameObject.activeSelf`（勝利時他會關掉自己），
///    但**打輸的時候物件還開著**（他去開死亡選單），所以那條路只抓得到一半。
///
/// ③ **戰績** —— 勝利時，替每一個打倒的敵人立一個 `killed_&lt;enemyId&gt;` 旗標。
///    《螺湮的祝福》的觸發條件「打倒半魚人祭司后」就是靠這個活過來的。
///    不做對照表 —— 旗標名直接從敵人 id 推得出來，多一張表就多一個會對不上的地方。
///
/// ────────────────────────────────────────────────────────
/// 【⚠️ 兩件還沒到位的事，到位之前這支跑不完整】
///
///   1. **沒有 `Stage_Battle` prefab** —— 戰鬥那一組（BattleManager／BattleDeck／
///      EnergySystem／戰鬥 UI）還住在 `SampleScene`，要先包成 prefab 放進 StageHost。
///   2. **`EnemyData.enemyId` 全是空的** —— 五個敵人資產都沒填，
///      `ReserveEncounterByEnemyData()` 會整組跳過並警告。填了才能指定打誰。
/// </summary>
public class BattleStageController : StageController
{
    public override StageType Stage => StageType.Battle;

    [Header("戰鬥本體")]
    [Tooltip("戰鬥組的 BattleManager。留空會試著在自己底下找。\n" +
             "找不到的話這一站會直接回報完成 —— 玩家不會卡住，但也沒打到架")]
    public BattleManager battleManager;

    [Header("敵人")]
    [Tooltip("這一站預設打誰（填 EnemyData 的 Enemy Id）。\n\n" +
             "⚠️ 目前五個敵人資產的 Enemy Id **都是空的**，填了才有用。\n" +
             "留空則沿用戰鬥組自己的抽怪邏輯")]
    public List<string> defaultEnemyIds = new List<string>();

    [Header("戰績")]
    [Tooltip("打倒敵人時立的旗標前綴。`killed_` ＋ 敵人 id。\n" +
             "《螺湮的祝福》等的是 killed_fish_priest")]
    public string defeatFlagPrefix = "killed_";

    /// <summary>
    /// 下一場戰鬥要打誰。**事件的 `StartBattle` 效果靠它指定對手**
    /// （《貪吃鬼》選項 B）。用完就清掉。
    ///
    /// 跟 `GameFlowManager.PendingEvent` 是同一個模式：
    /// prefab 沒辦法在 Inspector 指定「這次要打誰」，那是執行時才決定的。
    /// </summary>
    public static string PendingEnemyId;

    /// 這一場實際預約到的敵人。勝利時用來立旗標
    private readonly List<string> fightingEnemyIds = new List<string>();

    private RunContext run;
    private bool reported;

    // ==========================================
    public override void OnStageEnter(RunContext context)
    {
        run = context;
        reported = false;
        fightingEnemyIds.Clear();

        if (battleManager == null) battleManager = GetComponentInChildren<BattleManager>(true);

        // ⚠️ 訂閱要在 StartBattle 之前 —— 一場空的戰鬥（沒有敵人）
        //    有可能在同一幀就結束，晚訂就收不到了
        TutorialEventBus.OnSignalRaised -= HandleSignal;
        TutorialEventBus.OnSignalRaised += HandleSignal;

        ReserveEnemies();

        if (battleManager == null)
        {
            Debug.LogWarning(
                "[戰鬥] 這個 Stage 上沒有 BattleManager —— 戰鬥那一組還沒包成 prefab。\n" +
                "先直接回報完成，玩家不會卡在黑畫面。");
            Report(StageResult.Completed);
            return;
        }

        battleManager.gameObject.SetActive(true);
        battleManager.StartBattle();
    }

    public override IEnumerator OnStageExit()
    {
        TutorialEventBus.OnSignalRaised -= HandleSignal;
        yield break;
    }

    // ==========================================
    // 進去
    // ==========================================
    /// <summary>
    /// 告訴戰鬥組這一場要打誰。沒有指定就不預約 —— 那時候他會自己抽怪。
    /// </summary>
    private void ReserveEnemies()
    {
        var ids = new List<string>();

        // 事件指定的優先（《貪吃鬼》選項 B），用完就清
        if (!string.IsNullOrEmpty(PendingEnemyId))
        {
            ids.Add(PendingEnemyId);
            PendingEnemyId = null;
        }
        else
        {
            for (int i = 0; i < defaultEnemyIds.Count; i++)
            {
                if (!string.IsNullOrEmpty(defaultEnemyIds[i])) ids.Add(defaultEnemyIds[i]);
            }
        }

        if (ids.Count == 0) return;

        RunStateManager rs = RunStateManager.Instance;
        if (rs == null || rs.enemyDatabase == null)
        {
            Debug.LogWarning("[戰鬥] 沒有 RunStateManager 或 EnemyDatabase，指定不了對手，交給戰鬥組自己抽");
            return;
        }

        var enemies = new List<EnemyData>();
        for (int i = 0; i < ids.Count; i++)
        {
            EnemyData e = rs.enemyDatabase.GetById(ids[i]);

            if (e == null)
            {
                Debug.LogWarning(
                    $"[戰鬥] EnemyDatabase 裡找不到 enemyId =「{ids[i]}」。\n" +
                    "⚠️ 目前五個敵人資產的 Enemy Id 都是空的 —— 要先填才查得到。");
                continue;
            }

            enemies.Add(e);
            fightingEnemyIds.Add(ids[i]);
        }

        if (enemies.Count > 0) rs.ReserveEncounterByEnemyData(enemies);
    }

    // ==========================================
    // 出來
    // ==========================================
    private void HandleSignal(string signalId)
    {
        if (signalId == TutorialSignal.BattleWon) OnWon();
        else if (signalId == TutorialSignal.BattleLost) OnLost();
    }

    private void OnWon()
    {
        // ⚠️ 旗標要在回報之前立 —— 回報會觸發轉場，之後這個物件就沒了
        for (int i = 0; i < fightingEnemyIds.Count; i++)
        {
            run?.SetFlag(defeatFlagPrefix + fightingEnemyIds[i]);
        }

        Debug.Log($"[戰鬥] 勝利。HP {PlayerVitals.Hp}/{PlayerVitals.MaxHp}、" +
                  $"SAN {PlayerVitals.San}/{PlayerVitals.MaxSan}、牌組 {PlayerVitals.DeckCount} 張");

        Report(StageResult.Completed);
    }

    private void OnLost()
    {
        Debug.Log("[戰鬥] 失敗 —— 這場 run 結束，走遺產結算");

        // ⚠️ 戰鬥組在失敗時會自己開死亡選單。那個選單與我方的輪迴結算是兩套東西，
        //    誰負責「玩家按了重來之後發生什麼」要跟他確認 —— 見 Docs/Next.md
        Report(StageResult.PlayerDied);
    }

    /// <summary>只回報一次。訊號有可能重複發（例如同一幀兩個敵人同時死）。</summary>
    private void Report(StageResult result)
    {
        if (reported) return;
        reported = true;

        TutorialEventBus.OnSignalRaised -= HandleSignal;
        ReportComplete(result);
    }
}
