using System.Collections.Generic;
using UnityEngine;
using EldritchMile.Core;

/// <summary>
/// 特殊事件 Stage（C16：授予神牌）。
///
/// 開場白 → 攤出幾張牌 → 玩家挑一張 → 進牌組 → 收尾 → 回地圖。
///
/// 【給的是探索牌，不是真正的神牌】真正的神牌是**戰鬥用卡**，
/// 要進 `RunStateManager.savedDeck`（Romtyui 那邊），效果與平衡也歸他們。
/// 我方的份就是「事件把牌給出去」這一段 —— 本身範圍很小，
/// 等戰鬥接上後把 `GrantCard` 換成寫進對方的牌組即可，選項流程不用動。
///
/// 劇情大綱裡小說家／克拉夫特的「杜撰故事 — 抽取 1 張神牌」就落在這個系統上。
/// </summary>
public class SpecialEventStageController : ChoiceStageController
{
    public override StageType Stage => StageType.SpecialEvent;

    [System.Serializable]
    public class Offer
    {
        [Tooltip("可挑的卡")]
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
            if (o == null || o.card == null) continue;

            texts.Add(!string.IsNullOrEmpty(o.label) ? o.label : o.card.cardName);
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
        if (o == null || o.card == null) return;

        // 進的是探索牌組（RunContext），跨房間保存 —— 下一個探索房間就抽得到
        run?.exploreDeck.Add(o.card);

        string text = !string.IsNullOrEmpty(o.takenText)
            ? o.takenText
            : string.Format(defaultTakenFormat, o.card.cardName);

        PopupService.Instance?.ShowInstant(text);
        Debug.Log($"[特殊事件] 獲得卡牌：{o.card.cardName}（牌組現在 {run?.exploreDeck.Count} 張）");

        BeginOutro();
    }
}
