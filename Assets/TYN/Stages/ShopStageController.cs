using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EldritchMile.Core;
using EldritchMile.Shop;
using EldritchMile.UI;

/// <summary>
/// 商店 Stage。
///
/// ────────────────────────────────────────────────────────
/// 【它不再是「三個選項」了】
///
/// 舊版商店借用對話框的三個選項槽當商品列，挑一件就走。那是 Phase 6 的最小可玩版。
/// 現在改成貨架：八格商品、有價格、可以買到不想買為止，店主在旁邊用氣泡講話。
///
/// 連帶不再繼承 `ChoiceStageController` —— 那個基底的形狀是
/// 「開場白 → 選項 → 收尾」，而且會強制把對話框撐開。
/// 對話框會蓋住貨架，而商店的開場白現在是氣泡（bark），不是排隊播放的對白。
///
/// ────────────────────────────────────────────────────────
/// 【賣什麼不是寫死的】商品由 <see cref="LootTable"/> 抽出來，跟寶箱共用同一套。
/// 想讓漁村多出漁獲，改的是那張表的權重，不是這支程式。
///
/// 【亂數綁節點】同一個節點重進商店要看到同一批貨 ——
/// 否則玩家離開再進來就能重骰，等於商店沒有隨機性。
/// </summary>
public class ShopStageController : StageController
{
    public override StageType Stage => StageType.Shop;

    [Header("店主")]
    [Tooltip("店主的隱形點擊區。進場的寒暄由這裡發出")]
    public CharacterHitbox shopkeeper;

    [Header("貨架")]
    public ShopPanelUI shelf;

    [Tooltip("這間店預設賣什麼。**沒指定的話貨架會是空的** —— 商品不寫在這支程式裡")]
    public LootTable stockTable;

    [System.Serializable]
    public class RegionalStock
    {
        [Tooltip("區域 id。目前比對的是節點的 Content Id（漁村 = village）")]
        public string regionId = "";

        [Tooltip("這個區域改用哪張表。整張替換，不是疊加")]
        public LootTable table;
    }

    [Tooltip(
        "依區域替換整張貨表。商人是**移動商店**，但他在漁村補的貨自然會是漁獲多一點。\n\n" +
        "【為什麼是整張替換，不是「只換食物那一格」】各區域的表可以用 Table 條目\n" +
        "指回共用的卡片池與遺物池，所以「只有食物不同」寫起來一樣短，\n" +
        "但需要時又能整組改配比（例如礦山金幣多、遺物也多）。\n\n" +
        "⚠️ 區域目前是從節點的 Content Id 讀的。**真正的區域系統還不存在** ——\n" +
        "等它做好，改的是 ResolveStockTable() 那一支，資料不用動")]
    public List<RegionalStock> regionalStock = new List<RegionalStock>();

    [Tooltip("要擺幾件。0 = 填滿整個貨架")]
    [Min(0)] public int stockCount = 0;

    [Header("離開")]
    [Tooltip("EXIT 標籤上的 Button。**留空的話玩家會被困在店裡** —— 沒有別的出口。\n" +
             "兩段式：hover 讓標籤滑出來（SlideOutTab）→ 點擊跳出確認面板")]
    public Button exitButton;

    [Tooltip("EXIT 標籤的滑出元件。確認面板跳出來時會強制收回去 ——\n" +
             "面板蓋住標籤之後收不到 OnPointerExit，不收的話它會一直卡在伸出來的狀態")]
    public SlideOutTab exitTab;

    [Tooltip("「確定要離開嗎？」的確認面板。與探索的 ContinueAskPanel 是同一個形狀。\n" +
             "留空則點 EXIT 直接離開（不建議 —— 誤觸就出去了）")]
    public GameObject leaveAskPanel;

    [Tooltip("面板上的「是」。接 → 真的離開")]
    public Button confirmLeaveButton;

    [Tooltip("面板上的「否」。接 → 留在店裡。\n" +
             "⚠️ 問句是「確定要離開嗎？」，所以 YES = 離開。\n" +
             "接反了玩家按「是」會留在原地，而且不會有任何錯誤訊息")]
    public Button cancelLeaveButton;

    private readonly PanelToggle askToggle = new PanelToggle();

    [Header("台詞")]
    [Tooltip("成交時的備援台詞。{0} = 商品名。\n\n" +
             "**角色本身有 Purchase Lines 的話會優先用角色的** ——\n" +
             "台詞屬於角色，不屬於這個節點，同一個商人在哪開店都講一樣的話")]
    public string boughtFormat = "「{0}」啊，好眼光。";

    [Tooltip("錢不夠時說的話")]
    public string tooPoorLine = "……你這點錢不夠。";

    [Tooltip("賣光時說的話。留空則不說")]
    public string soldOutLine = "架上就這些了。";

    [Tooltip("離開時說的話。留空則直接走")]
    public string farewellLine = "";

    [Tooltip("說完道別等幾秒才回地圖")]
    [Min(0f)] public float farewellSeconds = 0.8f;

    private RunContext run;
    private bool leaving;

    // ==========================================
    public override void OnStageEnter(RunContext context)
    {
        run = context;
        leaving = false;

        if (shelf == null)
        {
            Debug.LogWarning("[商店] 沒有指定 ShopPanelUI，沒有貨架可以看，直接離開");
            ReportComplete();
            return;
        }

        shelf.OnSlotClicked -= HandleSlotClicked;
        shelf.OnSlotClicked += HandleSlotClicked;

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(AskLeave);
            exitButton.onClick.AddListener(AskLeave);
        }
        else
        {
            Debug.LogWarning("[商店] 沒有指定離開按鈕 —— 玩家進得來但出不去");
        }

        if (confirmLeaveButton != null)
        {
            confirmLeaveButton.onClick.RemoveListener(ConfirmLeave);
            confirmLeaveButton.onClick.AddListener(ConfirmLeave);
        }

        if (cancelLeaveButton != null)
        {
            cancelLeaveButton.onClick.RemoveListener(CancelLeave);
            cancelLeaveButton.onClick.AddListener(CancelLeave);
        }

        askToggle.Set(leaveAskPanel, false);

        StockShelf();
    }

    public override IEnumerator OnStageReady()
    {
        // 寒暄放在這裡而不是 OnStageEnter：進場時畫面還是全黑的，
        // 氣泡冒出來玩家看不到，等淡入完成才說才有意義
        if (shopkeeper != null) shopkeeper.Greet();

        yield break;
    }

    public override IEnumerator OnStageExit()
    {
        if (shelf != null) shelf.OnSlotClicked -= HandleSlotClicked;
        if (exitButton != null) exitButton.onClick.RemoveListener(AskLeave);
        if (confirmLeaveButton != null) confirmLeaveButton.onClick.RemoveListener(ConfirmLeave);
        if (cancelLeaveButton != null) cancelLeaveButton.onClick.RemoveListener(CancelLeave);

        askToggle.Set(leaveAskPanel, false);

        // 轉場要用 Immediate —— Hide() 會播縮回去的動作，但 Stage 這一刻就要卸載了，
        // 動作播不完，殘影會被帶到下一個畫面
        SpeechBubbleUI.Instance?.HideImmediate();

        yield break;
    }

    // ==========================================
    // 進貨
    // ==========================================
    private void StockShelf()
    {
        int want = stockCount > 0 ? stockCount : shelf.SlotCount;

        LootTable table = ResolveStockTable();

        if (table == null)
        {
            Debug.LogWarning("[商店] 沒有指定 Stock Table，貨架會是空的。\n" +
                             "商品由 LootTable 決定 —— 請建一張表並指定給這間店。");
            shelf.HideAll();
            return;
        }

        var rng = new System.Random(ShopSeed());
        List<ItemStack> goods = LootService.RollExactly(table, rng, want);

        shelf.Bind(goods);

        Debug.Log($"[商店] 進貨 {goods.Count} 件（{table.name}，種子 {ShopSeed()}）");
    }

    /// <summary>
    /// 這一站要用哪張貨表。找不到對應區域就用預設的。
    ///
    /// ⚠️ 區域現在是借用節點的 `contentId` —— **資料模型裡還沒有「區域」這個欄位**。
    /// 這是刻意留的接縫：等區域系統做好，只要改這一支去讀真正的區域欄位，
    /// 表本身與 Inspector 上的設定都不用動。
    /// </summary>
    private LootTable ResolveStockTable()
    {
        EldritchMile.Core.RunNodeData node = run != null ? run.CurrentNode : null;
        string region = node != null ? node.contentId : null;

        if (!string.IsNullOrEmpty(region))
        {
            for (int i = 0; i < regionalStock.Count; i++)
            {
                RegionalStock r = regionalStock[i];
                if (r == null || r.table == null) continue;

                if (string.Equals(r.regionId, region, System.StringComparison.OrdinalIgnoreCase))
                {
                    return r.table;
                }
            }
        }

        return stockTable;
    }

    /// <summary>
    /// 這間店的亂數種子。**綁在節點上** —— 同一個節點重進要是同一批貨。
    /// 用 run 的種子混節點 id，這樣不同 run 的同一個位置也不會賣一樣的東西。
    /// </summary>
    private int ShopSeed()
    {
        int seed = run != null ? run.runSeed : 0;

        // 完整寫出命名空間：全域命名空間裡另有一個同名的 RunNodeData（舊碼），不寫會撞
        EldritchMile.Core.RunNodeData node = run != null ? run.CurrentNode : null;
        if (node != null && !string.IsNullOrEmpty(node.nodeId))
        {
            seed ^= node.nodeId.GetHashCode();
        }

        return seed;
    }

    // ==========================================
    // 買
    // ==========================================
    private void HandleSlotClicked(ShopSlotUI slot)
    {
        if (slot == null || slot.IsEmpty || slot.SoldOut || leaving) return;

        string name = GameFlowManager.ItemName(slot.ItemId);

        if (run == null)
        {
            Debug.LogWarning("[商店] 沒有 RunContext，買不了東西");
            return;
        }

        // ⚠️ 先確認付得起再扣。SpendMoney 本身也是全有或全無，
        //    這裡再判一次是為了「付不起」時要說話而不是靜靜地什麼都沒發生
        if (!run.SpendMoney(slot.Price))
        {
            Say(tooPoorLine);
            return;
        }

        run.AddItem(slot.ItemId, slot.Count);

        // 武器牌買了要真的進戰鬥牌組（不是只躺在背包裡）。
        // 牌組歸戰鬥端持有，這裡只是呼叫 —— 見 PlayerVitals.AddCardToDeck
        ItemData data = GameFlowManager.Item(slot.ItemId);
        if (data != null && data.grantsCard != null)
        {
            for (int i = 0; i < slot.Count; i++) PlayerVitals.AddCardToDeck(data.grantsCard);
        }

        slot.MarkSoldOut();
        shelf.RefreshAffordability();

        Say(PurchaseLine(name));
        Debug.Log($"[商店] 買下 {name} ×{slot.Count}，花費 {slot.Price}");

        if (!shelf.HasStock() && !string.IsNullOrEmpty(soldOutLine)) Say(soldOutLine);
    }

    /// <summary>
    /// 成交要說什麼。角色自己有台詞就用他的，沒有才退回本節點的格式字串。
    ///
    /// 【亂數不能綁 run 種子】商店的「賣什麼」要能重現，但「成交講哪一句」不行 ——
    /// 綁了種子的話同一間店買三次會聽到同一句話。
    /// </summary>
    private string PurchaseLine(string itemName)
    {
        CharacterData c = shopkeeper != null ? shopkeeper.Character : null;

        if (c != null)
        {
            string line = c.PickPurchaseLine(purchaseRng);
            if (!string.IsNullOrEmpty(line)) return line;
        }

        return string.Format(boughtFormat, itemName);
    }

    private readonly System.Random purchaseRng = new System.Random();

    private void Say(string line)
    {
        if (string.IsNullOrEmpty(line)) return;

        if (shopkeeper != null) shopkeeper.Say(line);
        else SpeechBubbleUI.Instance?.Show(line);
    }

    // ==========================================
    // 離開
    // ==========================================
    /// <summary>
    /// 點了 EXIT。**不是直接離開**，先問一次（C14 的兩段式確認）。
    ///
    /// 沒有指定面板時就直接走 —— 少一個必填欄位比讓玩家卡在店裡好，
    /// 但 Inspector 上已經寫明不建議這樣配。
    /// </summary>
    private void AskLeave()
    {
        if (leaving) return;

        if (leaveAskPanel == null) { BeginLeave(); return; }

        // ⚠️ 面板會蓋在標籤上，蓋住之後標籤收不到 OnPointerExit，
        //    不主動收的話它會一直卡在伸出來的狀態
        exitTab?.SetShown(false);

        askToggle.Set(leaveAskPanel, true);
    }

    private void CancelLeave()
    {
        askToggle.Set(leaveAskPanel, false);
    }

    private void ConfirmLeave()
    {
        askToggle.Set(leaveAskPanel, false);
        BeginLeave();
    }

    private void BeginLeave()
    {
        if (leaving) return;
        leaving = true;

        if (exitButton != null) exitButton.interactable = false;

        if (string.IsNullOrEmpty(farewellLine))
        {
            ReportComplete();
            return;
        }

        Say(farewellLine);
        StartCoroutine(LeaveAfterFarewell());
    }

    private IEnumerator LeaveAfterFarewell()
    {
        yield return new WaitForSecondsRealtime(farewellSeconds);
        ReportComplete();
    }
}
