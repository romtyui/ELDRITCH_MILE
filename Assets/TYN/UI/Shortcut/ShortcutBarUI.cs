using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ⚠️ 專案切到 Input System package。舊的 UnityEngine.Input 執行時會丟
// InvalidOperationException，而且編譯期完全看不出來。見 RunDebugPanel 的說明。
using UnityEngine.InputSystem;

namespace EldritchMile.UI.Shortcut
{
    using EldritchMile.Core;

    /// <summary>
    /// 一條快捷欄：平常收合成一個圖示，滑鼠移上去（或點）就**往下一字排開**。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【對應美術稿】
    ///   方案 1／2 —— 收合的圖示展開成一列。這裡做的是方案 2 的變體：
    ///                 由上往下逐格展開，比「一次散開」好做也比較不會閃。
    ///   方案 3　　 —— 滑鼠移上去或點開。兩種都支援，見 openOnHover。
    ///   方案 4／5 —— hover 顯示文字。文字框在這裡，圖案突出在 ShortcutSlotUI。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【點下去會發生什麼】
    ///
    /// · **食物／補給**（`ItemData.IsUsable`）—— 直接使用：回 HP／SAN、消耗一個。
    ///   走 `PlayerVitals`，所以**地圖上、探索中、戰鬥裡都能用**。
    ///
    /// · **收藏品／遺物** —— 點了不會有事，那是刻意的。
    ///   Romtyui 的遺物設計是**戰鬥中被動觸發**（BattleStart／回合開始／出牌時），
    ///   不是點來用的。戰鬥開始時由 `BattleStageController` 送進 `RelicsInventory`。
    ///
    /// ⚠️ 先前這裡寫著「使用道具那個動作不存在」——**那是錯的**。
    /// Romtyui 的 `ItemInventory` / `ItemEffectData` 一直都在，只是需要
    /// `BattleManager`，所以那一套只能在戰鬥裡跑；戰鬥外的簡易效果才走 PlayerVitals。
    /// </summary>
    public class ShortcutBarUI : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("要顯示哪一類")]
        [Tooltip("道具標籤。Food＝食物（針筒）、Curio＝收藏品／遺物（卡冊）。\n" +
                 "留空 = 全部")]
        public string filterTag = "Food";

        [Header("元件")]
        [Tooltip("收合時看得到的那顆圖示。點它或移上去就展開")]
        public GameObject collapsedIcon;

        [Tooltip("展開後那些格子的容器")]
        public RectTransform expandedRoot;

        public ShortcutSlotUI slotPrefab;

        [Header("美術已經排好位置的格子")]
        [Tooltip("不留空的話，就**不生成新格子**，而是把道具填進這些已經存在的格子。\n\n"
                 + "【什麼時候用】美術把位置一格一格排好了的時候（食物那三支針筒）。\n"
                 + "交給 Layout Group 排的話，那份手調的間距就沒了。\n\n"
                 + "道具比格子多的時候，多出來的**不會顯示** —— 這是刻意的，\n"
                 + "格數是美術定的（Romtyui 的 ItemInventory.maxItemCount 也是 3）。")]
        public List<ShortcutSlotUI> fixedSlots = new List<ShortcutSlotUI>();

        [Tooltip("勾選 = 這條欄**一直是展開的**，沒有收合這件事。\n\n"
                 + "食物那一條就是這種 —— 美術分鏡稿的「碰觸前」已經三格全開，\n"
                 + "「有碰觸」只是把格子往外推，不是彈出來。\n\n"
                 + "⚙ 勾了之後 collapsedIcon / 自動收起那三條全部不作用。")]
        public bool alwaysExpanded = false;

        [Header("預設圖")]
        [Tooltip("道具自己沒有 icon 時用這張。食物欄放針管、遺物欄放卡冊。\n\n" +
                 "⚠️ 目前所有道具都還沒有自己的圖 —— 少了這張，整條欄會是一排空框，\n" +
                 "看起來像壞掉。之後某個道具填了自己的 icon 就會自動蓋過這張")]
        public Sprite fallbackIcon;

        [Tooltip("格子外框。留空就用 SC_Slot prefab 上原本的")]
        public Sprite slotFrame;

        [Tooltip("**固定格子專用**：這一格沒有道具時顯示的圖（空針筒）。\n\n"
                 + "留空 = 空格子直接關掉。\n"
                 + "填了 = 格子留著、換成這張 —— 分鏡稿「碰觸前(無食物)」就是這樣，\n"
                 + "玩家看得出「我最多帶三個、現在還有空位」。")]
        public Sprite emptySlotIcon;

        [Header("說明框（hover 時）")]
        public GameObject tooltipRoot;
        public TextMeshProUGUI tooltipTitle;
        public TextMeshProUGUI tooltipBody;

        [Header("行為")]
        [Tooltip("勾選＝滑鼠移上去就展開；取消＝**點一下開、再點一下關**（美術稿方案 3 的兩種）。\n\n" +
                 "預設是點擊 —— hover 展開在這個位置很容易誤觸：\n" +
                 "滑鼠只是要移到畫面右側就會整條彈開")]
        public bool openOnHover = false;

        [Header("自動收起（點擊展開時）")]
        [Tooltip("滑鼠離開整條欄之後，過多久收起來（秒）。0 = 離開就立刻收。\n\n" +
                 "**留一點緩衝是必要的** —— 游標從一格移到另一格的途中可能\n" +
                 "掠過欄位外面幾個影格，0 的話欄位會在你正要點的瞬間關掉")]
        [Min(0f)] public float closeAfterExitSeconds = 0.4f;

        [Tooltip("展開後完全沒有互動就自動收起（秒）。0 = 不自動收。\n\n" +
                 "這是上面那條的保險：用鍵盤開的、或游標根本沒進來過的情況，\n" +
                 "`OnPointerExit` 永遠不會來，只靠它的話欄位會一直開著")]
        [Min(0f)] public float idleCloseSeconds = 6f;

        [Tooltip("點擊快捷欄以外的任何地方就收起來。\n\n" +
                 "⚠️ 這是用**輪詢滑鼠**做的，不是蓋一塊全螢幕的攔截層 ——\n" +
                 "蓋一塊的話底下的東西（商店貨架、對話選項）全都點不到")]
        public bool closeOnClickOutside = true;

        [Tooltip("每一格出現的間隔秒數。0 = 同時出現。\n" +
                 "**逐格出現比一次散開好看**，而且格數多的時候不會一片閃")]
        [Min(0f)] public float staggerSeconds = 0.04f;

        [Tooltip("空的時候要不要顯示收合圖示。\n" +
                 "取消勾選＝身上沒有這類道具時整條藏起來")]
        public bool showWhenEmpty = true;

        [Header("空的時候")]
        public string emptyText = "（沒有）";

        // ==========================================
        /// <summary>
        /// 玩家點了某一格。**接口留著，目前沒有人訂閱** ——
        /// 等「使用道具」的動作做好再接上，UI 不用重做。
        /// </summary>
        public event System.Action<ItemData> OnItemUsed;

        private readonly List<ShortcutSlotUI> slots = new List<ShortcutSlotUI>();
        private bool expanded;

        /// Refresh 正在跑。防止 alwaysExpanded 時 Refresh → SetExpanded → Refresh 繞回來
        private bool refreshing;
        private Coroutine reveal;

        private void OnEnable() => Refresh();

        // ==========================================
        /// <summary>依目前身上的道具重建。</summary>
        /// <param name="collapseAfter">
        /// 重建後要不要收合。展開途中重讀時要傳 false —— 傳 true 會把剛要展開的收掉。
        /// </param>
        public void Refresh(bool collapseAfter = true)
        {
            // alwaysExpanded 時，收尾的 SetExpanded(true) 會再打一次 Refresh ——
            // 這個旗標把那一圈擋掉（見 SetExpanded）
            refreshing = true;
            // ── 先把「這一條欄該顯示哪些道具」算出來 ──
            //    兩種擺法（生成 / 用美術排好的格子）共用同一份結果
            List<ItemStack> shown = new List<ItemStack>();
            ItemDatabase db = GameFlowManager.Instance != null ? GameFlowManager.Instance.itemDatabase : null;
            RunContext run = GameFlowManager.Instance != null ? GameFlowManager.Instance.Run : null;

            if (run != null && db != null)
            {
                foreach (ItemStack stack in run.inventory)
                {
                    if (stack == null || stack.count <= 0) continue;

                    ItemData d = db.GetById(stack.id);

                    // 沒登記的道具（查不到 ItemData）在**有指定標籤**時跳過 ——
                    // 不知道它是不是食物，硬塞進食物欄會誤導。
                    // 但它不會消失：F1 除錯面板會把它標成「沒登記」，那才是抓這種錯的地方
                    if (d == null) continue;
                    if (!string.IsNullOrEmpty(filterTag) && !d.HasTag(filterTag)) continue;

                    shown.Add(stack);
                }
            }

            if (UsingFixedSlots) RefreshFixed(shown, db);
            else RefreshSpawned(shown, db, run);

            if (collapsedIcon != null)
                collapsedIcon.SetActive(showWhenEmpty || slots.Count > 0);

            // 空的時候要看得出「是真的沒有」，而不是「壞了」——
            // 沒有這個提示的話，展開一個空欄跟功能失效長得一模一樣
            if (slots.Count == 0)
                Debug.Log($"[快捷欄] {name}：身上沒有標籤「{filterTag}」的道具，展開會是空的");

            refreshing = false;
            if (collapseAfter) SetExpanded(alwaysExpanded, true);
        }

        /// <summary>
        /// 這條欄用的是美術排好的固定格子嗎。
        /// <see cref="fixedSlots"/> 有東西就是。
        /// </summary>
        public bool UsingFixedSlots
        {
            get { return fixedSlots != null && fixedSlots.Count > 0; }
        }

        /// <summary>
        /// 【美術排好的固定格子】把道具填進去，多出來的格子關掉。
        ///
        /// ⚠️ **這裡絕對不能 Destroy 格子。** 它們是場景裡的物件、位置是美術手調的，
        /// 砍掉就回不來了（而且下一次 Refresh 會變成空欄）。
        /// 生成出來的那條路才需要砍，見 <see cref="RefreshSpawned"/>。
        /// </summary>
        private void RefreshFixed(List<ItemStack> shown, ItemDatabase db)
        {
            DetachHandlers();
            slots.Clear();

            for (int i = 0; i < fixedSlots.Count; i++)
            {
                ShortcutSlotUI s = fixedSlots[i];
                if (s == null) continue;

                if (i >= shown.Count)
                {
                    // 這一格沒東西可放 —— 有空圖就換成空的樣子，沒有才整格關掉
                    if (emptySlotIcon != null) s.BindEmpty(emptySlotIcon);
                    else s.gameObject.SetActive(false);
                    continue;
                }

                ItemStack stack = shown[i];
                s.Bind(db.GetById(stack.id), stack.count, fallbackIcon, slotFrame);
                s.OnHoverChanged += HandleSlotHover;
                s.OnClicked += HandleSlotClicked;
                slots.Add(s);
            }

            NotifyOverflow(shown.Count - fixedSlots.Count);
        }

        /// 上一次有幾件塞不下。用來判斷「是不是又多了一件」
        private int lastOverflow;

        [Header("放不下的時候")]
        [Tooltip("超過格數時跳給玩家看的提示。{0} = 塞不下幾件。\n\n"
                 + "留空 = 只寫到 Console，不打擾玩家")]
        public string overflowMessage = "快捷欄只放得下 {0} 格　多出來的要先用掉一件才拿得到";

        /// <summary>
        /// 身上的東西比格子多。
        ///
        /// ⚠️ **只在「又變多了」的時候才提示。** Refresh 會在每次背包變動、
        /// 每次換環節時跑；每次都喊的話，玩家一路上會被同一句話洗版，
        /// 而那句話在第二次之後就不帶新資訊了。
        ///
        /// 東西變少（用掉了、塞得下了）時把計數歸零 ——
        /// 下次再滿出來要重新提醒一次，那時它又是新資訊了。
        /// </summary>
        private void NotifyOverflow(int overflow)
        {
            if (overflow <= 0) { lastOverflow = 0; return; }

            bool worse = overflow > lastOverflow;
            lastOverflow = overflow;

            Debug.LogWarning(
                $"[快捷欄] {name}：有 {overflow} 件「{filterTag}」放不下（只有 {fixedSlots.Count} 格）。\n" +
                "　東西還在背包裡，只是點不到 —— 先用掉一件就會遞補上來。");

            if (!worse || string.IsNullOrEmpty(overflowMessage)) return;

            PopupService.Instance?.ShowInstant(string.Format(overflowMessage, fixedSlots.Count));
        }

        /// <summary>【生成】舊的做法：依道具數量生格子，交給 Layout Group 排。</summary>
        private void RefreshSpawned(List<ItemStack> shown, ItemDatabase db, RunContext run)
        {
            for (int i = 0; i < slots.Count; i++) if (slots[i] != null) Destroy(slots[i].gameObject);
            slots.Clear();

            if (run == null || db == null || expandedRoot == null || slotPrefab == null) return;

            for (int i = 0; i < shown.Count; i++)
            {
                ShortcutSlotUI s = Instantiate(slotPrefab, expandedRoot);
                s.Bind(db.GetById(shown[i].id), shown[i].count, fallbackIcon, slotFrame);
                s.OnHoverChanged += HandleSlotHover;
                s.OnClicked += HandleSlotClicked;
                slots.Add(s);
            }
        }

        /// <summary>
        /// 解掉事件訂閱。**固定格子專用** ——
        /// 生成出來的格子連物件一起砍，訂閱自然消失；固定格子活得比訂閱久，
        /// 不解的話每次 Refresh 都會多疊一層，點一下會使用好幾次道具。
        /// </summary>
        private void DetachHandlers()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] == null) continue;
                slots[i].OnHoverChanged -= HandleSlotHover;
                slots[i].OnClicked -= HandleSlotClicked;
            }
        }

        // ==========================================
        public void SetExpanded(bool on, bool instant = false)
        {
            // ⚠️ **展開前一定要重讀背包。**
            //
            // 原本只在 OnEnable 建一次格子 —— 那是場景載入的時候，背包還是空的，
            // 所以永遠是 0 個格子，之後撿到東西也不會出現。
            // 症狀是「看得到欄位但點不到」，因為根本沒有東西可以點。
            //
            // 放在展開的時機而不是每幀 —— 只有要看的時候才重建，代價可以忽略
            // 常駐的欄沒有「收起來」這件事 —— 任何要求收合的呼叫都當成展開
            if (alwaysExpanded) on = true;

            if (on && !expanded && !refreshing) Refresh(false);

            expanded = on;

            // 每次開闔都把自動收起的計時歸零 ——
            // 不然上一輪殘留的秒數會在剛展開的瞬間立刻把它關掉
            idleTimer = 0f;
            exitTimer = 0f;

            if (reveal != null) { StopCoroutine(reveal); reveal = null; }

            if (!on)
            {
                for (int i = 0; i < slots.Count; i++) if (slots[i] != null) slots[i].gameObject.SetActive(false);
                HideTooltip();
                return;
            }

            if (instant || staggerSeconds <= 0f)
            {
                for (int i = 0; i < slots.Count; i++) if (slots[i] != null) slots[i].gameObject.SetActive(true);
                return;
            }

            reveal = StartCoroutine(RevealRoutine());
        }

        private IEnumerator RevealRoutine()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] != null) slots[i].gameObject.SetActive(true);
                yield return new WaitForSecondsRealtime(staggerSeconds);
            }
            reveal = null;
        }

        /// <summary>給按鈕綁的（openOnHover 取消勾選時用點的）。</summary>
        public void Toggle() => SetExpanded(!expanded);

        /// <summary>
        /// hover 判定掛在**整條欄**上，不是只掛收合圖示。
        ///
        /// 【為什麼】只掛圖示的話，滑鼠一往下移到展開的格子上就離開了圖示 ——
        /// 欄位會在你正要點的瞬間收起來。掛整條就不會有那個縫。
        /// 所以這個物件的 RectTransform 要**涵蓋收合圖示與展開區**，
        /// 而且要有一個 raycastTarget 的圖（可以是全透明）才收得到事件。
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
            idleTimer = 0f;
            exitTimer = 0f;

            if (openOnHover) SetExpanded(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            exitTimer = 0f;

            if (openOnHover) SetExpanded(false);
        }

        /// 滑鼠現在在不在整條欄上面
        private bool pointerInside;

        /// 展開後沒有任何互動累積了多久
        private float idleTimer;

        /// 滑鼠離開之後累積了多久
        private float exitTimer;

        /// <summary>
        /// 自動收起的三條路。**只在「點擊展開」模式下管**
        /// （hover 模式本來就靠 `OnPointerExit` 收）。
        ///
        /// 【為什麼「點擊外面」是用輪詢的】
        /// 常見做法是在底下鋪一塊全螢幕的透明攔截層，但那會把商店貨架、
        /// 對話選項全部擋掉 —— 玩家會覺得「畫面卡住了」。
        /// 輪詢只讀滑鼠狀態，不擋任何東西。
        ///
        /// ⚠️ 一定要走 Input System 的 `Mouse.current`，而且要判 null ——
        /// 舊的 `UnityEngine.Input` 編得過但執行時會洗版例外（見 RunDebugPanel）。
        /// 沒有滑鼠時 `Mouse.current` 是 null，那不是錯誤。
        /// </summary>
        /// 上一次看到的背包長相。變了就重建
        private int inventorySignature = int.MinValue;

        /// 距離上次檢查過了多久
        private float pollTimer;

        [Tooltip("多久檢查一次背包有沒有變（秒）。0 = 每一幀都查")]
        [Min(0f)] public float pollSeconds = 0.2f;

        /// <summary>
        /// 背包變了就重建。
        ///
        /// 【為什麼是輪詢而不是訂事件】`RunContext` 沒有「背包變動」的事件，
        /// 而會動背包的路有好幾條（事件的 GrantItem、寶箱的 LootService、
        /// 商店、快捷欄自己用掉一個…）。要改成事件就得每一條都補通知，
        /// 漏一條就是「撿到東西但欄位沒更新」——那正是這個 bug。
        ///
        /// 【為什麼原本沒事】舊的欄是**點開才展開**的，`SetExpanded(true)` 會先
        /// `Refresh(false)`，等於每次要看的時候都重讀。食物欄改成常駐之後
        /// 那個時機沒有了，於是只在 `OnEnable` 建一次 —— 之後撿什麼都不會變。
        ///
        /// 代價是每 0.2 秒掃一次背包（通常個位數個疊），可以忽略。
        /// </summary>
        private void PollInventory()
        {
            if (pollSeconds > 0f)
            {
                pollTimer += Time.unscaledDeltaTime;
                if (pollTimer < pollSeconds) return;
                pollTimer = 0f;
            }

            RunContext run = GameFlowManager.Instance != null ? GameFlowManager.Instance.Run : null;

            int sig = 17;
            if (run != null)
            {
                for (int i = 0; i < run.inventory.Count; i++)
                {
                    ItemStack st = run.inventory[i];
                    if (st == null) continue;
                    sig = sig * 31 + (st.id != null ? st.id.GetHashCode() : 0);
                    sig = sig * 31 + st.count;
                }
            }

            if (sig == inventorySignature) return;

            bool first = inventorySignature == int.MinValue;
            inventorySignature = sig;

            // 第一次不用喊 —— 那是正常的初始化，不是「背包變了」
            if (!first)
                Debug.Log($"[快捷欄] {name}：背包變了，重建格子");

            Refresh(!expanded && !alwaysExpanded);
        }

        private void Update()
        {
            PollInventory();

            if (!expanded || openOnHover || alwaysExpanded) return;

            // ── 1. 滑鼠離開整條欄超過緩衝時間 ──
            if (!pointerInside)
            {
                exitTimer += Time.unscaledDeltaTime;
                if (exitTimer >= closeAfterExitSeconds) { SetExpanded(false); return; }
            }

            // ── 2. 一陣子完全沒有互動 ──
            //    滑鼠在上面就算互動中，計時歸零
            if (idleCloseSeconds > 0f)
            {
                if (pointerInside) idleTimer = 0f;
                else idleTimer += Time.unscaledDeltaTime;

                if (idleTimer >= idleCloseSeconds) { SetExpanded(false); return; }
            }

            // ── 3. 點在快捷欄以外的地方 ──
            if (!closeOnClickOutside) return;

            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

            // 點在欄上的話 pointerInside 會是 true（enter 早一步到），不收
            if (!pointerInside) SetExpanded(false);
        }

        /// <summary>
        /// 點一下開、再點一下關。
        ///
        /// ⚠️ 點在**某一格**上時不會走到這裡 —— `ShortcutSlotUI` 自己也實作了
        /// `IPointerClickHandler`，Unity 只會送給往上找到的**第一個**處理者。
        /// 所以「點格子＝使用道具」與「點空白＝開關」不會互相干擾。
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (openOnHover) return;    // hover 模式下點擊不該再切換，會打架
            if (alwaysExpanded) return; // 常駐的欄沒有開關可切

            // **關著才開。** 開著時點空白處不收 ——
            // 收起來交給那三條（離開、閒置、點外面）。
            // 不然玩家想點某一格卻點偏了一點，整條就關掉了
            idleTimer = 0f;
            if (!expanded) SetExpanded(true);
        }

        // ==========================================
        private void HandleSlotHover(ShortcutSlotUI s)
        {
            if (!s.IsHovered) { HideTooltip(); return; }

            if (tooltipRoot == null) return;
            tooltipRoot.SetActive(true);

            if (tooltipTitle != null)
                tooltipTitle.text = s.Item != null ? s.Item.Label : "（沒登記的道具）";

            if (tooltipBody != null)
            {
                tooltipBody.text = s.Item != null && !string.IsNullOrEmpty(s.Item.description)
                    ? s.Item.description
                    : emptyText;
            }
        }

        private void HideTooltip()
        {
            if (tooltipRoot != null) tooltipRoot.SetActive(false);
        }

        /// <summary>
        /// 點一格 = 使用那件道具。
        ///
        /// 【為什麼走 PlayerVitals 而不是 Romtyui 的 ItemEffectData】
        /// 那一套的 `ItemUseContext` 需要 `BattleManager` 與 `BattleUnit` ——
        /// **只能在戰鬥裡跑**。但食物在地圖上、探索中也要能吃，
        /// 所以簡易效果（回 HP／SAN）走 `PlayerVitals`，戰鬥內外都有效。
        ///
        /// 兩者不衝突：日後要做「只能在戰鬥裡用」的複雜道具，
        /// 就在這裡多一條路由到 `ItemEffectData`，簡易效果照舊。
        /// </summary>
        private void HandleSlotClicked(ShortcutSlotUI s)
        {
            if (s.Item == null) return;

            ItemData d = s.Item;

            if (!d.IsUsable)
            {
                // 收藏品／遺物就是這一類：它們有效果，但**不是點來用的** ——
                // 是戰鬥開始時自動生效。講清楚，不然玩家會一直點
                Debug.Log($"[快捷欄]「{d.Label}」不是消耗品" +
                          (d.relicEffect != null ? " —— 它的效果在戰鬥中自動生效" : ""));
                return;
            }

            RunContext run = GameFlowManager.Instance != null ? GameFlowManager.Instance.Run : null;
            if (run == null) return;

            // ── 有代價的食物：付不起就**整個不發生** ──
            //
            // 【為什麼要在消耗之前檢查】`PlayerVitals.SpendHp` 付不起時會安靜地回 false，
            // 但那時道具已經被 ConsumeItem 吃掉了 —— 玩家會看到「奢侈的血塊消失了、
            // 什麼都沒發生」。順序反過來就沒有這個洞。
            //
            // 「付不起」的定義跟 PlayerVitals 一致：**扣到 0 或以下**。
            // 吃東西不該把自己吃死。
            if (!CanAffordCost(d))
            {
                Debug.Log($"[快捷欄]「{d.Label}」現在用不起 —— " +
                          $"代價 HP -{d.hpCost}／SAN -{d.sanCost}，" +
                          $"目前 HP {PlayerVitals.Hp}、SAN {PlayerVitals.San}");
                PopupService.Instance?.ShowInstant($"{d.Label}　現在承受不起");
                return;
            }

            // ⚠️ 先確認真的還有這件東西再套效果。
            //    反過來做的話，快速連點會在庫存只剩 1 個時回血兩次
            if (d.consumeOnUse && !run.ConsumeItem(d.id, 1))
            {
                Debug.LogWarning($"[快捷欄]「{d.Label}」用不掉 —— 背包裡已經沒有了");
                Refresh(false);
                return;
            }

            // ⚠️ 記下**吃之前**的數值 —— 播報要講「實際發生了什麼」，
            //    不是照抄道具上的帳面數字。滿血時吃蛋糕，帳面是 +35、實際是 +0；
            //    寫帳面的話玩家會以為系統壞了（「明明說 +35 怎麼沒變」）。
            int hp0 = PlayerVitals.Hp, san0 = PlayerVitals.San;

            // 先給再扣。反過來的話「回大量 HP、扣中等 SAN」那種食物
            // 會在 HP 很低時被自己的代價擋掉，而它本來就是要救命的
            if (d.hpRestore > 0) PlayerVitals.HealHp(d.hpRestore);
            if (d.sanRestore > 0) PlayerVitals.RestoreSan(d.sanRestore);
            if (d.hpCost > 0) PlayerVitals.SpendHp(d.hpCost);
            if (d.sanCost > 0) PlayerVitals.SpendSan(d.sanCost);

            int dHp = PlayerVitals.Hp - hp0, dSan = PlayerVitals.San - san0;

            if (dHp == 0 && dSan == 0)
            {
                // 道具沒白吃、數值卻沒動 —— 這是玩家最容易誤判成 bug 的情況，
                // 所以講清楚是「滿了」還是「系統還沒初始化」
                Debug.LogWarning(
                    $"[快捷欄]「{d.Label}」吃下去了，但 HP／SAN 完全沒有變動。\n" +
                    (PlayerVitals.IsReady
                        ? $"　目前 HP {PlayerVitals.Hp}/{PlayerVitals.MaxHp}、SAN {PlayerVitals.San}/{PlayerVitals.MaxSan}"
                          + " —— 應該是已經滿了，或這件道具的回復值本來就是 0。"
                        : "　⚠️ 這場 run 的 HP／SAN **還沒初始化**（見 PlayerVitals 的警告）。"));
            }

            Debug.Log($"[快捷欄] 使用「{d.Label}」"
                      + (d.hpRestore > 0 ? $"　HP +{d.hpRestore}" : "")
                      + (d.sanRestore > 0 ? $"　SAN +{d.sanRestore}" : "")
                      + (d.hpCost > 0 ? $"　HP -{d.hpCost}" : "")
                      + (d.sanCost > 0 ? $"　SAN -{d.sanCost}" : "")
                      + $"　→ HP {PlayerVitals.Hp}/{PlayerVitals.MaxHp}"
                      + $"　SAN {PlayerVitals.San}/{PlayerVitals.MaxSan}");

            PopupService.Instance?.ShowInstant(UsedTextFor(d, dHp, dSan));

            // 用完就重建 —— 數量要跟著變，用光了那一格要消失
            Refresh(false);

            OnItemUsed?.Invoke(d);
        }

        /// <summary>
        /// 代價付得起嗎。**扣到 0 或以下就算付不起** —— 與
        /// <see cref="PlayerVitals.SpendHp"/> 用同一條規矩，兩邊不能各判各的。
        /// </summary>
        private static bool CanAffordCost(ItemData d)
        {
            if (d.hpCost <= 0 && d.sanCost <= 0) return true;

            // 還沒進過戰鬥時 HP／SAN 尚未初始化。那時扣不動，也不該讓玩家白白用掉
            if (!PlayerVitals.IsReady) return false;

            if (d.hpCost > 0 && PlayerVitals.Hp - d.hpCost <= 0) return false;
            if (d.sanCost > 0 && PlayerVitals.San - d.sanCost <= 0) return false;

            return true;
        }

        /// <summary>
        /// 「吃了什麼、實際發生了什麼」。
        ///
        /// 【為什麼講實際值而不是道具上的數字】滿血時吃蛋糕，
        /// 帳面是 +35、實際是 +0 —— 照抄帳面的話玩家會以為系統壞了。
        /// 講「HP 100/100（已滿）」他就知道是自己已經滿了。
        /// </summary>
        private static string UsedTextFor(ItemData d, int dHp, int dSan)
        {
            var bits = new List<string>();
            if (dHp != 0) bits.Add($"HP {dHp:+#;-#;0}");
            if (dSan != 0) bits.Add($"SAN {dSan:+#;-#;0}");

            if (bits.Count == 0)
            {
                return PlayerVitals.IsReady
                    ? $"{d.Label}　（HP {PlayerVitals.Hp}/{PlayerVitals.MaxHp}、"
                      + $"SAN {PlayerVitals.San}/{PlayerVitals.MaxSan}，沒有變化）"
                    : $"{d.Label}　（沒有變化）";
            }

            return $"{d.Label}　{string.Join("　", bits.ToArray())}"
                 + $"　→　HP {PlayerVitals.Hp}/{PlayerVitals.MaxHp}　SAN {PlayerVitals.San}/{PlayerVitals.MaxSan}";
        }
    }
}
