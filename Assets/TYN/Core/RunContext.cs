using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 一整場 run 的狀態。純資料，不繼承 MonoBehaviour。
    ///
    /// 【生命週期】玩家死亡或通關時整個丟棄，由 GameFlowManager 建立新的。
    /// 需要跨輪迴保留的東西**不放這裡**，放 MetaProgressData。
    ///
    /// 【與 RunStateManager 的分工】(設計文件 §8)
    ///   RunStateManager (Romtyui) ── HP / 能量 / 戰鬥牌組，以它為準
    ///   RunContext      (本類別)  ── 地圖拓撲、探索牌組、鑰匙道具
    /// 兩者不重複儲存同一份資料。
    /// </summary>
    [Serializable]
    public class RunContext
    {
        [Header("地圖")]
        public MapData mapData = new MapData();

        [Header("探索狀態")]
        /// <summary>
        /// C7：鑰匙與道具。「有可能會出現需要先獲得鑰匙才能開啟的狀況」
        ///
        /// **允許同一個 id 出現在多疊裡** —— 這是刻意的，日後 per-instance 狀態
        /// （例如每條魚不同的 +HP／−SAN）就靠這個表達。所以查詢與消耗都要跨疊處理，
        /// 不可以假設「一個 id 只有一疊」。見 <see cref="ItemStack"/>。
        /// </summary>
        public List<ItemStack> inventory = new List<ItemStack>();

        /// 探索牌組。C18 的打牌環節從這裡抽手牌。
        public List<CardDataExplore> exploreDeck = new List<CardDataExplore>();

        [Header("貨幣")]
        /// <summary>
        /// 商店用的錢。
        ///
        /// ⚠️ **「錢」這個設定還沒定案** —— 名稱、來源、初始值都是佔位。
        /// 現在只保證一件事：商店有東西可以扣。等企劃決定了要改的是
        /// 顯示字串與初始值，不是這個欄位的形狀。
        ///
        /// 放在 RunContext 而不是 MetaProgressData，因為它是**單場**資源；
        /// 要跨輪迴保留的錢是另一種東西（遺產），到時候另開欄位。
        /// </summary>
        public int money = 0;

        [Header("旗標")]
        /// <summary>
        /// 這場 run 發生過什麼。**一個字串清單就夠了**，不要為每種狀態各開一個 bool。
        ///
        /// 它同時解決三件本來看起來無關的事：
        ///   · 「未觸發過的事件」 → `event_<id>` 在不在裡面
        ///   · 「第一次踏入漁村」 → `visited_village`
        ///   · 「坎貝爾在不在隊伍」 → 等隊伍系統做好由它來寫
        ///
        /// 【為什麼是 List 不是 HashSet】要能被 Unity 序列化（存檔）。
        /// 數量是幾十個等級，查找成本可以忽略。
        /// </summary>
        public List<string> flags = new List<string>();

        [Header("侵蝕度")]
        /// <summary>
        /// 各尊神的侵蝕度（0–100）。**每尊神各一條，不是單一全域值** ——
        /// 這是企劃定的，寫在這裡免得日後有人把它壓成一個數字。
        ///
        /// 目前只有「深淵」是確定的（`CorruptionTracks.Abyss`），其餘保留擴充空間：
        /// 加一尊神就是多一筆，不用改任何程式。
        /// </summary>
        public List<CorruptionEntry> corruption = new List<CorruptionEntry>();

        [Serializable]
        public class CorruptionEntry
        {
            public string godId = "";
            [Range(0, 100)] public int value;
        }

        [Header("計時")]
        /// <summary>
        /// 這場 run 開跑時的 `Time.unscaledTime`。
        /// 大綱的《門扉》觸發條件是「遊戲進行一段時間後（約 800 秒）」，靠它算。
        /// </summary>
        public float startedAtUnscaled;

        /// <summary>這場 run 玩了幾秒。</summary>
        public float ElapsedSeconds => Mathf.Max(0f, Time.unscaledTime - startedAtUnscaled);

        [Header("轉場暫存")]
        /// 進入節點前寫入，供 Stage 在 OnStageEnter 讀取。
        public RunNodeData pendingNode;

        [Header("除錯")]
        public int runSeed;

        public RunNodeData CurrentNode => mapData.CurrentNode;

        // ==========================================
        // 道具 / 鑰匙
        // ==========================================
        public bool HasItem(string id) => CountOf(id) > 0;

        /// <summary>
        /// 身上有幾個。**跨疊加總** —— 同一個 id 可能分在多疊裡（見欄位說明）。
        /// </summary>
        public int CountOf(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;

            int total = 0;
            for (int i = 0; i < inventory.Count; i++)
            {
                ItemStack s = inventory[i];
                if (s != null && s.id == id) total += s.count;
            }
            return total;
        }

        /// <summary>
        /// 身上有幾個帶這個標籤的東西。大綱的條件很多是這種形狀 ——
        /// 「身上有糧食」「持有 20 張以上的武器牌」「持有的神牌少於 3 張」。
        ///
        /// 要查標籤就得問道具庫，所以沒有道具庫時一律回 0（並不報錯，
        /// 主選單那種還沒有 GameFlowManager 的情境是正常的）。
        /// </summary>
        /// <param name="db">
        /// 要查的道具庫。傳 null 就向 <see cref="GameFlowManager"/> 借。
        /// **編輯器工具與測試一定要自己傳** —— `Instance` 只有在執行時才有值，
        /// 沒進 Play 模式的話每一次查詢都會回 0，而且不會報錯
        /// （跟 <see cref="LootService.Query"/> 是同一個理由）。
        /// </param>
        public int CountByTag(string tag, ItemDatabase db = null)
        {
            if (string.IsNullOrEmpty(tag)) return 0;

            if (db == null && GameFlowManager.Instance != null) db = GameFlowManager.Instance.itemDatabase;
            if (db == null) return 0;

            int total = 0;
            for (int i = 0; i < inventory.Count; i++)
            {
                ItemStack s = inventory[i];
                if (s == null || s.count <= 0) continue;

                ItemData d = db.GetById(s.id);
                if (d != null && d.HasTag(tag)) total += s.count;
            }
            return total;
        }

        public void AddItem(string id, int count = 1)
        {
            if (string.IsNullOrEmpty(id) || count <= 0) return;

            // ⚠️ 目前的合併規則是「同 id 就併進第一疊」。道具還都是可互換的，所以正確。
            //    等 ItemStack 加上 per-instance 狀態（例如每條魚不同的數值）之後，
            //    這裡必須改成「同 id **且狀態相同**才併」，否則兩條不同的魚會被併成一疊。
            for (int i = 0; i < inventory.Count; i++)
            {
                ItemStack s = inventory[i];
                if (s != null && s.id == id)
                {
                    s.count += count;
                    Debug.Log($"[Run] 獲得道具：{id} ×{count}（共 {CountOf(id)}）");
                    NotifyTutorial(id);
                    return;
                }
            }

            inventory.Add(new ItemStack(id, count));
            Debug.Log($"[Run] 獲得道具：{id} ×{count}（共 {count}）");
            NotifyTutorial(id);
        }

        /// <summary>
        /// 拿到東西時，看要不要通知新手教學。
        ///
        /// 【為什麼掛在 AddItem 而不是寶箱】獲得道具的路有好幾條
        /// （寶箱、對話選項、事件效果、商店），**這裡是唯一的匯流點**。
        /// 掛在寶箱上的話，換一條路拿到武器教學就會卡住。
        ///
        /// 大綱那一步寫的是「探索場地、獲得武器」，理論上比這裡窄；
        /// 但教學進行中玩家只有探索那一條路，所以實際上是一樣的，
        /// 而且寬一點不會壞（多發的訊號沒有人在等，就散掉了）。
        /// </summary>
        private static void NotifyTutorial(string id)
        {
            ItemData d = GameFlowManager.Item(id);
            if (d != null && d.HasTag("Weapon")) TutorialSignal.WeaponObtained();
        }

        /// <summary>
        /// 消耗道具。**全有或全無** —— 需要 3 個但只有 2 個時不會扣掉那 2 個。
        ///
        /// 【為什麼強調】這是這類 API 最典型的 bug：先扣再檢查，結果玩家付了錢卻沒買到東西，
        /// 而且因為資源真的少了，重試也修不回來。
        /// </summary>
        public bool ConsumeItem(string id, int count = 1)
        {
            if (count <= 0) return true;
            if (CountOf(id) < count) return false;   // 先確認付得起，才動手扣

            int remaining = count;

            for (int i = inventory.Count - 1; i >= 0 && remaining > 0; i--)
            {
                ItemStack s = inventory[i];
                if (s == null || s.id != id) continue;

                int take = Math.Min(s.count, remaining);
                s.count -= take;
                remaining -= take;

                // 空掉的疊要移除，否則 inventory 會慢慢長滿 count 為 0 的殘骸
                if (s.count <= 0) inventory.RemoveAt(i);
            }

            Debug.Log($"[Run] 消耗道具：{id} ×{count}（剩 {CountOf(id)}）");
            return true;
        }

        /// <summary>
        /// 消耗「**任何**帶這個標籤的東西」。同樣是全有或全無。
        ///
        /// 【為什麼需要這一支】大綱的《好餓好餓的貪吃鬼》寫的是「消耗多少**糧食**」——
        /// 不是某一種糧食。文案不會（也不該）指定要扣哪一條魚，
        /// 那是玩家背包當下有什麼決定的。
        ///
        /// 【扣的順序：從背包尾端往前】沒有「先扣最便宜的」這種聰明邏輯 ——
        /// 那需要價值評估，而價值是會變的（漁村的鹹魚在後段一文不值）。
        /// 真的要挑，應該是讓玩家自己挑，不是程式替他決定。
        /// </summary>
        /// <returns>實際扣掉的東西。扣不起就是空清單（**什麼都不會少**）。</returns>
        public List<ItemStack> ConsumeByTag(string tag, int count, ItemDatabase db = null)
        {
            var taken = new List<ItemStack>();
            if (string.IsNullOrEmpty(tag) || count <= 0) return taken;

            if (db == null && GameFlowManager.Instance != null) db = GameFlowManager.Instance.itemDatabase;
            if (db == null) return taken;

            // 先確認付得起，才動手扣（與 ConsumeItem 同一條規矩）
            if (CountByTag(tag, db) < count) return taken;

            int remaining = count;

            for (int i = inventory.Count - 1; i >= 0 && remaining > 0; i--)
            {
                ItemStack s = inventory[i];
                if (s == null || s.count <= 0) continue;

                ItemData d = db.GetById(s.id);
                if (d == null || !d.HasTag(tag)) continue;

                int take = Math.Min(s.count, remaining);
                s.count -= take;
                remaining -= take;

                taken.Add(new ItemStack(s.id, take));

                if (s.count <= 0) inventory.RemoveAt(i);
            }

            return taken;
        }

        // ==========================================
        // 旗標
        // ==========================================
        public bool HasFlag(string flag) =>
            !string.IsNullOrEmpty(flag) && flags.Contains(flag);

        /// <summary>立起一個旗標。已經有了就不重複加。回傳「這次是不是第一次」。</summary>
        public bool SetFlag(string flag)
        {
            if (string.IsNullOrEmpty(flag) || flags.Contains(flag)) return false;

            flags.Add(flag);
            Debug.Log($"[Run] 旗標：{flag}");
            return true;
        }

        // ==========================================
        // 侵蝕度
        // ==========================================
        public int GetCorruption(string godId)
        {
            if (string.IsNullOrEmpty(godId)) return 0;

            for (int i = 0; i < corruption.Count; i++)
                if (corruption[i] != null && corruption[i].godId == godId) return corruption[i].value;

            return 0;
        }

        /// <summary>
        /// 加減侵蝕度，夾在 0–100。回傳**實際變動量**（被夾住時會小於傳入值）。
        ///
        /// 【為什麼回傳實際變動量】提示文字要說「+5%」，而不是「+5% 但其實只加了 2%」。
        /// 已經 98% 時再 +5，玩家看到的應該是 +2。
        /// </summary>
        public int AddCorruption(string godId, int delta)
        {
            if (string.IsNullOrEmpty(godId) || delta == 0) return 0;

            CorruptionEntry e = null;
            for (int i = 0; i < corruption.Count; i++)
                if (corruption[i] != null && corruption[i].godId == godId) { e = corruption[i]; break; }

            if (e == null)
            {
                e = new CorruptionEntry { godId = godId, value = 0 };
                corruption.Add(e);
            }

            int before = e.value;
            e.value = Mathf.Clamp(before + delta, 0, 100);

            int actual = e.value - before;
            if (actual != 0) Debug.Log($"[Run] 侵蝕度 {godId}：{before} → {e.value}（{actual:+#;-#;0}）");

            return actual;
        }

        // ==========================================
        // 貨幣
        // ==========================================
        public void AddMoney(int amount)
        {
            if (amount <= 0) return;

            money += amount;
            Debug.Log($"[Run] 獲得 {amount}（共 {money}）");
        }

        /// <summary>
        /// 付錢。**付不起就完全不動**，跟 <see cref="ConsumeItem"/> 同一個規矩 ——
        /// 先確認付得起才扣，否則會出現「錢少了但東西沒拿到」而且重試也修不回來。
        /// </summary>
        public bool SpendMoney(int amount)
        {
            if (amount <= 0) return true;
            if (money < amount) return false;

            money -= amount;
            Debug.Log($"[Run] 花掉 {amount}（剩 {money}）");
            return true;
        }

        // ==========================================
        // 【遺產機制的切分點】
        // 這兩支是 RunContext 與 MetaProgressData 之間唯一的橋。
        // 日後要做遺產，改這裡就好，不需要動流程或 Stage。
        // ==========================================

        /// <summary>
        /// 開新的一場 run。meta 決定「這場 run 從遺產繼承什麼」。
        /// 目前只是把遺產道具放進背包當示範 —— 實際規則待企劃設計。
        /// </summary>
        public static RunContext CreateNew(MetaProgressData meta, int seed = 0)
        {
            var run = new RunContext();
            run.runSeed = seed != 0 ? seed : Environment.TickCount;
            run.startedAtUnscaled = Time.unscaledTime;

            if (meta != null)
            {
                // ── 遺產繼承點 ──
                // 待設計：目前先原樣帶入遺產道具。
                for (int i = 0; i < meta.legacyItemIds.Count; i++)
                {
                    run.AddItem(meta.legacyItemIds[i]);
                }

                if (meta.legacyItemIds.Count > 0)
                {
                    Debug.Log($"[Run] 繼承 {meta.legacyItemIds.Count} 項遺產");
                }
            }

            return run;
        }

        /// <summary>
        /// 這場 run 結束了，決定「留下什麼給下個輪迴」。
        /// 由 GameFlowManager 在 StageResult.PlayerDied / RunFinished 時呼叫。
        /// </summary>
        public void ContributeToMeta(MetaProgressData meta, StageResult result)
        {
            if (meta == null) return;

            meta.totalRuns++;

            if (result == StageResult.PlayerDied)
            {
                meta.deaths++;
            }

            RunNodeData node = CurrentNode;
            if (node != null && node.layer > meta.bestLayerReached)
            {
                meta.bestLayerReached = node.layer;
            }

            // ── 遺產產出點 ──
            // 待設計：什麼樣的道具會變成遺產？打到第幾層才有？
            // 目前刻意留空，避免在規則未定前寫死錯誤的行為。

            meta.Save();

            Debug.Log(
                $"[Run] 本場結束（{result}）：總場次 {meta.totalRuns}、" +
                $"死亡 {meta.deaths}、最深層 {meta.bestLayerReached}"
            );
        }
    }
}
