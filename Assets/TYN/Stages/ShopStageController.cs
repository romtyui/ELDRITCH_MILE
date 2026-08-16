using System.Collections.Generic;
using UnityEngine;
using EldritchMile.Core;

/// <summary>
/// 商店 Stage（C15 的最小可玩版）。
///
/// 開場白 → 列出商品 → 玩家挑一件 → 拿到手 → 收尾 → 回地圖。
///
/// 【還沒有的】**貨幣**。`RunContext` 目前沒有任何貨幣欄位，那是還沒定案的設計 ——
/// 所以現在是「挑一件帶走」而不是「買」。等貨幣定了，在 `HandleChosen` 裡加扣款即可，
/// 選項 UI 與流程都不用動。
///
/// 【C15 的「買完留在商店繼續買」也還沒有】現在挑一件就離開。
/// 要做的話把 `BeginOutro()` 換成「重新 ShowOptions，並把買過的移除」。
/// </summary>
public class ShopStageController : ChoiceStageController
{
    public override StageType Stage => StageType.Shop;

    [System.Serializable]
    public class Stock
    {
        [Tooltip("商品的道具 id。要在 ItemDatabase 裡登記過，否則玩家會看到 id")]
        public string itemId = "";

        [Tooltip("選項上顯示的文字。留空則自動用道具的顯示名")]
        public string label = "";

        [Tooltip("選了之後說的話。留空則用預設格式")]
        [TextArea(2, 3)] public string takenText = "";
    }

    [Header("商品")]
    [Tooltip("最多三件 —— 對話框只有三個選項槽")]
    public List<Stock> stock = new List<Stock>();

    [Tooltip("拿走商品後的預設台詞。{0} = 道具名")]
    [TextArea(2, 3)]
    public string defaultTakenFormat = "你拿走了「{0}」。";

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

        for (int i = 0; i < stock.Count && i < Options.SlotCount; i++)
        {
            Stock s = stock[i];
            if (s == null || string.IsNullOrEmpty(s.itemId)) continue;

            texts.Add(!string.IsNullOrEmpty(s.label)
                ? s.label
                : GameFlowManager.ItemName(s.itemId));
        }

        if (texts.Count == 0)
        {
            Debug.LogWarning("[商店] 沒有設定任何商品，直接離開");
            BeginOutro();
            return;
        }

        Options.OnOptionClicked -= HandleChosen;
        Options.OnOptionClicked += HandleChosen;

        // 商品不需要判定，所以是 PlainChoice —— 顯示機率只會誤導玩家
        Options.Show(texts, null, DialogueOptionUI.Mode.PlainChoice);
    }

    protected override void Unsubscribe()
    {
        if (Options != null) Options.OnOptionClicked -= HandleChosen;
    }

    private void HandleChosen(DialogueOptionUI option)
    {
        if (option == null || option.Index < 0 || option.Index >= stock.Count) return;

        Stock s = stock[option.Index];
        if (s == null || string.IsNullOrEmpty(s.itemId)) return;

        // 這裡就是日後扣錢的位置。現在沒有貨幣，所以直接給。
        run?.AddItem(s.itemId);

        string name = GameFlowManager.ItemName(s.itemId);
        string text = !string.IsNullOrEmpty(s.takenText)
            ? s.takenText
            : string.Format(defaultTakenFormat, name);

        PopupService.Instance?.ShowInstant(text);
        Debug.Log($"[商店] 玩家取得：{name}");

        BeginOutro();
    }
}
