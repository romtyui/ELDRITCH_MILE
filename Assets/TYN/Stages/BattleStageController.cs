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
/// 【④ 接上宿主場景】`Stage_Battle` 是 prefab，**prefab 存不了場景引用**。
/// 戰鬥那一組原本在 `SampleScene` 靠 Inspector 指著相機／BGM／全域單例，
/// 包成 prefab 後那些欄位全部是 `None`，所以進場時要補綁 —— 見 `BindToHostScene()`。
/// 這些失效全部是安靜的，`verboseBinding` 打開會逐項印出來。
///
/// ────────────────────────────────────────────────────────
/// 【⚠️ 還沒到位的事，到位之前這支跑不完整】
///
///   1. ~~沒有 `Stage_Battle` prefab~~ ✅ 2026-08-22 建好了，在 `Assets/TYN/Stages/`。
///   2. **`EnemyData.enemyId` 全是空的** —— 五個敵人資產都沒填，
///      `ReserveEncounterByEnemyData()` 會整組跳過並警告。填了才能指定打誰。
///   3. **`TutorialStarter.battleManager` 是 private** —— 我們指不了，
///      戰鬥教學序列因此拿不到 BattleManager。要請 Romtyui 開個公開的設定方式。
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

    [Header("診斷")]
    [Tooltip("印出每一項「接上宿主場景」的結果（相機、BGM、遺物加成、教學 UI）。" +
             "這些綁定失敗全都是安靜的，畫面看起來只是怪，不會報錯 —— 覺得哪裡不對就先打開這個")]
    public bool verboseBinding = false;

    /// 這一場登記進 ModifierSystem 的 RelicsRuntime。離場時要註銷，
    /// 否則 providers 會累積一堆已被銷毀的物件
    private RelicsRuntime boundRelicsRuntime;

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

        // 接上宿主場景的東西。**一定要在 StartBattle 之前** ——
        // 相機沒綁好的話第一幀就會用錯的排序畫出來
        BindToHostScene();

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
        UnbindFromHostScene();
        yield break;
    }

    // ==========================================
    // 接上宿主場景
    // ==========================================
    /// <summary>
    /// `Stage_Battle` 是 prefab，**prefab 存不了場景引用**。
    /// 戰鬥那一組原本在 `SampleScene` 裡靠 Inspector 指著場景物件（相機、BGM、
    /// 全域單例），包成 prefab 之後那些欄位全部會是 `None`。這裡在進場時補回來。
    ///
    /// ⚠️ **這些全部安靜地失效** —— 相機沒綁只是排序不對、BGM 沒綁只是音量鍵沒反應，
    /// 一個錯誤訊息都不會有。所以每一項失敗都要印出來。
    /// </summary>
    private void BindToHostScene()
    {
        BindCanvasCameras();
        BindBgmToOptionMenu();
        BindModifierSystem();
        BindTutorialUI();
    }

    /// <summary>
    /// 四個 Canvas 有三個是 `ScreenSpaceCamera`。
    ///
    /// ⚠️ 相機是 null 時 Unity **不會報錯**，而是當成 Overlay 來畫 ——
    /// 但戰鬥用的是 URP 2D 燈光（`Light2D` / `PSBMonsterLightReveal`），
    /// Overlay 的 Canvas 不吃燈光也不跟世界物件排序，結果會像「美術壞掉」，
    /// 沒有人會聯想到是 prefab 化造成的。
    /// </summary>
    private void BindCanvasCameras()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();

        if (cam == null)
        {
            Debug.LogWarning("[戰鬥] 場景裡找不到相機，ScreenSpaceCamera 的 Canvas 會退化成 Overlay，" +
                             "2D 燈光與排序會不正確", this);
            return;
        }

        int bound = 0;
        foreach (Canvas canvas in GetComponentsInChildren<Canvas>(true))
        {
            // Overlay 的 Canvas 不需要相機，指了也無害；一起指是為了他哪天改 renderMode
            if (canvas.worldCamera == cam) continue;
            canvas.worldCamera = cam;
            bound++;
        }

        if (verboseBinding) Debug.Log($"[戰鬥] 已把 {bound} 個 Canvas 綁到相機「{cam.name}」", this);
    }

    /// <summary>
    /// 戰鬥的選項選單有音量控制，指的是 `SampleScene` 那顆 BGMManager。
    ///
    /// 【為什麼不把 BGMManager 包進 prefab】場景裡已經有一顆在播同一首曲子了。
    /// 包進來會變成兩個 AudioSource 播同一首、相位差幾毫秒，聽起來像破音；
    /// 而且他那顆音量是 1.0，會直接蓋過場景的設定。
    /// </summary>
    private void BindBgmToOptionMenu()
    {
        OptionMenuUI menu = GetComponentInChildren<OptionMenuUI>(true);
        if (menu == null) return;

        AudioSource bgm = null;
        foreach (AudioSource src in FindObjectsByType<AudioSource>(FindObjectsInactive.Include))
        {
            // 只認宿主場景的 —— 自己樹底下的不算
            if (src.transform.IsChildOf(transform)) continue;
            if (!src.loop) continue;                 // BGM 一定是循環的，音效不是
            bgm = src;
            break;
        }

        if (bgm == null)
        {
            if (verboseBinding) Debug.Log("[戰鬥] 場景裡沒有循環播放的 AudioSource，選項選單的音量鍵不會有作用", this);
            return;
        }

        menu.controlledAudioSources = new[] { bgm };
        if (verboseBinding) Debug.Log($"[戰鬥] 選項選單的音量已綁到「{bgm.name}」", this);
    }

    /// <summary>
    /// `ModifierSystem`（遺物加成）在 `Awake` 時 `FindFirstObjectByType&lt;RelicsRuntime&gt;()`，
    /// 但那時候 `Stage_Battle` 還沒生出來，所以它一定找不到 —— 必須由我們補登記。
    ///
    /// ⚠️ 進場登記就要離場註銷。`RelicsRuntime` 是**每場戰鬥一個新的**（跟著 prefab 生滅），
    /// 不註銷的話 `ModifierSystem` 的 providers 會一場一場累積已被銷毀的物件。
    /// </summary>
    private void BindModifierSystem()
    {
        if (ModifierSystem.Instance == null) return;   // 沒有它只是遺物加成不生效，不是錯誤

        boundRelicsRuntime = GetComponentInChildren<RelicsRuntime>(true);
        if (boundRelicsRuntime == null) return;

        ModifierSystem.Instance.RegisterProvider(boundRelicsRuntime);
        if (verboseBinding) Debug.Log("[戰鬥] RelicsRuntime 已登記進 ModifierSystem", this);
    }

    /// <summary>
    /// `TutorialManager` 也是在 `Awake` 找 `TutorialUI`，同樣找不到。
    /// 這個欄位是 public，可以直接指。
    ///
    /// ⚠️ **`TutorialStarter.battleManager` 是 private，我們指不了。**
    /// 戰鬥教學要真的跑起來的話，需要請 Romtyui 開一個公開的設定方式
    /// （欄位改 public，或加一支 `Rebind()`）。在那之前教學序列拿不到 BattleManager。
    /// </summary>
    private void BindTutorialUI()
    {
        if (TutorialManager.Instance == null) return;

        TutorialUI ui = GetComponentInChildren<TutorialUI>(true);
        if (ui == null) return;

        TutorialManager.Instance.tutorialUI = ui;
        if (verboseBinding) Debug.Log("[戰鬥] TutorialUI 已接上 TutorialManager", this);
    }

    private void UnbindFromHostScene()
    {
        if (boundRelicsRuntime != null && ModifierSystem.Instance != null)
        {
            ModifierSystem.Instance.UnregisterProvider(boundRelicsRuntime);
        }
        boundRelicsRuntime = null;
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
