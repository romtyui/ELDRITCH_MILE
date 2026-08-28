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

        PopupService.Instance?.ShowInstant(text);

        BeginOutro();
    }

    /// <summary>選項上顯示的卡名。戰鬥牌優先 —— 神牌事件給的就是它。</summary>
    private static string CardNameOf(Offer o)
    {
        if (o == null) return "";
        if (o.battleCard != null) return o.battleCard.cardName;
        return o.card != null ? o.card.cardName : "";
    }
}
