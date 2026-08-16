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
                    return;
                }
            }

            inventory.Add(new ItemStack(id, count));
            Debug.Log($"[Run] 獲得道具：{id} ×{count}（共 {count}）");
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
