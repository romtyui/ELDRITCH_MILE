using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
// ⚠️ 這個專案的 Player Settings 已經切到 **Input System package**。
// 舊的 `UnityEngine.Input.GetKeyDown()` 在執行時會直接丟 InvalidOperationException ——
// 而且**編譯期完全看不出來**，舊 API 仍然存在、仍然編得過。
// 寫法對齊隊友的 `BattleDebugHotkeys`，全專案一致。
using UnityEngine.InputSystem;
#endif

namespace EldritchMile.Core
{
    /// <summary>
    /// 開發用的持有狀況面板。**這不是背包 UI，是除錯工具。**
    ///
    /// ────────────────────────────────────────────────────────
    /// 【為什麼要做，明明背包 UI 被否決了】
    ///
    /// 被否決的是「玩家要開一個大背包翻東西」，那是介面決策。
    /// 但**開發時看不見自己身上有什麼**是另一回事 ——
    /// 寶箱給了沒、商店扣了沒、事件的效果套用了沒，現在只能翻 Console。
    ///
    /// 快捷欄（食物／收藏／卡片分開）要接的是**同一份資料**，
    /// 所以這支不會白做：它先證明「依標籤過濾」這條路走得通。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【為什麼用 IMGUI 而不是 uGUI】
    ///
    /// 除錯工具的成本必須趨近於零：掛上去、按一個鍵、就看得到。
    /// 做成 uGUI 要拉 Canvas、排版、對位、做 prefab —— 那些工都花在
    /// 一個玩家永遠不會看到的東西上。而且 uGUI 版本會有「不小心留在正式版」的風險。
    ///
    /// 這支只在**編輯器與開發版**存在（見下方的 #if），正式包裡整個類別會消失。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【⚠️ 卡片不在背包裡】
    ///
    /// 武器牌買下去是進 `RunStateManager.savedDeck`（戰鬥端持有），
    /// 跟 `RunContext.inventory` 是**兩份不同的東西**。
    /// 所以面板把它們分開列 —— 做快捷欄的人第一天就該知道這件事，
    /// 不然介面做到一半才發現「卡片」那一格要查的是另一個來源。
    /// </summary>
    public class RunDebugPanel : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

        [Header("開關")]
        [Tooltip("按這個鍵開合。預設 F1。\n" +
                 "⚠️ 這是 Input System 的 Key，不是舊的 KeyCode")]
        public Key toggleKey = Key.F1;

        [Tooltip("一進遊戲就打開")]
        public bool openOnStart = false;

        [Header("外觀")]
        [Tooltip("面板寬度（像素）")]
        [Min(200)] public int width = 420;

        [Tooltip("字級。4K 螢幕上調大一點")]
        [Min(8)] public int fontSize = 13;

        /// <summary>背包的分類頁。**這幾格就是日後快捷欄要分的那幾類**。</summary>
        private static readonly string[] TabNames = { "全部", "糧食", "收藏品", "補給", "其他" };
        private static readonly string[] TabTags = { "", "Food", "Curio", "Supply", "" };

        private bool open;
        private int tab;
        private Vector2 scroll;
        private GUIStyle labelStyle;
        private GUIStyle headerStyle;

        private void Start() { open = openOnStart; }

        private void Update()
        {
            // 沒有鍵盤（手把、觸控裝置）時 Keyboard.current 是 null，不是錯誤
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            // 用 var —— KeyControl 住在 UnityEngine.InputSystem.Controls，
            // 寫出型別名就要多一個 using。隊友的 BattleDebugHotkeys 也是這樣避開的
            var key = kb[toggleKey];
            if (key != null && key.wasPressedThisFrame) open = !open;
        }

        private void OnGUI()
        {
            if (!open) return;

            EnsureStyles();

            float h = Mathf.Min(Screen.height - 40, 760);
            GUILayout.BeginArea(new Rect(10, 10, width, h), GUI.skin.box);

            RunContext run = GameFlowManager.Instance != null ? GameFlowManager.Instance.Run : null;

            GUILayout.Label($"持有狀況　[{toggleKey}] 關閉", headerStyle);

            if (run == null)
            {
                GUILayout.Label("現在沒有進行中的 run（主選單）", labelStyle);
                GUILayout.EndArea();
                return;
            }

            DrawVitals(run);

            DrawStageJump();

            GUILayout.Space(6);
            tab = GUILayout.Toolbar(tab, TabNames);

            scroll = GUILayout.BeginScrollView(scroll);

            DrawInventory(run);
            DrawDecks(run);
            DrawCorruption(run);
            DrawFlags(run);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        /// <summary>
        /// 直接跳到某個 Stage。
        ///
        /// 【為什麼需要】新做好的 Stage 常常還沒有「地圖上的入口」——
        /// 那時候要驗畫面就得先想辦法走到它，很浪費時間。
        /// 這幾顆按鈕讓「看一眼新畫面」變成一秒的事。
        ///
        /// ⚠️ 這是**跳過流程**的捷徑，不是正常入口。跳過去的 Stage 結束之後
        /// 會照常回報完成、地圖下拉，所以不會把流程弄壞。
        /// </summary>
        private void DrawStageJump()
        {
            if (GameFlowManager.Instance == null) return;

            GUILayout.Space(4);
            GUILayout.Label("直接跳到（除錯用，跳過流程）", labelStyle);

            GUILayout.BeginHorizontal();
            // ⚠️ 這裡要列**全部**能跳的 Stage。少列一個，那個 Stage 就等於沒有入口 ——
            //    新做好的東西最常卡在「我要怎麼走到它」
            int perRow = 0;
            foreach (StageType t in new[]
            {
                StageType.ProbabilityDialogue,
                StageType.SpecialEvent,
                StageType.Event,
                StageType.Dialogue,
                StageType.Explore,
                StageType.Shop,
                StageType.Battle,
                StageType.Menu,
            })
            {
                if (perRow >= 4)   // 一排四顆，不然按鈕會被擠到看不見字
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    perRow = 0;
                }

                if (GUILayout.Button(t.ToString()))
                {
                    Debug.Log($"[除錯] 直接跳到 {t}");
                    GameFlowManager.Instance.DebugJumpToStage(t);
                }
                perRow++;
            }
            GUILayout.EndHorizontal();
        }

        // ==========================================
        private void DrawVitals(RunContext run)
        {
            // ⚠️ HP／SAN 不在 RunContext 裡 —— 它們歸戰鬥端持有，我方只能透過轉接頭讀。
            //    沒初始化的時候讀出來是 0，那不是 bug，是「這場 run 還沒設定起始值」
            string hp = PlayerVitals.IsReady
                ? $"HP {PlayerVitals.Hp}/{PlayerVitals.MaxHp}　SAN {PlayerVitals.San}/{PlayerVitals.MaxSan}"
                : "HP／SAN **尚未初始化**（GameFlowManager 的 Starting Max Hp 是 0）";

            GUILayout.Label(hp, labelStyle);
            GUILayout.Label($"金幣 {run.money}　已進行 {run.ElapsedSeconds:F0} 秒", labelStyle);
        }

        private void DrawInventory(RunContext run)
        {
            ItemDatabase db = GameFlowManager.Instance != null ? GameFlowManager.Instance.itemDatabase : null;

            GUILayout.Label($"── 背包（{run.inventory.Count} 疊）──", headerStyle);

            int shown = 0;

            for (int i = 0; i < run.inventory.Count; i++)
            {
                ItemStack s = run.inventory[i];
                if (s == null || s.count <= 0) continue;

                ItemData d = db != null ? db.GetById(s.id) : null;
                if (!PassesTab(d)) continue;

                string tags = d != null && d.tags.Count > 0
                    ? "  [" + string.Join(", ", d.tags.ToArray()) + "]"
                    : "";

                // 查不到資料的道具要看得出來 —— 那通常代表 id 打錯或忘了登記
                string label = d != null ? d.Label : $"{s.id}（**沒登記**）";

                GUILayout.Label($"　{label} ×{s.count}{tags}", labelStyle);
                shown++;
            }

            if (shown == 0) GUILayout.Label("　（這一類沒有東西）", labelStyle);
        }

        /// <summary>這一格要不要顯示。「其他」＝不屬於前面任何一類的。</summary>
        private bool PassesTab(ItemData d)
        {
            if (tab == 0) return true;
            if (d == null) return tab == 4;

            if (tab == 4)
            {
                return !d.HasTag("Food") && !d.HasTag("Curio") && !d.HasTag("Supply");
            }

            return d.HasTag(TabTags[tab]);
        }

        private void DrawDecks(RunContext run)
        {
            GUILayout.Space(6);

            // ⚠️ 這一段是分開的，不是背包的一部分 —— 見類別說明
            GUILayout.Label($"── 戰鬥牌組（{PlayerVitals.DeckCount} 張・不在背包裡）──", headerStyle);

            RunStateManager rs = RunStateManager.Instance;
            if (rs == null)
            {
                GUILayout.Label("　場上沒有 RunStateManager", labelStyle);
            }
            else
            {
                var counts = new Dictionary<string, int>();
                for (int i = 0; i < rs.savedDeck.Count; i++)
                {
                    CardData c = rs.savedDeck[i];
                    if (c == null) continue;

                    string n = string.IsNullOrEmpty(c.cardName) ? c.name : c.cardName;
                    counts[n] = counts.TryGetValue(n, out int v) ? v + 1 : 1;
                }

                if (counts.Count == 0) GUILayout.Label("　（空的）", labelStyle);
                foreach (var kv in counts) GUILayout.Label($"　{kv.Key} ×{kv.Value}", labelStyle);
            }

            GUILayout.Label($"── 探索牌組（{run.exploreDeck.Count} 張）──", headerStyle);
        }

        private void DrawCorruption(RunContext run)
        {
            GUILayout.Space(6);
            GUILayout.Label("── 侵蝕度 ──", headerStyle);

            if (run.corruption.Count == 0)
            {
                GUILayout.Label("　（都是 0）", labelStyle);
                return;
            }

            for (int i = 0; i < run.corruption.Count; i++)
            {
                RunContext.CorruptionEntry e = run.corruption[i];
                if (e == null) continue;

                GUILayout.Label($"　{e.godId}　{e.value}%", labelStyle);
            }
        }

        private void DrawFlags(RunContext run)
        {
            GUILayout.Space(6);
            GUILayout.Label($"── 旗標（{run.flags.Count}）──", headerStyle);

            if (run.flags.Count == 0)
            {
                GUILayout.Label("　（還沒有）", labelStyle);
                return;
            }

            for (int i = 0; i < run.flags.Count; i++)
            {
                GUILayout.Label($"　{run.flags[i]}", labelStyle);
            }
        }

        private void EnsureStyles()
        {
            if (labelStyle != null && labelStyle.fontSize == fontSize) return;

            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = fontSize, richText = false };
            headerStyle = new GUIStyle(GUI.skin.label) { fontSize = fontSize, fontStyle = FontStyle.Bold };
        }

#endif
    }
}
