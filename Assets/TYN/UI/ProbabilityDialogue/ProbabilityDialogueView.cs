using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EldritchMile.UI.ProbabilityDialogue
{
    using EldritchMile.Core;
    using EldritchMile.Core.ProbabilityDialogue;

    /// <summary>
    /// 把 <see cref="ProbabilityDialogueSession"/> 接到畫面上。
    ///
    /// ⚠️ **這一支只接結果，不做任何判斷**（規格 §8：EventView 不可記錄主要 State）。
    /// 機率怎麼算、成不成功，全部在 Session 裡；這裡只負責畫出來與收使用者的輸入。
    ///
    /// 【顏色從哪來】<see cref="AttributeChartData"/> —— **不在這裡另外維護一張對照表**。
    /// 卡框的顏色（本我紅／超我藍／自我綠）是美術畫在圖裡的，
    /// 回答的色點必須跟它一致；兩邊指向同一份資料才不會各自漂移。
    /// </summary>
    public class ProbabilityDialogueView : MonoBehaviour
    {
        [Header("屬性顏色")]
        [Tooltip("屬性 → 顏色／名稱的來源。**跟探索打牌用的是同一份**。\n" +
                 "留空會退回一組寫死的預設色，並發一次警告")]
        public AttributeChartData attributeChart;

        [Header("NPC")]
        public Image backgroundImage;
        public Image npcPortrait;
        public TextMeshProUGUI npcNameText;

        [Tooltip("NPC 的問話。失敗後會換成語氣更強的下一段")]
        public TextMeshProUGUI promptText;

        [Header("回答")]
        [Tooltip("回答按鈕的容器")]
        public RectTransform answerRoot;
        public ProbabilityAnswerUI answerPrefab;

        [Header("卡牌")]
        [Tooltip("手牌區的容器。⚠️ **不可以掛 LayoutGroup** —— 排版是 HandFanLayout 算的，會打架")]
        public RectTransform handRoot;

        [Tooltip("卡面 prefab。**與探索打牌共用同一個**（`EP_cardexplore_template`）。\n\n" +
                 "兩邊畫的本來就是同一組美術（`機率牌XX無框` ＋ `機率牌框紅/藍/綠`），\n" +
                 "各做一份的結果就是尺寸與比例各自漂開。\n\n" +
                 "⚠️ 卡面 prefab **不含互動元件** —— `ProbabilityCardUI` 是生成後才掛上去的。\n" +
                 "不這樣做的話它會跟探索的 `ExploreCardDrag` 搶同一個點擊事件。")]
        public CardViewUIExplore cardPrefab;

        [Header("對話框（沿用舊版那一套）")]
        [Tooltip("勾選 ＝ 問話走**全專案共用的對話框**（`PopupService` → `DialogueBoxUI`）。\n\n" +
                 "這樣分頁、打字機、推進鍵、名字框、立繪全部與探索打牌那一套一模一樣，\n" +
                 "不必在這裡再寫一份 —— 也不會出現「兩個對話框長得不一樣」。\n\n" +
                 "取消勾選 ＝ 用下面那組本地的 Prompt／NpcName／NpcPortrait（離線測試用）")]
        public bool useSharedDialogueBox = true;

        [Tooltip("問話講完之前，回答列與手牌先藏起來。\n\n" +
                 "**「講完話才出現打牌」就是這一格** —— 取消勾選則一開場就全部攤開")]
        public bool hidePlayUntilSpoken = true;

        [Header("手牌排版（與探索打牌同一套）")]
        [Tooltip("卡片間距。**比卡片窄就會重疊**。\n" +
                 "卡面共用之後兩邊卡寬一樣（176），預設 130 約重疊 26%。\n\n" +
                 "跟 `ExploreHandUI.cardSpacing` 是同一個意思 —— 現在連數字都該一樣")]
        [Min(1f)] public float cardSpacing = 130f;

        [Tooltip("整排手牌的最大寬度。超過會自動壓縮間距，不會擠出畫面")]
        [Min(1f)] public float maxHandWidth = 760f;

        [Tooltip("hover 時卡片上浮的距離。\n\n" +
                 "⚠️ 上浮**只動視覺層**，根物件留在原位 ——\n" +
                 "把根物件往上移的話游標會被抽走，卡片下緣會瘋狂閃爍")]
        [Min(0f)] public float hoverLift = 40f;

        [Header("提示")]
        [Tooltip("卡牌沒有影響到任何回答時顯示（規格 R8：要顯示 No Effect）")]
        public GameObject noEffectHint;

        [Min(0f)] public float noEffectSeconds = 1.2f;

        [Header("結算")]
        [Tooltip("結算文字的格式。{0} = 這一場真的發生的效果\n" +
                 "（「獲得 老舊釣竿、【深淵】的侵蝕度 +5%」）。\n\n" +
                 "**跟事件的 `effectLineFormat` 是同一件事** —— 那邊是接在結果內文後面，\n" +
                 "這邊因為問話早就播完了，所以自己占一頁。\n\n" +
                 "留空 = 不顯示結算（不建議：玩家會不知道自己拿到了什麼）")]
        public string outcomeLineFormat = "（{0}）";

        [Header("節奏")]
        [Tooltip("判定結果顯示多久才跑後續。太短玩家看不到自己成功了沒")]
        [Min(0f)] public float resolveDisplaySeconds = 1.0f;

        // ==========================================
        private ProbabilityDialogueSession session;
        private readonly List<ProbabilityAnswerUI> answerUIs = new List<ProbabilityAnswerUI>();
        private readonly List<ProbabilityCardUI> cardUIs = new List<ProbabilityCardUI>();
        private Coroutine noEffectRoutine;

        /// <summary>UI 自己的輸入鎖。Session 也有 State，這裡只是避免動畫播到一半又收到點擊。</summary>
        private bool inputLocked;

        public void Attach(ProbabilityDialogueSession s, CharacterDatabase charDb = null)
        {
            session = s;
            if (session == null) return;

            session.OnStarted += HandleStarted;
            session.OnCardPlayed += HandleCardPlayed;
            session.OnOptionResolved += HandleOptionResolved;
            session.OnOptionDisabled += HandleOptionDisabled;
            session.OnPromptChanged += HandlePromptChanged;
            session.OnHandChanged += RebuildHand;
            session.OnEnded += HandleEnded;

            this.charDb = charDb;
        }

        private CharacterDatabase charDb;

        public void Detach()
        {
            // ⚠️ 共用的東西一定要還回去。不還的話下一站會看到
            //    「立繪還留在畫面上」「對話框關不掉」——
            //    那是 DialogueBoxUI 註解裡講的同一個坑（HoldOpen / HoldPortrait 沒清）。
            if (PopupService.Instance != null) PopupService.Instance.OnAllClosed -= HandleAllSpoken;

            if (Box != null)
            {
                Box.HoldOpen = false;
                Box.SetPersistentPortrait(null);
            }

            speakerHitbox = null;

            if (session == null) return;
            session.OnStarted -= HandleStarted;
            session.OnCardPlayed -= HandleCardPlayed;
            session.OnOptionResolved -= HandleOptionResolved;
            session.OnOptionDisabled -= HandleOptionDisabled;
            session.OnPromptChanged -= HandlePromptChanged;
            session.OnHandChanged -= RebuildHand;
            session.OnEnded -= HandleEnded;
            session = null;
        }

        // ==========================================
        /// <summary>
        /// 屬性的顯示顏色。**單一來源是 `AttributeChartData`** ——
        /// 卡框圖的顏色就是照它畫的，這裡再定義一次就會有兩個真相。
        /// </summary>
        public Color ColorOf(EldritchMile.Core.ExploreAttribute attr)
        {
            if (attributeChart != null) return attributeChart.ColorOf(attr);

            if (!warnedNoChart)
            {
                warnedNoChart = true;   // 每場只吵一次，不然每個色點都印一行
                Debug.LogWarning(
                    "[機率對話] View 沒有指定 Attribute Chart，色點先用預設色。\n" +
                    "⚠️ 這會讓色點跟卡框的顏色對不上 —— 把 AttributeChart 拉進來就好。", this);
            }

            switch (attr)
            {
                case EldritchMile.Core.ExploreAttribute.Id:       return new Color(0.86f, 0.34f, 0.32f);
                case EldritchMile.Core.ExploreAttribute.Superego: return new Color(0.36f, 0.60f, 0.86f);
                case EldritchMile.Core.ExploreAttribute.Ego:      return new Color(0.44f, 0.76f, 0.48f);
                default:                                          return new Color(0.72f, 0.72f, 0.72f);
            }
        }

        private bool warnedNoChart;

        // ==========================================
        private void HandleStarted()
        {
            inputLocked = false;

            if (backgroundImage != null && session.Data.background != null)
            {
                backgroundImage.sprite = session.Data.background;
                backgroundImage.enabled = true;
            }

            CharacterData npc = charDb != null ? charDb.GetById(session.Data.npcId) : null;
            speakerLabel = npc != null ? npc.Label : session.Data.npcId;

            if (npcNameText != null) npcNameText.text = speakerLabel;
            if (npcPortrait != null)
            {
                npcPortrait.sprite = npc != null ? npc.portrait : null;
                npcPortrait.enabled = npcPortrait.sprite != null;
            }

            // ⚠️ 順序照抄 DialogueStageController：**立繪要在任何一句台詞排隊之前固定住**。
            //    `ShowSpeech` / `ShowSystem` 都是 `portraitRoot.SetActive(圖 != null)`，
            //    晚一步設的話，角色一開口立繪就不見，掛在立繪上的點擊區也跟著死掉。
            if (UsingSharedBox)
            {
                Box.SetPersistentPortrait(npc != null ? npc.portrait : null);

                // ⚠️ HoldOpen 也要在排隊之前 —— Advance() 是「文字播完 → 沒 HoldOpen 就 Hide()」，
                //    而 Hide() 會關掉整個對話框，回答列就沒有背景可以貼了
                Box.HoldOpen = true;

                // 立繪上的點擊區：讓玩家點角色能聽到閒聊（跟舊版同一支）
                if (speakerHitbox == null) speakerHitbox = CharacterHitbox.SceneSpeaker;
                speakerHitbox?.SetCharacter(session.Data.npcId);

                PopupService.Instance.OnAllClosed -= HandleAllSpoken;
                PopupService.Instance.OnAllClosed += HandleAllSpoken;

                // 開場問候的氣泡（舊版在 ResolveSpeaker 之後就會叫一次）
                if (npc != null) Say(npc.PickGreeting(lineRng));
            }

            RebuildAnswers();
            RebuildHand();

            // 先藏起來，等問話講完（HandleAllSpoken）才攤開
            SetPlayVisible(!hidePlayUntilSpoken || !UsingSharedBox);

            // ⚠️ **開場白不在這裡講。**
            //
            // `Session.Begin()` 的結尾是
            //     OnStarted → OnHandChanged → OnPromptChanged(0, initialPrompt)
            // 三發連著跑。這裡再講一次的話，開場白會**整段播兩輪**。
            //
            // 開場白與失敗後的問話走的是同一條路（OnPromptChanged），
            // 交給 HandlePromptChanged 一支處理就好。
        }

        // ==========================================
        // 共用對話框
        // ==========================================
        private string speakerLabel = "";
        private CharacterHitbox speakerHitbox;

        private DialogueBoxUI Box =>
            PopupService.Instance != null ? PopupService.Instance.dialogueBox : null;

        /// <summary>真的用得成共用對話框嗎。缺 PopupService 時退回本地那組，不讓玩家卡住。</summary>
        private bool UsingSharedBox
        {
            get
            {
                if (!useSharedDialogueBox) return false;

                if (PopupService.Instance == null || Box == null)
                {
                    if (!warnedNoBox)
                    {
                        warnedNoBox = true;   // 每場只吵一次
                        Debug.LogWarning(
                            "[機率對話] 場上沒有 PopupService／DialogueBoxUI，" +
                            "問話退回這個 Stage 自己的 Prompt 文字（不會分頁）。", this);
                    }
                    return false;
                }
                return true;
            }
        }

        private bool warnedNoBox;

        /// <summary>
        /// 把一段問話拆成頁，逐頁排進共用對話框。
        ///
        /// 【怎麼分頁】**空行 ＝ 換頁**。附件的一段對話本來就是一句一段，
        /// 所以照抄進資產就自然分好了，不必另外維護一個頁面清單。
        ///
        /// 【誰在講話】開頭是「角色名：」的那一頁走 `ShowSpeech`（有名字框），
        /// 其餘走 `ShowText` 的系統提示公版（旁白）——
        /// 這是舊版 `ChoiceStageController.Line.speaker` 留空與否的同一套規則。
        /// </summary>
        private void Speak(string text)
        {
            if (!UsingSharedBox || string.IsNullOrEmpty(text)) return;

            string[] pages = text.Replace("\r\n", "\n").Split(
                new[] { "\n\n" }, System.StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < pages.Length; i++)
            {
                string page = pages[i].Trim();
                if (page.Length == 0) continue;

                string speaker, body;
                SplitSpeaker(page, out speaker, out body);

                if (string.IsNullOrEmpty(speaker)) PopupService.Instance.ShowText(body);
                else PopupService.Instance.ShowSpeech(speaker, body);
            }
        }

        /// <summary>
        /// 「坎貝爾：「好，來表演魔術吧！」」→ speaker = 坎貝爾、body = 「好，來表演魔術吧！」
        ///
        /// 只認**這一段對話的說話者**，不是看到冒號就當成人名 ——
        /// 旁白裡出現冒號（「時藏看向你：…」那種）不該被誤判成換人講話。
        /// </summary>
        private void SplitSpeaker(string page, out string speaker, out string body)
        {
            speaker = "";
            body = page;

            if (string.IsNullOrEmpty(speakerLabel)) return;

            string[] marks = { speakerLabel + "：", speakerLabel + ":" };
            for (int i = 0; i < marks.Length; i++)
            {
                if (!page.StartsWith(marks[i])) continue;

                speaker = speakerLabel;
                body = page.Substring(marks[i].Length).TrimStart();
                return;
            }
        }

        /// <summary>挑台詞用。**不綁 run 種子** —— 綁了同一場 run 每次都聽到同一句。</summary>
        private readonly System.Random lineRng = new System.Random();

        /// <summary>
        /// 讓角色在氣泡裡說一句（bark）。**與舊版 `DialogueStageController.Say` 同一套**：
        /// 有立繪點擊區就掛在它身上（氣泡會跟著立繪），沒有就退回畫面上的預設位置。
        /// </summary>
        /// <param name="waitForCurrent">
        /// true ＝ 等現在那句講完再講。**結語要用這個** ——
        /// 判定反饋才剛冒出來，不等的話結語會把它直接刷掉，玩家只看到閃一下。
        /// </param>
        private void Say(string line, bool waitForCurrent = false)
        {
            if (string.IsNullOrEmpty(line)) return;

            if (speakerHitbox != null) { speakerHitbox.Say(line, waitForCurrent); return; }

            SpeechBubbleUI.Instance?.Show(line, null, speakerLabel, waitForCurrent);
        }

        /// <summary>這一段對話的說話者。找不到就是 null。</summary>
        private CharacterData Speaker =>
            charDb != null && session != null ? charDb.GetById(session.Data.npcId) : null;

        /// <summary>問話講完了 —— 這時候才把回答列與手牌攤開。</summary>
        private void HandleAllSpoken()
        {
            // ⚠️ 判定完（成功或全部失敗）之後那句結語播完也會走到這裡，
            //    那時不可以再把手牌叫回來
            bool canPlay = session != null
                        && session.CurrentState == ProbabilityDialogueSession.State.CardPhase;

            SetPlayVisible(canPlay);
            if (canPlay) inputLocked = false;
        }

        /// <summary>
        /// 回答列與手牌的開關。**「講完話才出現打牌」就是這個。**
        ///
        /// ⚠️ **不可以用 `SetActive(false)`。** 這兩個容器底下都是 LayoutGroup，
        /// 而 **Layout 不會在停用的物件上跑**。關掉之後生成的卡片，
        /// `ProbabilityCardUI.Bind()` 結尾抓到的 `homePosition` 會是 prefab 的原始值 (0,0) ——
        /// 於是整手牌疊在容器原點，拖回去也回到原點。
        ///
        /// 這就是交接文件「坑 1」的同一件事：**要隱形就調 alpha，不要停用。**
        /// 快捷欄的 hover 回不來、外框點不到，成因都在這裡。
        /// </summary>
        private void SetPlayVisible(bool on)
        {
            Veil(answerRoot, on);
            Veil(handRoot, on);
        }

        private static void Veil(RectTransform target, bool on)
        {
            if (target == null) return;

            CanvasGroup g = target.GetComponent<CanvasGroup>();
            if (g == null) g = target.gameObject.AddComponent<CanvasGroup>();

            g.alpha = on ? 1f : 0f;
            g.blocksRaycasts = on;
            g.interactable = on;
        }

        private void RebuildAnswers()
        {
            for (int i = 0; i < answerUIs.Count; i++) if (answerUIs[i] != null) Destroy(answerUIs[i].gameObject);
            answerUIs.Clear();

            if (answerRoot == null || answerPrefab == null) return;

            foreach (ProbabilityDialogueSession.RuntimeOption o in session.Options)
            {
                ProbabilityAnswerUI ui = Instantiate(answerPrefab, answerRoot);
                ui.Bind(o, ColorOf);
                ui.OnClicked += HandleAnswerClicked;
                answerUIs.Add(ui);
            }
        }

        private void RebuildHand()
        {
            for (int i = 0; i < cardUIs.Count; i++) if (cardUIs[i] != null) Destroy(cardUIs[i].gameObject);
            cardUIs.Clear();

            if (handRoot == null || cardPrefab == null || session == null) return;

            foreach (CardDataExplore c in session.Hand)
            {
                CardViewUIExplore face = Instantiate(cardPrefab, handRoot);
                ProbabilityCardUI ui = AttachCardUI(face);

                ui.Bind(c);
                ui.SetVisualRoot(EldritchMile.UI.HandFanLayout.BuildVisualRoot(ui.transform as RectTransform));
                ui.OnHoverChanged += HandleCardHover;
                ui.OnPlayRequested += HandleCardPlayRequested;
                ui.OnAimChanged += HandleCardAim;
                cardUIs.Add(ui);
            }

            LayoutHand();
        }

        // ==========================================
        // 手牌排版　——　與 ExploreHandUI.Layout() 同一套
        // ==========================================

        /// <summary>目前滑鼠在哪一張上面。只影響上浮，不影響疊放順序。</summary>
        private ProbabilityCardUI hoveredCard;

        private void HandleCardHover(ProbabilityCardUI card, bool on)
        {
            hoveredCard = on ? card : (hoveredCard == card ? null : hoveredCard);
            LayoutHand();
        }

        /// <summary>
        /// 把 `ProbabilityCardUI` 掛到共用的卡面上，並接好圖層。
        ///
        /// 【為什麼是執行時掛，不是做進 prefab】
        /// `ProbabilityCardUI` 與探索的 `ExploreCardDrag` **都是輸入處理器**，
        /// Unity 會把點擊送給物件上的每一個處理器 —— 兩個同時在的話會搶事件
        /// （對話那支會把卡標成 spent，探索的牌就再也打不出去）。
        ///
        /// 所以共用的 prefab 只放卡面，互動元件由各自的手牌區掛上去。
        /// 探索那邊本來就是這樣做的（`AddComponent&lt;ExploreCardDrag&gt;`），這裡照抄。
        /// </summary>
        private static ProbabilityCardUI AttachCardUI(CardViewUIExplore face)
        {
            GameObject go = face.gameObject;

            // ⚠️ **共用的卡面 prefab 上本來就掛著 `ExploreCardDrag`**（探索那邊做進去的）。
            //    留著的話它會跟 ProbabilityCardUI 同時收到點擊 ——
            //    Unity 是把事件送給物件上的**每一個**處理器，不是只送第一個。
            //    症狀會是「點一下出兩次牌」或「牌被標成 spent 之後再也打不出去」。
            EldritchMile.Explore.ExploreCardDrag drag = go.GetComponent<EldritchMile.Explore.ExploreCardDrag>();
            if (drag != null)
            {
                drag.enabled = false;   // 先關掉：Destroy 要到影格結尾才生效
                Destroy(drag);
            }

            ProbabilityCardUI ui = go.GetComponent<ProbabilityCardUI>();
            if (ui == null) ui = go.AddComponent<ProbabilityCardUI>();

            // 圖層直接沿用卡面 prefab 上already接好的引用 ——
            // 用名字去 Find 的話，美術改個物件名就會靜靜地壞掉
            ui.artwork = face.artworkImage;
            ui.frame = face.cardFrameImage;

            // 牌面數字**已經畫在美術裡**（機率牌100無框 那張就印著 100），
            // 所以不另外疊一個數字上去 —— 疊了會變成同一張卡上有兩個數字
            ui.valueText = null;
            ui.nameText = null;

            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            ui.canvasGroup = cg;

            return ui;
        }

        /// <summary>
        /// 手牌排版。**規則走共用的 <see cref="EldritchMile.UI.HandFanLayout"/>** ——
        /// 與探索打牌是同一支，兩邊才不會又長得不一樣。
        ///
        /// 這裡只留「哪一張要浮起來」，那是這個環節自己的狀態。
        /// </summary>
        private void LayoutHand()
        {
            int n = cardUIs.Count;
            if (n == 0) return;

            var rects = new List<RectTransform>(n);
            for (int i = 0; i < n; i++)
            {
                rects.Add(cardUIs[i] != null ? cardUIs[i].transform as RectTransform : null);
            }

            EldritchMile.UI.HandFanLayout.Arrange(rects, cardSpacing, maxHandWidth);

            for (int i = 0; i < n; i++)
            {
                if (cardUIs[i] == null) continue;
                cardUIs[i].SetLift(cardUIs[i] == hoveredCard && !cardUIs[i].IsDragging ? hoverLift : 0f);
            }
        }

        // ==========================================
        private void HandleCardPlayRequested(ProbabilityCardUI ui)
        {
            if (inputLocked || session == null) { ui.ReturnHome(); return; }
            if (!session.PlayCard(ui.Data)) ui.ReturnHome();
        }

        /// <summary>拖曳／指到卡片時，把屬性相符且**還可用**的回答亮起來（規格 §3.1）。</summary>
        private void HandleCardAim(ProbabilityCardUI ui, bool aiming)
        {
            if (session == null) return;

            foreach (ProbabilityAnswerUI a in answerUIs)
            {
                if (a == null || a.Bound == null) continue;

                // 用 Session 同一支判定 —— 亮起來的和真的會加機率的必須是同一批，
                // 各寫一次遲早會不一致
                bool match = aiming
                             && a.Bound.available
                             && ProbabilityCardRules.Affects(ui.Data, a.Bound.source.acceptedAttributes);

                // 亮起來的顏色照屬性走 —— 色點、卡框、亮起來的回答是同一個顏色系統
                a.SetHighlighted(match, match ? HighlightColorFor(ui.Data, a.Bound.source) : (Color?)null);
            }
        }

        /// <summary>
        /// 這張牌指到這個回答時，該用什麼顏色亮起來。
        ///
        /// 【有屬性的牌】就用牌自己的顏色 —— 玩家看到的是
        /// 「我這張**紅**牌讓這個回答亮了**紅**」，因果一目了然。
        ///
        /// 【黑白牌（`None`）】它對任何回答都有效（見 `ProbabilityCardRules.Affects`），
        /// 所以「牌的顏色」在這裡沒有意義。退回**回答自己的顏色** ——
        /// 收兩種屬性的回答因此會亮成**兩色的混色**，那正是它跟單色回答的差別。
        ///
        /// ⚠️ 混色是平均 RGB，不是取第一個。取第一個的話，
        /// 「紅＋藍」與「藍＋紅」會亮成不同顏色，而那兩個回答其實是一樣的。
        /// </summary>
        private Color HighlightColorFor(CardDataExplore card, ProbabilityAnswerOption option)
        {
            if (card != null && card.attribute != EldritchMile.Core.ExploreAttribute.None)
            {
                return ColorOf(card.attribute);
            }

            return BlendOf(option != null ? option.acceptedAttributes : null);
        }

        /// <summary>幾種屬性的平均色。空的就退回「無屬性」的顏色。</summary>
        private Color BlendOf(List<EldritchMile.Core.ExploreAttribute> attributes)
        {
            if (attributes == null || attributes.Count == 0)
            {
                return ColorOf(EldritchMile.Core.ExploreAttribute.None);
            }

            float r = 0f, g = 0f, b = 0f;
            for (int i = 0; i < attributes.Count; i++)
            {
                Color c = ColorOf(attributes[i]);
                r += c.r; g += c.g; b += c.b;
            }

            int n = attributes.Count;
            return new Color(r / n, g / n, b / n, 1f);
        }

        private void HandleCardPlayed(
            CardDataExplore card,
            List<ProbabilityDialogueSession.RuntimeOption> targets,
            List<int> before, List<int> after)
        {
            // 亮度收掉
            foreach (ProbabilityAnswerUI a in answerUIs) if (a != null) a.SetHighlighted(false);

            for (int i = 0; i < targets.Count; i++)
            {
                ProbabilityAnswerUI ui = FindUI(targets[i]);
                if (ui != null) ui.AnimateProbability(before[i], after[i]);
            }

            // 規格 R8：沒有影響到任何回答時要講
            if (targets.Count == 0) ShowNoEffect();
        }

        private void ShowNoEffect()
        {
            if (noEffectHint == null) return;
            if (noEffectRoutine != null) StopCoroutine(noEffectRoutine);
            noEffectRoutine = StartCoroutine(NoEffectRoutine());
        }

        private IEnumerator NoEffectRoutine()
        {
            noEffectHint.SetActive(true);
            yield return new WaitForSecondsRealtime(noEffectSeconds);
            noEffectHint.SetActive(false);
            noEffectRoutine = null;
        }

        // ==========================================
        private void HandleAnswerClicked(ProbabilityAnswerUI ui)
        {
            if (inputLocked || session == null || ui.Bound == null) return;

            // 規格 §5.1：選了之後馬上鎖住所有輸入
            inputLocked = true;
            session.SelectOption(ui.Bound);
        }

        private void HandleOptionResolved(ProbabilityDialogueSession.RuntimeOption o, int roll, bool success)
        {
            // ⚠️ **成功時要把 successText 畫出來**（規格 §9.2 第 4 步）。
            //
            // 失敗那條路是 Session 換 CurrentPrompt、透過 OnPromptChanged 通知，
            // 但成功不走那條 —— 所以這裡不寫的話，玩家會看到「按下去沒反應、然後跳走」。
            string success_text = o != null && o.source != null ? o.source.successText : "";

            if (success && !string.IsNullOrEmpty(success_text))
            {
                if (UsingSharedBox)
                {
                    SetPlayVisible(false);
                    Speak(success_text);
                }
                else if (promptText != null)
                {
                    promptText.text = success_text;
                }
            }

            // 判定反饋的氣泡。**與對話框的文字是兩件事** ——
            // 框裡是劇本寫好的 successText，氣泡是角色自己的口頭禪
            CharacterData who = Speaker;
            if (who != null) Say(success ? who.PickSuccessLine(lineRng) : who.PickFailureLine(lineRng));

            StartCoroutine(UnlockAfterDisplay());
        }

        private IEnumerator UnlockAfterDisplay()
        {
            yield return new WaitForSecondsRealtime(resolveDisplaySeconds);
            inputLocked = false;
        }

        private void HandleOptionDisabled(ProbabilityDialogueSession.RuntimeOption o)
        {
            ProbabilityAnswerUI ui = FindUI(o);
            if (ui != null) ui.SetDisabled();
        }

        private void HandlePromptChanged(int failureCount, string prompt)
        {
            if (!UsingSharedBox)
            {
                if (promptText != null) promptText.text = prompt;
                return;
            }

            // 失敗後 NPC 會再說一段。**說話期間一樣先收起手牌** ——
            // 不收的話玩家會在對話跑到一半時繼續出牌，畫面上兩件事在搶注意力
            SetPlayVisible(false);
            inputLocked = true;
            Speak(prompt);
        }

        private void HandleEnded(bool success)
        {
            inputLocked = true;
            SetPlayVisible(false);
            foreach (ProbabilityCardUI c in cardUIs) if (c != null) c.gameObject.SetActive(false);

            // ⚠️ waitForCurrent = true —— 判定反饋才剛冒出來，
            //    不等的話道別會把它直接刷掉，玩家只看到閃一下
            CharacterData who = Speaker;
            if (who != null) Say(who.PickFarewell(lineRng), true);

            ShowOutcome();
        }

        /// <summary>
        /// 「（獲得 老舊釣竿）」—— 對話的結算，**跟事件同一套**。
        ///
        /// 【為什麼一定要有】效果早就套用了（`Session.RunOutcome`），
        /// 但畫面上不講的話玩家只會看到對話結束、然後莫名其妙多了一件東西。
        /// 事件那邊（`EventStageController.HandleChosen`）本來就會接一句，
        /// 對話少了它才是不一致。
        ///
        /// 【為什麼是排隊而不是 ShowInstant】這一刻對話框裡放的是 successText／
        /// finalFailureText 的最後一頁，玩家還在讀。用 Instant 會把那頁直接刷掉。
        /// 排在後面，玩家推進一下就看到 —— 而**離開鍵要等這一頁也讀完才出現**。
        ///
        /// 【為什麼文字不是自己組的】效果提示是 `EventEffect.Apply` 回傳的，
        /// 在這裡再寫一次「獲得 XXX」的話，效果改了兩邊就會不同步。
        /// </summary>
        private void ShowOutcome()
        {
            if (!UsingSharedBox || session == null) return;
            if (string.IsNullOrEmpty(outcomeLineFormat)) return;

            string notes = session.LastOutcomeNotes;
            if (string.IsNullOrEmpty(notes)) return;

            PopupService.Instance.ShowText(string.Format(outcomeLineFormat, notes));
        }

        private ProbabilityAnswerUI FindUI(ProbabilityDialogueSession.RuntimeOption o)
        {
            for (int i = 0; i < answerUIs.Count; i++)
                if (answerUIs[i] != null && answerUIs[i].Bound == o) return answerUIs[i];
            return null;
        }
    }
}
