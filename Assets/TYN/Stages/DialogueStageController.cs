using System.Collections.Generic;
using UnityEngine;
using EldritchMile.Core;

// 手牌區住在 Explore 命名空間，但它其實是**跨環節共用**的常駐 UI ——
// 探索的寶箱與這裡的對話選項用的是同一組手牌。
// （名稱是歷史因素：它先為探索而生。日後若要搬進 Core，改這一行即可。）
using EldritchMile.Explore;

/// <summary>
/// 對話 Stage（Phase 6 的核心，C18 的完整形狀）。
///
/// 開場白 → 攤出選項 → **玩家用機率卡打在選項上** → 判定結果反映在選項內文 → 收尾。
///
/// 【這裡才是 C18 原本在講的東西】企劃草圖上那三列「A 50 / B 50 / C 50」
/// 指的就是這個畫面：hover 一張手牌，每個選項各自顯示成功率。
/// 探索房間的寶箱只是「只有一個選項」的特例 —— 同一套規則引擎
/// （`DialogueEncounterController`）兩邊共用，這裡不做任何判斷。
///
/// 【一起落地的三條約束】
///   · C18① 主要目標選定 —— 三個選項同時在場，手牌變暗必須知道是對哪一個
///   · C18③ 判定結果反映在選項內文 —— 選項現在有文字元件了
///   · C18⑦ 蓄意失敗合法 —— 判定成功也不自動結束，玩家自己按結束
/// </summary>
public class DialogueStageController : ChoiceStageController
{
    public override StageType Stage => StageType.Dialogue;

    [System.Serializable]
    public class Option
    {
        [Tooltip("選項內文")]
        [TextArea(2, 3)] public string text = "";

        [Tooltip("這個選項吃哪一種思路。與手牌的屬性相剋決定成功率")]
        public ExploreAttribute attribute = ExploreAttribute.None;

        [Tooltip("判定成功時接在選項內文後面的字（C18③）")]
        public string successSuffix = "　→ 成功";

        [Tooltip("判定失敗時接在選項內文後面的字（C18③）")]
        public string failSuffix = "　→ 失敗";

        [Tooltip("判定成功時說的話")]
        [TextArea(2, 3)] public string successText = "";

        [Tooltip("判定失敗時說的話")]
        [TextArea(2, 3)] public string failText = "";

        [Tooltip("成功後獲得的道具 id")]
        public List<string> grantItemIdsOnSuccess = new List<string>();
    }

    [Header("選項")]
    [Tooltip("最多三個 —— 對話框只有三個選項槽")]
    public List<Option> options = new List<Option>();

    [Header("打牌")]
    [Tooltip("手牌來源。牌組內容從 RunContext.exploreDeck 同步過來")]
    public ExplorationDeck explorationDeck;

    [Tooltip("開場抽幾張。C18⑤：這個數字同時也是可嘗試的次數上限")]
    [Min(1)] public int cardsPerEncounter = 5;

    // 規則引擎與手牌區都常駐 EventScene，prefab 無法在 Inspector 引用場景物件
    private DialogueEncounterController Encounter => DialogueEncounterController.Instance;
    private ExploreHandUI HandUI => ExploreHandUI.Instance;

    private RunContext run;

    protected override void OnPrepare(RunContext context)
    {
        run = context;
        SyncDeckFromRun();
    }

    /// <summary>與 ExploreStageController 相同：牌組的真相在 RunContext，跨房間保存。</summary>
    private void SyncDeckFromRun()
    {
        if (explorationDeck == null || run == null) return;

        if (run.exploreDeck.Count == 0)
        {
            run.exploreDeck.AddRange(explorationDeck.startingDeck);
        }
        else
        {
            explorationDeck.startingDeck.Clear();
            explorationDeck.startingDeck.AddRange(run.exploreDeck);
        }

        explorationDeck.InitializeDeck();
        Debug.Log($"[對話] 牌組同步完成：{explorationDeck.startingDeck.Count} 張");
    }

    protected override void ShowOptions(RunContext context)
    {
        if (context != null) run = context;

        if (Options == null || Encounter == null)
        {
            Debug.LogWarning("[對話] 缺少 DialogueOptionsPanel 或 DialogueEncounterController，無法進行");
            BeginOutro();
            return;
        }

        var texts = new List<string>();
        var attrs = new List<ExploreAttribute>();

        for (int i = 0; i < options.Count && i < Options.SlotCount; i++)
        {
            Option o = options[i];
            if (o == null || string.IsNullOrEmpty(o.text)) continue;

            texts.Add(o.text);
            attrs.Add(o.attribute);
        }

        if (texts.Count == 0)
        {
            Debug.LogWarning("[對話] 沒有設定任何選項，直接離開");
            BeginOutro();
            return;
        }

        Options.OnOptionClicked -= HandleOptionClicked;
        Options.OnOptionResolved -= HandleOptionResolved;
        Options.OnOptionClicked += HandleOptionClicked;
        Options.OnOptionResolved += HandleOptionResolved;

        IReadOnlyList<DialogueOptionUI> shown =
            Options.Show(texts, attrs, DialogueOptionUI.Mode.ProbabilityTarget);

        // 抽手牌
        var hand = new List<CardInstanceExplore>();
        if (explorationDeck != null)
        {
            explorationDeck.DrawCards(cardsPerEncounter);
            hand.AddRange(explorationDeck.Hand);
        }

        if (hand.Count == 0)
        {
            Debug.LogWarning("[對話] 牌組抽不到任何卡，選項無法判定");
            BeginOutro();
            return;
        }

        // 先開手牌區再 Begin —— 手牌區在 Start 才訂閱事件，順序反了會漏掉第一次 OnHandChanged
        HandUI?.Show();

        Encounter.OnEncounterEnded -= HandleEncounterEnded;
        Encounter.OnEncounterEnded += HandleEncounterEnded;

        var targets = new List<IProbabilityTarget>();
        for (int i = 0; i < shown.Count; i++) targets.Add(shown[i]);

        Encounter.Begin(hand, targets);
    }

    protected override void Unsubscribe()
    {
        if (Options != null)
        {
            Options.OnOptionClicked -= HandleOptionClicked;
            Options.OnOptionResolved -= HandleOptionResolved;
        }

        if (Encounter != null)
        {
            Encounter.OnEncounterEnded -= HandleEncounterEnded;
            Encounter.HandExhaustedInterceptor = null;
        }
    }

    // ==========================================
    // 點擊：兩種語意
    // ==========================================
    /// <summary>
    /// 點選項 ＝ **把選取中的卡打在它身上**，與探索房間的寶箱完全一致。
    ///
    /// 【操作方式定案 2026-08-16】兩條路並存，語意一致：
    ///   · 拖曳 —— 把卡拖到選項上放開
    ///   · 點選 —— 先點卡（標記）→ 再點選項（＝出牌）
    ///
    /// 【為什麼不做「點選項＝選定主要目標」】那會讓同一個點擊有兩種意思，
    /// 而玩家分不出自己現在觸發的是哪一種。既然探索已經是「先卡後目標」，
    /// 對話沿用同一套，玩家只要學一次。
    ///
    /// C18① 的主要目標選定因此暫時沒有入口 —— 手牌變暗在多選項時會關閉
    /// （「無效」沒有唯一答案，硬壓暗會騙人）。要做的話得另找一個不搶點擊的手勢。
    /// </summary>
    private void HandleOptionClicked(DialogueOptionUI option)
    {
        if (option == null || Encounter == null || !Encounter.IsActive) return;

        if (HandUI != null && HandUI.TryPlaySelectedOn(option)) return;

        // 沒有選取的卡 —— 什麼都不做。玩家還沒選牌就點選項，是操作順序反了
        Debug.Log("[對話] 先點一張手牌，再點選項");
    }

    // ==========================================
    // 判定結果
    // ==========================================
    private void HandleOptionResolved(DialogueOptionUI option, bool success, float usedRate)
    {
        if (option == null || option.Index < 0 || option.Index >= options.Count) return;

        Option data = options[option.Index];
        if (data == null) return;

        // C18③：結果反映在**選項內文**
        option.AppendResultText(success ? data.successSuffix : data.failSuffix);

        // 同時反映在對話框正文。用 ShowInstant（即時替換）而不是排隊 ——
        // C18 的設計是連續嘗試，每出一張牌都要點掉一則訊息會被打斷得很嚴重
        string body = success ? data.successText : data.failText;
        if (!string.IsNullOrEmpty(body))
        {
            PopupService.Instance?.ShowInstant(
                Encounter != null ? Encounter.WithAttemptLine(body) : body);
        }

        if (success && run != null)
        {
            for (int i = 0; i < data.grantItemIdsOnSuccess.Count; i++)
            {
                string id = data.grantItemIdsOnSuccess[i];
                if (string.IsNullOrEmpty(id)) continue;

                run.AddItem(id);
                Debug.Log($"[對話] 選項成功，獲得：{GameFlowManager.ItemName(id)}");
            }
        }

        // ⚠️ 這裡刻意**不**因為成功就結束 —— 蓄意失敗是合法策略（C18⑦）。
        //    結束由玩家按手牌區的結束鈕，或手牌用盡自動觸發。
    }

    private void HandleEncounterEnded()
    {
        if (Encounter != null) Encounter.OnEncounterEnded -= HandleEncounterEnded;

        HandUI?.Hide();

        // Q13 暫行做法：環節結束就把剩下的手牌棄掉
        if (explorationDeck != null) explorationDeck.DiscardHand();

        BeginOutro();
    }
}
