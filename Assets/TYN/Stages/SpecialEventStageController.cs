using System.Collections.Generic;
using UnityEngine;
using EldritchMile.Core;

/// <summary>
/// 特殊事件 Stage（C16：授予神牌）。
///
/// 開場白 → 攤出幾張牌 → 玩家挑一張 → 進牌組 → 收尾 → 回地圖。
///
/// 【✅ 2026-08-27：戰鬥接上了，現在可以真的給神牌】
/// 原本只能給探索牌，因為戰鬥牌組（`RunStateManager.savedDeck`）當時還碰不到。
/// 現在 `PlayerVitals.AddCardToDeck()` 通了，所以 `Offer` 同時支援兩種：
///   · `battleCard`（`CardData`）→ 進**戰鬥牌庫**。神牌走這一支
///   · `card`（`CardDataExplore`）→ 進探索牌組
/// 兩個都填的話**兩個都給**（一次事件同時影響兩副牌組是合法的設計）。
///
/// 劇情大綱裡小說家／克拉夫特的「杜撰故事 — 抽取 1 張神牌」就落在這個系統上。
/// </summary>
public class SpecialEventStageController : ChoiceStageController
{
    public override StageType Stage => StageType.SpecialEvent;

    [System.Serializable]
    public class Offer
    {
        [Tooltip("進**戰鬥牌庫**的卡（Romtyui 的 CardData）。神牌走這一支。\n" +
                 "會加進 RunStateManager.savedDeck，下一場戰鬥就抽得到")]
        public CardData battleCard;

        [Tooltip("進**探索牌組**的卡。留空就只給上面那張戰鬥牌")]
        public CardDataExplore card;

        [Tooltip("選項上顯示的文字。留空則用卡名")]
        public string label = "";

        [Tooltip("選了之後說的話。留空則用預設格式")]
        [TextArea(2, 3)] public string takenText = "";
    }

    [Header("可挑的牌")]
    [Tooltip("最多三張 —— 對話框只有三個選項槽")]
    public List<Offer> offers = new List<Offer>();

    [Tooltip("取得後的預設台詞。{0} = 卡名")]
    [TextArea(2, 3)]
    public string defaultTakenFormat = "「{0}」進了你的牌組。";

    [Tooltip("把 Taken Text 的**最後一段**當成結算，挪到收尾台詞之後才播。\n\n" +
             "「獲得了什麼」應該是玩家離開前看到的**最後一件事**，不是夾在中間。\n" +
             "順序會變成：\n" +
             "　你跪了下來…（劇情）→ 祭壇仍然是塌的（收尾）→ 牌進了牌庫（結算）\n\n" +
             "【怎麼切】**空行 ＝ 分段**，跟對話框的分頁規則同一套 ——\n" +
             "所以文案照原本那樣寫就好，不必為了這個功能改成兩欄。\n\n" +
             "整段沒有空行時，整段都當結算（那通常就是 Default Taken Format 那種一句話）。\n\n" +
             "取消勾選 = 回到舊行為（整段在選完的當下一次播完）")]
    public bool settlementLast = true;

    private RunContext run;

    protected override void OnPrepare(RunContext context)
    {
        run = context;
    }

    protected override void ShowOptions(RunContext context)
    {
        if (context != null) run = context;
        if (Options == null) { BeginOutro(); return; }

        var texts = new List<string>();

        for (int i = 0; i < offers.Count && i < Options.SlotCount; i++)
        {
            Offer o = offers[i];
            if (o == null || (o.card == null && o.battleCard == null)) continue;

            texts.Add(!string.IsNullOrEmpty(o.label) ? o.label : CardNameOf(o));
        }

        if (texts.Count == 0)
        {
            Debug.LogWarning("[特殊事件] 沒有設定任何可挑的牌，直接離開");
            BeginOutro();
            return;
        }

        Options.OnOptionClicked -= HandleChosen;
        Options.OnOptionClicked += HandleChosen;

        Options.Show(texts, null, DialogueOptionUI.Mode.PlainChoice);
    }

    protected override void Unsubscribe()
    {
        if (Options != null) Options.OnOptionClicked -= HandleChosen;
    }

    private void HandleChosen(DialogueOptionUI option)
    {
        if (option == null || option.Index < 0 || option.Index >= offers.Count) return;

        Offer o = offers[option.Index];
        if (o == null || (o.card == null && o.battleCard == null)) return;

        // ── 戰鬥牌庫（神牌走這一支）──
        if (o.battleCard != null)
        {
            if (PlayerVitals.AddCardToDeck(o.battleCard))
            {
                Debug.Log($"[特殊事件] 神牌「{o.battleCard.cardName}」進了戰鬥牌庫" +
                          $"（現在 {PlayerVitals.DeckCount} 張）");
            }
            else
            {
                // AddCardToDeck 自己會說明原因（通常是這場 run 還沒初始化牌組）。
                // 這裡不再重複吵，但要讓玩家知道「沒拿到」而不是靜靜地少一張
                Debug.LogWarning($"[特殊事件] 神牌「{o.battleCard.cardName}」加不進戰鬥牌庫");
            }
        }

        // ── 探索牌組（跨房間保存，下一個探索房間就抽得到）──
        if (o.card != null)
        {
            run?.exploreDeck.Add(o.card);
            Debug.Log($"[特殊事件] 探索牌「{o.card.cardName}」進了探索牌組" +
                      $"（現在 {run?.exploreDeck.Count} 張）");
        }

        string text = !string.IsNullOrEmpty(o.takenText)
            ? o.takenText
            : string.Format(defaultTakenFormat, CardNameOf(o));

        string body, settlement;
        SplitSettlement(text, out body, out settlement);

        // ⚠️ 前半段要用 ShowInstant，不可以排隊。
        //    這一刻對話框還開著（正顯示那個問句），排進去的東西會卡在佇列裡，
        //    玩家要再點一下才看得到 —— 事件那邊踩過同一個坑
        if (!string.IsNullOrEmpty(body)) PopupService.Instance?.ShowInstant(body);

        outroTailLines.Clear();
        if (!string.IsNullOrEmpty(settlement))
        {
            // 沒有前半段的話（整段都是結算），收尾之前總得先講一句，
            // 不然玩家點下去會有一拍完全沒有反應
            if (string.IsNullOrEmpty(body)) PopupService.Instance?.ShowInstant(settlement);
            else outroTailLines.Add(new Line { text = settlement });
        }

        BeginOutro();
    }

    /// <summary>
    /// 把「劇情」與「結算」拆開。**空行 ＝ 分段**，跟對話框的分頁規則同一套。
    ///
    /// 最後一段當結算（「【深淵】的牌進了你的牌庫。」），前面全部是劇情。
    /// 整段沒有空行時 body 是空的、整段都是結算 ——
    /// 那通常就是 `defaultTakenFormat` 那種一句話的情況。
    ///
    /// `settlementLast` 取消勾選就整段都當劇情，回到舊行為。
    /// </summary>
    private void SplitSettlement(string text, out string body, out string settlement)
    {
        body = text;
        settlement = "";

        if (!settlementLast || string.IsNullOrEmpty(text)) return;

        string norm = text.Replace("\r\n", "\n");
        int cut = norm.LastIndexOf("\n\n");

        if (cut < 0)
        {
            // 沒有空行 —— 整段都是結算
            body = "";
            settlement = norm.Trim();
            return;
        }

        body = norm.Substring(0, cut).Trim();
        settlement = norm.Substring(cut + 2).Trim();
    }

    /// <summary>選項上顯示的卡名。戰鬥牌優先 —— 神牌事件給的就是它。</summary>
    private static string CardNameOf(Offer o)
    {
        if (o == null) return "";
        if (o.battleCard != null) return o.battleCard.cardName;
        return o.card != null ? o.card.cardName : "";
    }
}
