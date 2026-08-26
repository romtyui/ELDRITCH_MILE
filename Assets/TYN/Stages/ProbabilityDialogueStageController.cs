using System.Collections;
using UnityEngine;
using EldritchMile.Core;
using EldritchMile.Core.ProbabilityDialogue;
using EldritchMile.UI.ProbabilityDialogue;

/// <summary>
/// 機率卡牌對話的 Stage。
///
/// ⚠️ **與舊的 `DialogueStageController` 是兩套，刻意並存** ——
/// 理由見 <see cref="StageType.ProbabilityDialogue"/>。
///
/// 這一支很薄：規則在 <see cref="ProbabilityDialogueSession"/>、畫面在
/// <see cref="ProbabilityDialogueView"/>，這裡只負責「進場開一場、結束回報流程」。
/// </summary>
public class ProbabilityDialogueStageController : StageController
{
    public override StageType Stage => StageType.ProbabilityDialogue;

    [Header("畫面")]
    [Tooltip("留空會在自己底下找")]
    public ProbabilityDialogueView view;

    [Header("內容")]
    [Tooltip("這一站要演哪一段對話。\n\n" +
             "**執行時可以被 PendingDialogue 覆蓋** —— 事件或節點要指定演哪一段時用那個。\n" +
             "留空且沒有 Pending 的話，這一站會直接回報完成（玩家不會卡住，但也沒有對話）")]
    public ProbabilityDialogueData defaultDialogue;

    /// <summary>
    /// 下一次要演哪一段。跟 `BattleStageController.PendingEnemyId` 同一個模式 ——
    /// prefab 沒辦法在 Inspector 指定「這次演哪一段」，那是執行時才決定的。
    /// 用完就清。
    /// </summary>
    public static ProbabilityDialogueData PendingDialogue;

    [Header("結束")]
    [Tooltip("結束後隔多久才回報流程完成。讓玩家看得完最後一句")]
    [Min(0f)] public float endDelaySeconds = 1.6f;

    private ProbabilityDialogueSession session;
    private bool reported;

    // ==========================================
    public override void OnStageEnter(RunContext run)
    {
        reported = false;

        if (view == null) view = GetComponentInChildren<ProbabilityDialogueView>(true);

        ProbabilityDialogueData data = PendingDialogue != null ? PendingDialogue : defaultDialogue;
        PendingDialogue = null;

        if (data == null)
        {
            Debug.LogWarning(
                "[機率對話] 這一站沒有指定對話資料（Default Dialogue 是空的，也沒有 Pending）。\n" +
                "直接回報完成 —— 玩家不會卡在空畫面。");
            Report();
            return;
        }

        if (view == null)
        {
            Debug.LogError("[機率對話] 這個 Stage 上找不到 ProbabilityDialogueView，沒有畫面可以演", this);
            Report();
            return;
        }

        // 亂數綁 run 種子 ＋ 事件 id —— **同一場 run 的同一段對話，重進不會換一手牌**
        int seed = (run != null ? run.runSeed : 0) ^ (data.eventId != null ? data.eventId.GetHashCode() : 0);

        session = new ProbabilityDialogueSession();
        view.Attach(session, GameFlowManager.Instance != null ? GameFlowManager.Instance.characterDatabase : null);

        session.OnEnded += HandleEnded;

        if (!session.Begin(data, new System.Random(seed)))
        {
            // Begin 自己會報錯（規格 §11：不可以讓玩家卡在畫面上）
            Report();
        }
    }

    private void HandleEnded(bool success)
    {
        Debug.Log($"[機率對話] 結束：{(success ? "成功" : "全部失敗")}");
        StartCoroutine(ReportAfterDelay());
    }

    private IEnumerator ReportAfterDelay()
    {
        yield return new WaitForSecondsRealtime(endDelaySeconds);
        Report();
    }

    private void Report()
    {
        if (reported) return;
        reported = true;
        ReportComplete(StageResult.Completed);
    }

    public override IEnumerator OnStageExit()
    {
        if (session != null) session.OnEnded -= HandleEnded;
        if (view != null) view.Detach();
        session = null;
        yield break;
    }
}
