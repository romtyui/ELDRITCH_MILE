using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EldritchMile.Core;

// 手牌區住在 Explore 命名空間，但它是跨環節共用的常駐 UI
using EldritchMile.Explore;

/// <summary>
/// 隨機事件 Stage。演大綱〈事件〉那一章的表格。
///
/// 【它跟其他 Stage 最大的不同：內容是執行時才決定的】
/// 對話／商店的內容寫在各自的 prefab 上；事件不行 —— 這一站要演哪個事件是
/// `EventLibrary` 當場抽的。所以 prefab 上什麼都不填，開場時去
/// `GameFlowManager.PendingEvent` 拿。
///
/// 【前置，不覆蓋】演完之後由總管接回原本那個節點的 Stage。
///
/// 【結束由玩家按專屬的鈕，不跟對話共用】
/// 對話框的推進鈕是全幅透明的，事件如果沿用它，玩家隨手一點就跳過整個事件了。
/// 所以事件有自己的結束鍵，而且**要等文字全部播完才出現**。
/// </summary>
public class EventStageController : ChoiceStageController
{
    public override StageType Stage => StageType.Event;

    [Header("結束")]
    [Tooltip("事件專屬的結束鍵。**文字播完才會顯示**。\n" +
             "留空的話會退回「播完自動結束」——不建議，玩家會來不及讀完")]
    public Button endButton;

    [Header("結果")]
    [Tooltip("效果的提示要怎麼接在結果內文後面。{0} = 所有效果")]
    [TextArea(2, 4)]
    public string effectLineFormat = "\n\n（{0}）";

    [Tooltip("多個效果之間用什麼隔開")]
    public string effectSeparator = "、";

    [Header("背景")]
    [Tooltip("進場時墊在後面的場景美術。留空會在自己底下找。\n\n" +
             "**先用戶外那張** —— 事件沒有自己的場景，不墊東西的話\n" +
             "會直接看到相機的天空底色")]
    public StageBackdrop backdrop;

    /// <summary>這一站要演的事件。由總管在轉場時放好。</summary>
    private EventData data;

    /// <summary>文字播完了、正在等玩家按結束。</summary>
    private bool awaitingEnd;

    protected override void OnPrepare(RunContext run)
    {
        data = GameFlowManager.Instance != null ? GameFlowManager.Instance.PendingEvent : null;
        awaitingEnd = false;

        // 事件沒有自己的場景，不墊背景的話會直接看到相機的天空底色
        if (backdrop == null) backdrop = GetComponentInChildren<StageBackdrop>(true);
        backdrop?.Spawn();

        // ⚠️ 事件沒有打牌環節，手牌區整組都不該出現。
        //
        // 不收的話畫面上會有**兩顆長得像的結束鍵** —— 打牌的 `Btn_EndEncounter`
        // 就在事件結束鍵旁邊，而它在事件裡按了不會有任何意義。
        // 手牌區平常是常駐在場景的，沒有人主動收它就會一直留著。
        ExploreHandUI.Instance?.Hide();

        SetEndButtonVisible(false);

        if (endButton != null)
        {
            endButton.onClick.RemoveListener(EndEvent);
            endButton.onClick.AddListener(EndEvent);
        }

        if (data == null)
        {
            Debug.LogWarning("[事件] 沒有 PendingEvent —— 這個 Stage 只該由總管在抽到事件時載入");
            return;
        }

        // 事件的內文是 prefab 上沒有的，開場白要在這裡才灌進去。
        // ⚠️ 直接改 introLines 而不是自己排隊 —— 基底類別的時序
        //（開場白播完才顯示選項）已經處理好了，繞過它只會再踩一次同樣的坑
        introLines.Clear();

        if (!string.IsNullOrEmpty(data.title))
        {
            introLines.Add(new Line { speaker = "", text = $"《{data.title}》" });
        }

        // **一句一句播，不是整段倒出來。**
        // 大綱的事件內文動輒兩三百字，一次全塞進對話框讀起來很吃力，
        // 而且失去「一句一句揭露」的節奏感 —— 那正是這種事件的味道所在。
        foreach (string para in SplitParagraphs(data.bodyText))
        {
            // 「半魚人：餓...好餓」這種開頭的段落改用**有名字框**的樣式播。
            // 認得的名字列在 EventData.speakerNames —— 見 SpeakerLine 的說明
            string who, line;
            EldritchMile.Core.SpeakerLine.TrySplit(para, data.speakerNames, out who, out line);

            introLines.Add(new Line { speaker = who, text = line, portrait = data.image });
        }

        Debug.Log($"[事件] 開始演出：{data.title}（{introLines.Count} 段）");

        // 新手教學的【觸發祭壇事件】那一步在等這個。沒填就不發
        TutorialSignal.Raise(data.startSignal);
    }

    /// <summary>
    /// 把一整段內文切成一句一句。
    ///
    /// 切在**換行**上：文案在資產裡怎麼分行，玩家就怎麼一段一段看到。
    /// 連續的空行算同一個斷點，所以段落之間空一行也不會多冒出一則空訊息。
    /// </summary>
    private static List<string> SplitParagraphs(string body)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(body)) return result;

        string[] parts = body.Replace("\r\n", "\n").Split('\n');

        for (int i = 0; i < parts.Length; i++)
        {
            string t = parts[i].Trim();
            if (!string.IsNullOrEmpty(t)) result.Add(t);
        }

        // 整段沒有換行時，至少要有一則
        if (result.Count == 0) result.Add(body);

        return result;
    }

    protected override void ShowOptions(RunContext run)
    {
        if (data == null) { ShowResult(""); return; }

        // 沒有選項的事件（純敘述）：內文播完就套效果、等玩家按結束。
        // 大綱的《無人的小船》是「一個沒有標籤的選項」，效果掛在上面
        if (data.options.Count == 0)
        {
            ShowResult("");
            return;
        }

        if (data.options.Count == 1 && string.IsNullOrEmpty(data.options[0].label))
        {
            // ⚠️ 這裡以前只丟 `ApplyOption` 的回傳值（＝效果提示），
            //    **把 `resultText` 整段吞掉了** —— 純敘述的事件（沒有選項可選的那種）
            //    因此永遠看不到自己的結果文字。
            //    《第一輪體驗結束》的「謝謝測試」就是這樣消失的。
            //
            //    多選項那條（HandleChosen）本來就是「結果文字 ＋ 效果提示」，
            //    兩條各寫一次才會走歪 —— 現在共用 BuildResultBody()。
            ShowResult(BuildResultBody(data.options[0]));
            return;
        }

        if (Options == null)
        {
            Debug.LogWarning("[事件] 場上沒有 DialogueOptionsPanel，選項顯示不出來");
            ShowResult("");
            return;
        }

        var texts = new List<string>();
        for (int i = 0; i < data.options.Count && i < Options.SlotCount; i++)
        {
            texts.Add(data.options[i].label);
        }

        Options.OnOptionClicked -= HandleChosen;
        Options.OnOptionClicked += HandleChosen;

        // 事件的選項**不需要判定** —— 玩家是在做選擇，不是在闖關。
        // 顯示機率只會誤導（跟商店挑商品是同一個道理）
        Options.Show(texts, null, DialogueOptionUI.Mode.PlainChoice);
    }

    protected override void Unsubscribe()
    {
        if (Options != null) Options.OnOptionClicked -= HandleChosen;
        if (endButton != null) endButton.onClick.RemoveListener(EndEvent);

        // 背景是生成出來的，離開時要收掉 —— 不收的話會一路留到下一站
        backdrop?.Despawn();
    }

    private void HandleChosen(DialogueOptionUI option)
    {
        if (data == null || option == null) return;
        if (option.Index < 0 || option.Index >= data.options.Count) return;

        EventData.Option picked = data.options[option.Index];

        Options?.HideAll();

        ShowResult(BuildResultBody(picked));
    }

    /// <summary>
    /// 套用效果，並把「結果文字 ＋ 效果提示」接成要播的那一段。
    ///
    /// ⚠️ **兩條路都要走這一支**（有選項的、沒選項的）。
    /// 以前沒選項那條只丟效果提示、把 `resultText` 整段吞掉 ——
    /// 純敘述的事件因此永遠看不到自己的結果文字，而且不會有任何錯誤訊息。
    ///
    /// 效果提示是**自動生成的**，不必在文案裡手寫「（【深淵】的侵蝕度+5%）」——
    /// 那樣數字改了兩邊會不同步。
    /// </summary>
    private string BuildResultBody(EventData.Option option)
    {
        if (option == null) return "";

        string notes = ApplyOption(option);
        string body = option.resultText ?? "";

        if (!string.IsNullOrEmpty(notes)) body += string.Format(effectLineFormat, notes);

        return body;
    }

    /// <summary>
    /// 播結果內文，播完顯示結束鍵。
    ///
    /// 【為什麼不用基底的 BeginOutro】那一支播完會自動 `Finish()`，
    /// 而事件要等玩家按鈕。所以這裡自己排隊、自己等 `OnAllClosed`，
    /// **刻意不把 phase 推進到 Outro** —— 推了基底就會替我們結束。
    /// </summary>
    private void ShowResult(string body)
    {
        if (PopupService.Instance == null) { Finish(); return; }

        awaitingEnd = true;

        var lines = SplitParagraphs(body);
        if (lines.Count == 0) return;   // 沒有結果文字 → Update 會直接讓結束鍵出現

        System.Collections.Generic.List<string> names =
            data != null ? data.speakerNames : null;

        // ⚠️ 第一段一定要用 ShowInstant，不可以排隊。
        //
        // `PopupService.Enqueue` 只有在**對話框沒開著**時才會立刻播；
        // 這一刻框還開著（正顯示最後一句內文），所以排進去的東西會卡在佇列裡，
        // 玩家要「再點一下」才看得到結算 —— 但 Console 早就印出來了，
        // 看起來就像結算掉了一拍。
        string who0, line0;
        if (EldritchMile.Core.SpeakerLine.TrySplit(lines[0], names, out who0, out line0))
            PopupService.Instance.ShowSpeechInstant(who0, line0);
        else
            PopupService.Instance.ShowInstant(lines[0]);

        // 其餘的照常排隊，玩家一句一句點過去
        for (int i = 1; i < lines.Count; i++)
        {
            string who, line;
            if (EldritchMile.Core.SpeakerLine.TrySplit(lines[i], names, out who, out line))
                PopupService.Instance.ShowSpeech(who, line);
            else
                PopupService.Instance.ShowSystemWithCloseUp(lines[i], data != null ? data.image : null);
        }
    }

    /// <summary>
    /// 等「話真的講完」再放出結束鍵。
    ///
    /// 【為什麼是輪詢而不是事件】`OnAllClosed` 是在玩家**點擊推進**、
    /// 而且佇列剛好空掉時才發的 —— 用它的話結束鍵要多點一下才出現。
    /// 我們要的是「最後一句打完的當下」，那沒有對應的事件可訂閱。
    /// </summary>
    private void Update()
    {
        if (!awaitingEnd) return;
        if (endButton == null || endButton.gameObject.activeSelf) return;

        if (PopupService.Instance != null && PopupService.Instance.IsIdle)
        {
            SetEndButtonVisible(true);
        }
    }

    private void SetEndButtonVisible(bool visible)
    {
        if (endButton == null) return;
        endButton.gameObject.SetActive(visible);
    }

    /// <summary>玩家按了結束。這是事件唯一的出口。</summary>
    private void EndEvent()
    {
        awaitingEnd = false;
        SetEndButtonVisible(false);

        // 基底的 Finish 會解除訂閱、收選項、放開 HoldOpen 並回報完成
        Finish();
    }

    /// <summary>
    /// 套用一個選項的所有效果，回傳合併好的提示字串。
    ///
    /// 企劃要的是「**變動的當下有提示，但遊戲過程不會持續顯示**」——
    /// 所以提示就接在這一次的結果內文後面，不另外做常駐的 UI。
    /// </summary>
    private string ApplyOption(EventData.Option option)
    {
        if (option == null) return "";

        // 教學要分辨玩家走了哪一條（祈禱 / 無視）。
        // 在效果之前發 —— 教學的下一步可能就是要看那個效果的結果
        TutorialSignal.Raise(option.chosenSignal);

        RunContext run = GameFlowManager.Instance != null ? GameFlowManager.Instance.Run : null;
        if (run == null) return "";

        var notes = new List<string>();

        for (int i = 0; i < option.effects.Count; i++)
        {
            EventEffect e = option.effects[i];
            if (e == null) continue;

            string note = e.Apply(run);
            if (!string.IsNullOrEmpty(note)) notes.Add(note);
        }

        return string.Join(effectSeparator, notes.ToArray());
    }
}
