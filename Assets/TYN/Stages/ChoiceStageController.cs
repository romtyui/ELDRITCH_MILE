using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EldritchMile.Core;

/// <summary>
/// 「開場白 → 選項 → 結果 → 回地圖」的 Stage 基底。
///
/// 【為什麼三個 Stage 共用】對話、商店、特殊事件的**形狀完全一樣**，
/// 差別只在「選項是什麼」與「選了之後給什麼」：
///
///   對話     選項是可以用機率卡打的判定目標，結果有成功／失敗之分
///   商店     選項是商品，點了就拿
///   特殊事件 選項是可挑的牌，點了就拿
///
/// 分成三份寫的話，光是「播完開場白才顯示選項」這個時序就要對三次。
///
/// 【時序是這一段最容易寫錯的地方】訊息是排隊播的，選項必須等**開場白全部播完**
/// 才出現，否則玩家會在還沒看完狀況時就被要求做決定。
/// 所以用 `PopupService.OnAllClosed` 當節拍器，並用 `phase` 區分現在是哪一段 ——
/// 沒有 `phase` 的話，結果文字播完也會被當成「開場白播完」，選項會再冒出來一次。
/// </summary>
public abstract class ChoiceStageController : StageController
{
    [System.Serializable]
    public class Line
    {
        [Tooltip("說話者。留空則走系統提示的公版樣式")]
        public string speaker = "";

        [TextArea(2, 4)] public string text = "";

        [Tooltip("立繪／特寫圖。可留空")]
        public Sprite portrait;
    }

    [Header("開場白")]
    [Tooltip("進場先播這幾句，播完才顯示選項")]
    public List<Line> introLines = new List<Line>();

    [Header("收尾")]
    [Tooltip("選完之後播這幾句，播完回地圖。可留空")]
    public List<Line> outroLines = new List<Line>();

    [Tooltip("選項顯示時，對話框正文要留著哪一句。\n" +
             "留空則沿用開場白的最後一句（通常就是那個問句，這樣最自然）。\n\n" +
             "⚠️ 這句不能沒有 —— **選項是對話框的子物件**，框關掉的話選項活著也看不見")]
    [TextArea(2, 3)]
    public string optionsPrompt = "";

    /// 現在走到哪一段。**沒有這個的話結果文字播完會被誤判成開場白播完。**
    protected enum Phase { Intro, Choosing, Outro, Done }
    protected Phase phase = Phase.Intro;

    protected DialogueOptionsPanel Options => DialogueOptionsPanel.Instance;
    protected DialogueBoxUI Box => PopupService.Instance != null ? PopupService.Instance.dialogueBox : null;

    // ==========================================
    // 生命週期
    // ==========================================
    public override void OnStageEnter(RunContext run)
    {
        phase = Phase.Intro;

        if (PopupService.Instance == null)
        {
            Debug.LogWarning($"[{Stage}] 場上沒有 PopupService，無法進行，直接結束");
            Finish();
            return;
        }

        if (Options == null)
        {
            Debug.LogWarning(
                $"[{Stage}] 場上沒有 DialogueOptionsPanel —— 選項無法顯示。\n" +
                "它應該掛在對話框的 option_box 上，與對話框、手牌區同一組常駐場景。");
        }

        Options?.HideAll();

        PopupService.Instance.OnAllClosed -= HandleAllClosed;
        PopupService.Instance.OnAllClosed += HandleAllClosed;

        OnPrepare(run);

        // ⚠️ HoldOpen 必須在**排隊之前**就設好。
        //
        // 對話框的 Advance() 是「文字播完 → 若沒 HoldOpen 就 Hide()」，而 Hide() 會
        // SetActive(false) 整個 root —— **選項是那個 root 的子物件**。
        // 等 OnAllClosed 觸發後才設 HoldOpen 已經太晚：框關掉了，選項活著也看不見，
        // 畫面就是一片空白。
        if (Box != null) Box.HoldOpen = true;

        // 開場白播完才會走到 ShowOptions（見 HandleAllClosed）
        if (!QueueLines(introLines))
        {
            // 沒有開場白 → 對話框從來沒被打開過，得先撐一句話出來當背景
            phase = Phase.Choosing;
            EnsureBoxOpenForOptions();
            ShowOptions(run);
        }
    }

    /// <summary>
    /// 顯示選項前確保對話框是開的。
    /// 選項是對話框的子物件，框沒開的話選項活著也看不見。
    /// </summary>
    private void EnsureBoxOpenForOptions()
    {
        if (Box == null) return;

        Box.HoldOpen = true;
        if (Box.IsShowing) return;

        string prompt = optionsPrompt;

        // 沒指定就沿用開場白的最後一句 —— 通常就是那個問句
        if (string.IsNullOrEmpty(prompt) && introLines != null)
        {
            for (int i = introLines.Count - 1; i >= 0; i--)
            {
                if (introLines[i] != null && !string.IsNullOrEmpty(introLines[i].text))
                {
                    prompt = introLines[i].text;
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(prompt)) prompt = "……";

        // ShowInstant：直接替換正文、不排隊 —— 這裡不能再觸發一次 OnAllClosed
        PopupService.Instance?.ShowInstant(prompt);
    }

    public override IEnumerator OnStageExit()
    {
        if (PopupService.Instance != null) PopupService.Instance.OnAllClosed -= HandleAllClosed;

        Unsubscribe();
        Options?.HideAll();

        if (Box != null) Box.HoldOpen = false;

        yield break;
    }

    // ==========================================
    // 子類別要實作／可覆寫的
    // ==========================================

    /// <summary>進場時、播開場白**之前**呼叫。用來準備資料（抽手牌之類）。</summary>
    protected virtual void OnPrepare(RunContext run) { }

    /// <summary>開場白播完，該顯示選項了。</summary>
    protected abstract void ShowOptions(RunContext run);

    /// <summary>離場時解除自己額外掛的事件。</summary>
    protected virtual void Unsubscribe() { }

    // ==========================================
    // 共用工具
    // ==========================================

    /// <summary>把幾句話排進佇列。回傳 false 代表一句都沒有。</summary>
    protected bool QueueLines(List<Line> lines)
    {
        if (lines == null || lines.Count == 0) return false;

        bool any = false;

        for (int i = 0; i < lines.Count; i++)
        {
            Line line = lines[i];
            if (line == null || string.IsNullOrEmpty(line.text)) continue;

            any = true;

            if (string.IsNullOrEmpty(line.speaker))
            {
                if (line.portrait != null)
                    PopupService.Instance.ShowSystemWithCloseUp(line.text, line.portrait);
                else
                    PopupService.Instance.ShowText(line.text);
            }
            else
            {
                PopupService.Instance.ShowSpeech(line.speaker, line.text, line.portrait);
            }
        }

        return any;
    }

    /// <summary>
    /// 選完了、結果也演完了 → 播收尾台詞然後回地圖。
    /// 子類別在自己的分支結束時呼叫這支。
    /// </summary>
    protected void BeginOutro()
    {
        if (phase == Phase.Outro || phase == Phase.Done) return;

        phase = Phase.Outro;
        Options?.HideAll();

        // 選項收掉之後對話框才可以關 —— 在那之前要一直開著撐住版面
        if (Box != null) Box.HoldOpen = false;

        if (!QueueLines(outroLines))
        {
            // 沒有收尾台詞的話，等目前這則播完就收
            PopupService.Instance?.CloseWhenDrained();
        }
        else
        {
            PopupService.Instance?.CloseWhenDrained();
        }
    }

    protected void Finish()
    {
        if (phase == Phase.Done) return;
        phase = Phase.Done;

        if (PopupService.Instance != null) PopupService.Instance.OnAllClosed -= HandleAllClosed;

        Options?.HideAll();
        if (Box != null) Box.HoldOpen = false;

        ReportComplete(StageResult.Completed);
    }

    private void HandleAllClosed()
    {
        switch (phase)
        {
            case Phase.Intro:
                phase = Phase.Choosing;

                // HoldOpen 在 OnStageEnter 就設好了，這裡是防呆：
                // 若中途有人呼叫過 Hide()（它會把 HoldOpen 一起清掉），把框撐回來
                EnsureBoxOpenForOptions();

                ShowOptions(GameFlowManager.Instance != null ? GameFlowManager.Instance.Run : null);
                break;

            case Phase.Outro:
                Finish();
                break;

            // Choosing 期間佇列本來就會空（在等玩家），不做事
        }
    }
}
