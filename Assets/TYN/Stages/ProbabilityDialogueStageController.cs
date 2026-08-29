using System.Collections;
using UnityEngine;

// ⚠️ 專案切到 Input System package。舊的 UnityEngine.Input 執行時會丟
// InvalidOperationException，而且編譯期看不出來。見 RunDebugPanel 的說明。
using UnityEngine.InputSystem;
using UnityEngine.UI;
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
    [Tooltip("對話事件庫。**正常遊玩走這裡** ——\n" +
             "附件《對話》：「進入位於地圖上的對話節點觸發，隨後根據當前同行角色中隨機抽取相關事件進行」。\n\n" +
             "留空則退回下面那一段固定的對話")]
    public ProbabilityDialogueLibrary library;

    [Tooltip("挑不到（或沒有庫）時演的那一段。\n\n" +
             "**執行時可以被 PendingDialogue 覆蓋** —— 事件或節點要指定演哪一段時用那個。\n" +
             "留空且沒有 Pending 的話，這一站會直接回報完成（玩家不會卡住，但也沒有對話）")]
    public ProbabilityDialogueData defaultDialogue;

    /// <summary>
    /// 下一次要演哪一段。跟 `BattleStageController.PendingEnemyId` 同一個模式 ——
    /// prefab 沒辦法在 Inspector 指定「這次演哪一段」，那是執行時才決定的。
    /// 用完就清。
    /// </summary>
    public static ProbabilityDialogueData PendingDialogue;

    [Header("背景")]
    [Tooltip("進場時墊在後面的場景美術。留空會在自己底下找。\n\n" +
             "**先用戶外那張**（`Art_Village_Outdoor`）—— 對話在大綱裡是\n" +
             "「路上遇到同行的人」，室內的背景對不上")]
    public StageBackdrop backdrop;

    [Header("結束")]
    [Tooltip("對話專屬的離開鍵。**話全部講完（含結算那一頁）才會出現**。\n\n" +
             "【為什麼要有一顆自己的鈕】對話框的推進鍵是**全幅透明**的 ——\n" +
             "沿用它的話玩家隨手一點就離開了，而結算那一頁正好排在最後，\n" +
             "等於「拿到什麼」永遠來不及看。事件那邊（`EventStageController.endButton`）\n" +
             "早就是這個做法，對話少了它才是不一致。\n\n" +
             "留空 = 退回舊的「等一下、點一下或逾時就走」——\n" +
             "不建議，但至少不會卡住玩家")]
    public Button endButton;

    [Tooltip("**只在沒有 End Button 時**才用得到：結束後最少停留幾秒，這段時間內點擊無效 ——\n" +
             "不然玩家在判定瞬間的那一下點擊會直接把結果跳掉")]
    [Min(0f)] public float endMinSeconds = 0.8f;

    [Tooltip("**只在沒有 End Button 時**才用得到：超過這個秒數還沒點就自動結束。0 = 一定要玩家點。\n\n" +
             "【為什麼不是純計時】讀字的速度因人而異，計時到了就跳走的話\n" +
             "讀得慢的人永遠看不完最後一句。但也不能只等點擊 ——\n" +
             "玩家放著去做別的事回來會不知道自己卡在哪。\n\n" +
             "⚠️ 有離開鍵的時候**不逾時** —— 那顆鈕就是明確的出口，\n" +
             "自己跳走反而會讓玩家覺得畫面被搶走了")]
    [Min(0f)] public float endAutoSeconds = 6f;

    private ProbabilityDialogueSession session;
    private bool reported;

    /// <summary>這一站實際在演哪一段。演完要拿它去立「演過了」的旗標。</summary>
    private ProbabilityDialogueData playing;

    // ==========================================
    public override void OnStageEnter(RunContext run)
    {
        reported = false;

        SetEndButtonVisible(false);

        if (endButton != null)
        {
            endButton.onClick.RemoveListener(Report);
            endButton.onClick.AddListener(Report);
        }

        if (backdrop == null) backdrop = GetComponentInChildren<StageBackdrop>(true);
        backdrop?.Spawn();

        if (view == null) view = GetComponentInChildren<ProbabilityDialogueView>(true);

        // 優先序：指名的 Pending → 對話庫隨機抽 → prefab 上那一段固定的。
        //
        // ⚠️ 順序不能反。Pending 是「這個節點就是要演這一段」（事件指定），
        // 蓋不過去的話那個指定就沒有意義了。
        ProbabilityDialogueData data = PendingDialogue;
        PendingDialogue = null;

        if (data == null && library != null)
        {
            // 亂數綁「run 種子 ＋ 節點 id」——
            // 同一場 run 的同一個節點重進不會換一段對話，但不同節點會換。
            // 跟 RoomController.Populate 的做法一樣
            string nodeId = run != null && run.CurrentNode != null ? run.CurrentNode.nodeId : "";
            int pickSeed = (run != null ? run.runSeed : 0) ^ (nodeId != null ? nodeId.GetHashCode() : 0);
            data = library.Pick(run, new System.Random(pickSeed));
        }

        if (data == null) data = defaultDialogue;

        playing = data;

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

        // ── 手牌從玩家自己的探索牌組發 ──
        //
        // 對話用的牌和開寶箱用的牌是**同一種牌、同一副牌組** ——
        // 所以牌組建構才會影響對話結果。
        //
        // ⚠️ 一定要先 SeedExploreDeck：F1 直接跳進來、或這場 run 還沒進過
        // 任何探索房時，run.exploreDeck 可能還沒發過起始牌組。
        // 沒有這一行的話手牌會是空的（或只有事件送的那一張）。
        if (run != null)
        {
            var deckSrc = Object.FindFirstObjectByType<ExplorationDeck>(FindObjectsInactive.Include);
            if (deckSrc != null) EldritchMile.Explore.ExploreStageController.SeedExploreDeck(run, deckSrc);
        }

        if (!session.Begin(data, new System.Random(seed), run != null ? run.exploreDeck : null))
        {
            // Begin 自己會報錯（規格 §11：不可以讓玩家卡在畫面上）
            Report();
        }
    }

    private void HandleEnded(bool success)
    {
        Debug.Log($"[機率對話] 結束：{(success ? "成功" : "全部失敗")}");

        // ⚠️ **成功與失敗都要標記。**
        // 只標成功的話，玩家每次走到對話節點都會再撞見同一段失敗過的對話 ——
        // 附件把「本輪次是否還會再度觸發已觸發但失敗的事件」列為未確定，
        // 這裡先取「不會」；要改就把 Library 那一條的 Once 取消勾選。
        RunContext run = GameFlowManager.Instance != null ? GameFlowManager.Instance.Run : null;
        ProbabilityDialogueLibrary.MarkPlayed(run, playing);

        StartCoroutine(WaitThenReport());
    }

    /// <summary>
    /// 等玩家看完最後一句（**含結算那一頁**），然後把離開鍵放出來。
    ///
    /// ⚠️ **先等話講完再做任何事。** 問話走的是共用對話框，分頁時玩家的點擊是
    /// 「翻頁」不是「我看完了」—— 不等的話，翻第一頁那一下就會被當成要離開。
    ///
    /// 【為什麼是輪詢 IsIdle 而不是訂 OnAllClosed】`OnAllClosed` 是在玩家
    /// **點擊推進**、而且佇列剛好空掉時才發的 —— 用它的話離開鍵要多點一下才出現。
    /// 我們要的是「最後一句打完的當下」。事件那邊也是同一個理由用輪詢。
    ///
    /// 沒有離開鍵時退回舊的三段式（不可跳過的一小段 → 等點擊 → 逾時自動走），
    /// 那是為了「鈕還沒接上去」的過渡期，不是預設的體驗。
    /// </summary>
    private IEnumerator WaitThenReport()
    {
        while (PopupService.Instance != null && !PopupService.Instance.IsIdle) yield return null;

        if (endButton != null)
        {
            // 出口只有這一顆鈕 —— 不逾時、不吃隨便一下點擊。
            // 鎖住推進，不然玩家隨手一點就把結算那一頁跳掉了
            if (PopupService.Instance != null) PopupService.Instance.LockAdvance = true;
            SetEndButtonVisible(true);
            yield break;
        }

        float t = 0f;
        while (t < endMinSeconds) { t += Time.unscaledDeltaTime; yield return null; }

        while (true)
        {
            if (AnyContinuePressed()) break;
            if (endAutoSeconds > 0f && t >= endAutoSeconds) break;
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        Report();
    }

    private void SetEndButtonVisible(bool visible)
    {
        if (endButton == null) return;
        endButton.gameObject.SetActive(visible);
    }

    /// <summary>
    /// 玩家有沒有按下「繼續」。滑鼠左鍵或任何按鍵都算。
    /// **只有在沒有離開鍵時才會用到。**
    ///
    /// 沒有滑鼠／鍵盤時對應的 current 是 null，那不是錯誤（手把、觸控裝置），
    /// 所以兩個都要各自判空 —— 少判一個就會在那種裝置上丟例外。
    /// </summary>
    private static bool AnyContinuePressed()
    {
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.anyKey.wasPressedThisFrame) return true;

        return false;
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

        if (endButton != null) endButton.onClick.RemoveListener(Report);
        SetEndButtonVisible(false);

        // Detach 會把 HoldOpen / 立繪還回去（見那一支的說明），
        // 這裡再讓對話框自己收掉 —— 不收的話它會一路留到下一站
        if (view != null) view.Detach();
        PopupService.Instance?.CloseWhenDrained();

        // 背景是生成出來的，離開時要收掉 —— 不收的話它會一路留到下一站
        backdrop?.Despawn();

        session = null;
        playing = null;
        yield break;
    }
}
