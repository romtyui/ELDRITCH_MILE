using System.Collections.Generic;
using UnityEngine;
using EldritchMile.Core;

// 手牌區住在 Explore 命名空間，但它其實是**跨環節共用**的常駐 UI ——
// 探索的寶箱與這裡的對話選項用的是同一組手牌。
// （名稱是歷史因素：它先為探索而生。日後若要搬進 Core，改這一行即可。）
using EldritchMile.Explore;
using EldritchMile.UI;

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
///   · C18⑦ 蓄意失敗合法 —— 判定成功不會結束**整個環節**，玩家可以繼續打別的選項
///
/// 【2026-08-18 調整】**成功的選項會結案**（變暗、不再接受出牌）——
/// 對話選項的語意是「問過就問過了」，再問一次沒有意義，而且不結案就能重複刷獎勵。
/// 失敗不結案（那正是逐次衰減存在的理由）。三個都問完就自動收尾。
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

        [Tooltip("要用**屬性色**顯示的關鍵字。必須是上面內文裡的一小段，例如「什麼東西」。\n" +
                 "留空則整句都是原色。顏色由屬性決定，改色改 AttributeChart 一個地方就好")]
        public string keyword = "";

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

    /// <summary>
    /// 這一站已經發過獎勵的選項。
    ///
    /// 【為什麼需要】C18⑦「判定成功不自動結束」意味著玩家可以**對同一個選項再出一張牌**，
    /// 而每一次成功本來都會再發一次獎勵 —— 五張手牌就能把同一個撬棍刷三根。
    ///
    /// 【為什麼不是「延到結算才發」】專案的既有原則是
    /// **狀態立刻入袋、播報可以延後**（見 `ChestInteractable.Open`）——
    /// 延到結算才發的話，玩家中途離開就會白做工。
    /// 所以照樣立刻給，只是每個選項**只給一次**。
    /// </summary>
    private readonly HashSet<int> rewardedOptions = new HashSet<int>();

    /// 這一站已經成功過的選項。成功即結案（選項會變暗且不再接受出牌）
    private readonly HashSet<int> succeededOptions = new HashSet<int>();

    /// 這次實際攤出來的選項。用來判斷「是不是全部都打完了」
    private readonly List<DialogueOptionUI> shownOptions = new List<DialogueOptionUI>();

    [Header("說話的人（氣泡 / bark）")]
    [Tooltip("這一段對話是誰在說。對應 CharacterDatabase 的 id。\n" +
             "留空則完全不出現氣泡 —— 純旁白的節點就留空。\n\n" +
             "下面填了角色池的話，這裡變成**備援** —— 池子挑不到人才用它")]
    public string speakerCharacterId = "";

    [Tooltip("說話者從這個池子裡隨機挑。**填了就蓋過上面手填的 id**。\n\n" +
             "「對話從該區域的角色池隨機一個，符合條件時特定角色也加入」——\n" +
             "條件（侵蝕度／旗標）寫在池子的條目上，這支程式不必知道有哪些人。\n\n" +
             "亂數綁節點：同一個節點重進**還是同一個人**，離開再進來不會換人")]
    public CharacterPool speakerPool;

    /// <summary>
    /// 這一站實際挑中的人。空字串代表沒挑（或沒有池子），此時退回 <see cref="speakerCharacterId"/>。
    ///
    /// ⚠️ 刻意不直接覆寫 `speakerCharacterId` —— 那是 prefab 上的資料，
    /// 執行時改掉的話，下一次載入這個 Stage 會以為手填的就是上次抽到的人。
    /// </summary>
    private string resolvedSpeakerId = "";

    /// <summary>這一段對話說話者的 id。池子挑到誰就是誰，沒有就用手填的。</summary>
    private string SpeakerId =>
        !string.IsNullOrEmpty(resolvedSpeakerId) ? resolvedSpeakerId : speakerCharacterId;

    [Tooltip("立繪上的隱形點擊區，讓玩家點角色聽閒聊。\n\n" +
             "⚠️ 它住在**場景**（立繪是場景物件），而 prefab 不能引用場景物件，\n" +
             "所以這裡留空即可 —— 執行時會自己找 CharacterHitbox.SceneSpeaker")]
    public CharacterHitbox speakerHitbox;

    [Tooltip("判定**成功**時換成哪個表情。要跟角色資料上 Mood Portraits 的名字一致。\n" +
             "留空 = 不換表情。角色沒有那張圖時也會安靜地維持預設立繪")]
    public string moodOnSuccess = "";

    [Tooltip("判定**失敗**時換成哪個表情。留空 = 不換")]
    public string moodOnFailure = "";

    /// <summary>挑台詞用。**不綁 run 種子** —— 綁了同一場 run 每次都聽到同一句。</summary>
    private readonly System.Random lineRng = new System.Random();

    /// <summary>這一段對話的說話者。找不到就是 null（不出現氣泡）。</summary>
    private CharacterData Speaker => GameFlowManager.Character(SpeakerId);

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

        // ⚠️ 挑人要在**立繪固定之前** —— 下面那一段要拿說話者的立繪，
        //    順序反了會固定成上一站那個人的圖
        ResolveSpeaker();

        // ⚠️ 立繪要在**任何一句台詞排隊之前**就固定住。
        //
        // `ShowSpeech` / `ShowSystem` 都是 `portraitRoot.SetActive(圖 != null)` ——
        // 也就是**每一句沒帶立繪的台詞都會主動把立繪關掉**。
        // 對話節點的開場白、選項提示、判定結果全都沒帶圖，
        // 所以晚一步設，角色一開口立繪就不見，掛在立繪上的點擊區也跟著死掉。
        //
        // 這跟 `HoldOpen` 必須早設是同一個時序問題（見 ChoiceStageController.OnStageEnter）。
        CharacterData c = Speaker;
        if (c != null && c.portrait != null) Box?.SetPersistentPortrait(c.portrait);
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

        rewardedOptions.Clear();
        succeededOptions.Clear();
        shownOptions.Clear();

        var texts = new List<string>();
        var attrs = new List<ExploreAttribute>();
        var keywords = new List<string>();

        for (int i = 0; i < options.Count && i < Options.SlotCount; i++)
        {
            Option o = options[i];
            if (o == null || string.IsNullOrEmpty(o.text)) continue;

            texts.Add(o.text);
            attrs.Add(o.attribute);
            keywords.Add(o.keyword);
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
            Options.Show(texts, attrs, DialogueOptionUI.Mode.ProbabilityTarget, keywords);

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
        for (int i = 0; i < shown.Count; i++)
        {
            targets.Add(shown[i]);
            shownOptions.Add(shown[i]);
        }

        Encounter.Begin(hand, targets);

        // 第一句招呼在**打牌開始的這一刻**才出現（企劃指定）——
        // 進場就冒的話會跟開場白的對話框打架，兩個框同時在講話
        BeginSpeaker();
    }

    protected override void Unsubscribe()
    {
        // 解除固定，否則下一個 Stage 會帶著這個角色的立繪進場
        Box?.SetPersistentPortrait(null);

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

        // 選項本身演一次「霧凝聚又散去」：底色壓暗、換成屬性色的「成功／失敗」，再散回內文
        option.PlayResultFlash(success);

        // 角色也用氣泡給一句即時反饋（與商店買東西是同一個形狀）
        CharacterData c = Speaker;
        if (c != null)
        {
            Say(success ? c.PickSuccessLine(lineRng) : c.PickFailureLine(lineRng));
            SetMood(success ? moodOnSuccess : moodOnFailure);
        }

        // 同時反映在對話框正文。用 ShowInstant（即時替換）而不是排隊 ——
        // C18 的設計是連續嘗試，每出一張牌都要點掉一則訊息會被打斷得很嚴重
        string body = success ? data.successText : data.failText;
        if (!string.IsNullOrEmpty(body))
        {
            PopupService.Instance?.ShowInstant(
                Encounter != null ? Encounter.WithAttemptLine(body) : body);
        }

        if (success && run != null && data.grantItemIdsOnSuccess.Count > 0)
        {
            if (rewardedOptions.Add(option.Index))
            {
                for (int i = 0; i < data.grantItemIdsOnSuccess.Count; i++)
                {
                    string id = data.grantItemIdsOnSuccess[i];
                    if (string.IsNullOrEmpty(id)) continue;

                    run.AddItem(id);
                    Debug.Log($"[對話] 選項成功，獲得：{GameFlowManager.ItemName(id)}");
                }
            }
            else
            {
                // 判定本身仍然是真的（會照常顯示成功、照常衰減），只是不再給第二次獎勵
                Debug.Log($"[對話] 選項 {option.Index} 已經給過獎勵了，這次成功不再發");
            }
        }

        // ⚠️ 成功會讓**那一個選項**結案（變暗、不再接受出牌），但**整個環節不結束** ——
        //    蓄意失敗仍然是合法策略（C18⑦），玩家可以繼續打剩下的選項。
        //    結案本身由 DialogueOptionUI 在動效播完後自己做。
        if (success)
        {
            succeededOptions.Add(option.Index);

            // 全部都問完了就沒有目標可打了。這時候還把手牌留在畫面上，
            // 玩家會以為自己漏了什麼 —— 等動效演完就收尾
            if (AllOptionsSpent()) StartCoroutine(EndAfterFlash(option));
        }
    }

    /// <summary>攤出來的選項是不是全部都成功過了。</summary>
    private bool AllOptionsSpent()
    {
        for (int i = 0; i < shownOptions.Count; i++)
        {
            if (shownOptions[i] != null && !succeededOptions.Contains(shownOptions[i].Index)) return false;
        }
        return shownOptions.Count > 0;
    }

    /// <summary>
    /// 等結果動效演完才收尾。
    ///
    /// 直接收的話玩家最後一次成功的畫面會被轉場切掉 ——
    /// 那一下正是他要的回饋，不能吃掉。
    /// </summary>
    private System.Collections.IEnumerator EndAfterFlash(DialogueOptionUI option)
    {
        float wait = option != null ? option.TotalFlashSeconds : 1.3f;
        yield return new WaitForSecondsRealtime(wait + 0.15f);

        if (Encounter != null && Encounter.IsActive) Encounter.EndEncounter(false);
    }

    private void HandleEncounterEnded()
    {
        if (Encounter != null) Encounter.OnEncounterEnded -= HandleEncounterEnded;

        HandUI?.Hide();

        // Q13 暫行做法：環節結束就把剩下的手牌棄掉
        if (explorationDeck != null) explorationDeck.DiscardHand();

        // 通用結語。**在收尾台詞排隊之前說** —— 對話框接著會播 outro，
        // 兩個一起出現時氣泡是角色的話、對話框是旁白，剛好分工
        CharacterData c = Speaker;
        if (c != null) Say(c.PickFarewell(lineRng), true);   // ← 排隊，不要蓋掉最後一句反饋

        BeginOutro();
    }

    // ==========================================
    // 氣泡（bark）
    // ==========================================
    /// <summary>
    /// 打牌開始的那一刻，讓角色講第一句招呼，並把場景上的點擊區指向他。
    ///
    /// 【為什麼不在進場時做】進場正在播開場白（對話框），氣泡同時冒出來的話
    /// 畫面上會有兩個地方在講話，玩家不知道該讀哪一個。
    /// </summary>
    /// <summary>
    /// 決定這一站是誰在說話。沒有池子、或池子挑不到人，就用 prefab 上手填的 id。
    ///
    /// 【亂數為什麼綁節點】商店的「賣什麼」是同一個道理 ——
    /// 玩家離開再進來看到的必須是同一個人，否則等於可以重骰到自己想見的角色。
    ///
    /// 【為什麼要跟事件的種子錯開】兩者都在同一個節點抽，
    /// 直接共用 `runSeed ^ nodeId` 的話兩條抽獎會完全相關
    /// （事件抽中第一順位時，說話者也一定是第一順位）。加一撮鹽把它們分開。
    /// </summary>
    private void ResolveSpeaker()
    {
        resolvedSpeakerId = "";
        if (speakerPool == null) return;

        int seed = run != null ? run.runSeed : 0;

        EldritchMile.Core.RunNodeData node = run != null ? run.CurrentNode : null;
        if (node != null && !string.IsNullOrEmpty(node.nodeId)) seed ^= node.nodeId.GetHashCode();

        seed ^= SpeakerSeedSalt;

        CharacterData picked = speakerPool.Pick(run, new System.Random(seed));

        if (picked == null)
        {
            Debug.LogWarning(
                $"[對話] 角色池「{speakerPool.name}」挑不到人，退回手填的「{speakerCharacterId}」。\n" +
                "池子裡的條目全部條件不成立、或 Weight 都是 0。");
            return;
        }

        resolvedSpeakerId = picked.id;
        Debug.Log($"[對話] 這一站的說話者：{picked.Label}（來自 {speakerPool.name}）");

        // ⚠️ 池子挑到的人**不保證素材齊全** —— 手填 id 的年代是人工確認的，
        //    現在換人是隨機的，缺什麼只會靜靜地不顯示。所以在這裡講出來。
        if (picked.portrait == null)
        {
            Debug.LogWarning(
                $"[對話] 「{picked.Label}」沒有立繪 —— 這一站不會有角色圖，" +
                "掛在立繪上的點擊區也不會出現，氣泡會退回預設錨點。", picked);
        }

        if (picked.successLines.Count == 0 || picked.failureLines.Count == 0)
        {
            Debug.LogWarning(
                $"[對話] 「{picked.Label}」沒有成功／失敗台詞 —— 判定完不會有氣泡反饋。", picked);
        }
    }

    /// <summary>把說話者的抽獎與事件的抽獎錯開。數字本身沒有意義，只要不是 0。</summary>
    private const int SpeakerSeedSalt = 0x5EA1;

    private void BeginSpeaker()
    {
        CharacterData c = Speaker;
        if (c == null) return;

        // prefab 存不下場景物件的引用，所以執行時才解析（見欄位說明）
        if (speakerHitbox == null) speakerHitbox = CharacterHitbox.SceneSpeaker;

        // 同一塊點擊區服務所有對話角色，進來時換人就好
        speakerHitbox?.SetCharacter(SpeakerId);

        Say(c.PickGreeting(lineRng));
    }

    /// <summary>
    /// 讓說話者用氣泡講一句。空字串就不說（沒填那組台詞是合法的）。
    ///
    /// 有點擊區就走它（氣泡會指到立繪頭上）；沒有就退回氣泡自己的預設錨點。
    /// </summary>
    /// <summary>
    /// 換立繪的表情。**沒有那張圖就安靜地維持現況** —— 見 CharacterData.GetPortrait。
    ///
    /// 走 `SetPersistentPortrait` 而不是直接改 Image，是為了同時維持 `HoldPortrait`；
    /// 直接改圖的話下一句沒帶立繪的訊息就會把整個立繪關掉。
    /// </summary>
    private void SetMood(string mood)
    {
        if (string.IsNullOrEmpty(mood)) return;

        CharacterData c = Speaker;
        if (c == null || Box == null) return;

        Box.SetPersistentPortrait(c.GetPortrait(mood));
    }

    private void Say(string line) => Say(line, false);

    /// <param name="waitForCurrent">
    /// true = 等現在那句講完再講。**結語要用這個** ——
    /// 打出最後一張牌時，判定反饋才剛冒出來，環節馬上就結束了；
    /// 不等的話結語會把那句反饋直接刷掉，玩家只看到閃一下。
    /// </param>
    private void Say(string line, bool waitForCurrent)
    {
        if (string.IsNullOrEmpty(line)) return;

        if (speakerHitbox != null) { speakerHitbox.Say(line, waitForCurrent); return; }

        CharacterData c = Speaker;
        SpeechBubbleUI.Instance?.Show(
            line, null, c != null ? c.Label : SpeakerId, waitForCurrent);
    }
}
