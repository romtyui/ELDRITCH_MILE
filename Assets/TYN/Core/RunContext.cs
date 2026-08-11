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
        /// C7：鑰匙與道具。「有可能會出現需要先獲得鑰匙才能開啟的狀況」
        public List<string> inventory = new List<string>();

        /// 探索牌組。C18 的打牌環節從這裡抽手牌。
        public List<CardDataExplore> exploreDeck = new List<CardDataExplore>();

        [Header("轉場暫存")]
        /// 進入節點前寫入，供 Stage 在 OnStageEnter 讀取。
        public RunNodeData pendingNode;

        [Header("除錯")]
        public int runSeed;

        public RunNodeData CurrentNode => mapData.CurrentNode;

        // ==========================================
        // 道具 / 鑰匙
        // ==========================================
        public bool HasItem(string id)
        {
            return !string.IsNullOrEmpty(id) && inventory.Contains(id);
        }

        public void AddItem(string id)
        {
            if (string.IsNullOrEmpty(id)) return;

            inventory.Add(id);
            Debug.Log($"[Run] 獲得道具：{id}");
        }

        public bool ConsumeItem(string id)
        {
            if (!HasItem(id)) return false;

            inventory.Remove(id);
            Debug.Log($"[Run] 消耗道具：{id}");
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
                    run.inventory.Add(meta.legacyItemIds[i]);
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
