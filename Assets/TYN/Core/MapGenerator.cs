using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 地圖生成。**純資料，不碰任何 UI 或 Transform。**
    ///
    /// 【與舊版的兩個關鍵差別】
    ///
    /// 1. 連線計算改用 `xPercent` 而非 `transform.localPosition.x`。
    ///    舊版在 SpawnNode 設好 anchor 後立刻讀 localPosition 來排序找最近節點，
    ///    依賴 Unity 何時重算 RectTransform —— 是個會隨版本或執行順序改變行為的隱患。
    ///    改用百分比後結果完全等價，而且與畫面無關。
    ///
    /// 2. 用 System.Random + seed，不用 UnityEngine.Random（全域狀態）。
    ///    同一個 seed 必定產生同一張地圖，除錯與重現問題容易得多。
    /// </summary>
    public static class MapGenerator
    {
        public static MapData Generate(MapGenerationSettings settings, int seed)
        {
            if (settings == null)
            {
                Debug.LogWarning("[地圖生成] 沒有指定 MapGenerationSettings，產生一張最小地圖");
                return CreateFallback();
            }

            var rng = new System.Random(seed);

            return settings.useDemoRoute
                ? GenerateDemoRoute(settings, rng)
                : GenerateProcedural(settings, rng);
        }

        // ==========================================
        // 隨機生成
        // ==========================================
        /// <summary>
        /// 固定網格 ＋ 隨機遊走路徑。**不規則感來自「哪些格子活下來」，
        /// 不是來自把位置隨機推歪。**
        ///
        /// ────────────────────────────────────────────────────────
        /// 【為什麼換掉舊做法】
        ///
        /// 舊版是「每層決定放 N 個 → 在寬度上平均擺開 → 加一點抖動」。
        /// 那個結構**生不出不規則** —— 每一層都是等距的一排，加再多 jitter
        /// 也只是同一排在晃，看起來就是一條長條。
        ///
        /// ────────────────────────────────────────────────────────
        /// 【現在的做法】（Slay the Spire 的地圖演算法）
        ///
        ///   1. 鋪一個固定的規則網格（gridColumns 欄 × mapLayers 層）
        ///   2. 從第 0 層挑一格，每次只能走到上一層**相鄰的欄**（左／同／右）
        ///   3. 重複 pathCount 次，並保證前幾條的起點不同
        ///   4. **路徑不能交叉**
        ///   5. 沒有被任何路徑經過的格子**直接不存在**
        ///
        /// 「不至於不合理」來自第 2 與第 4 條那兩個硬約束：
        /// 線不會橫跨整張圖，也不會打結。
        ///
        /// 【為什麼一定連得通】每條路徑都是從第 0 層一路走到頂，
        /// 所以每個活下來的格子，本來就在某條從起點到終點的路上。
        /// 舊版那種「事後補救孤兒節點」的邏輯在這裡不需要了。
        /// </summary>
        private static MapData GenerateProcedural(MapGenerationSettings s, System.Random rng)
        {
            var map = new MapData();

            int layerCount = Mathf.Max(2, s.mapLayers);
            int columns = Mathf.Max(1, s.gridColumns);
            int pathCount = Mathf.Max(1, s.pathCount);

            // Boss 層單獨處理（只有一個節點，所有路徑最後都匯進去），
            // 所以路徑只走到倒數第二層
            int walkTop = layerCount - 2;

            // edges[floor] = 這一層往上一層的所有連線 (from欄, to欄)。用來擋交叉
            var edges = new List<List<int[]>>();
            for (int f = 0; f <= walkTop; f++) edges.Add(new List<int[]>());

            // visited[floor, col]：這一格有沒有被走過
            var visited = new bool[layerCount, columns];

            // 保證前 startNodeCount 條路徑的起點在不同欄
            var startCols = new List<int>();
            for (int c = 0; c < columns; c++) startCols.Add(c);
            Shuffle(startCols, rng);

            int distinctStarts = Mathf.Clamp(s.startNodeCount, 1, Mathf.Min(columns, pathCount));

            for (int p = 0; p < pathCount; p++)
            {
                int col = p < distinctStarts ? startCols[p] : rng.Next(columns);
                visited[0, col] = true;

                for (int f = 0; f < walkTop; f++)
                {
                    int next = PickNextColumn(col, columns, edges[f], rng);

                    edges[f].Add(new[] { col, next });
                    visited[f + 1, next] = true;
                    col = next;
                }
            }

            // ── 把活下來的格子變成節點 ──
            var grid = new RunNodeData[layerCount, columns];

            for (int f = 0; f <= walkTop; f++)
            {
                for (int c = 0; c < columns; c++)
                {
                    if (!visited[f, c]) continue;
                    grid[f, c] = MakeNode(s, rng, f, c, columns, layerCount);
                    map.allNodes.Add(grid[f, c]);
                }
            }

            // ── Boss 層：只有一個，擺正中間，所有倒數第二層的節點都連過去 ──
            var boss = MakeNode(s, rng, layerCount - 1, 0, 1, layerCount);
            boss.nodeId = $"Node_{layerCount - 1}_boss";
            map.allNodes.Add(boss);

            // ── 依路徑連線 ──
            for (int f = 0; f <= walkTop; f++)
            {
                for (int e = 0; e < edges[f].Count; e++)
                {
                    RunNodeData from = grid[f, edges[f][e][0]];
                    RunNodeData to = grid[f + 1, edges[f][e][1]];
                    if (from != null && to != null) Connect(from, to);
                }
            }

            for (int c = 0; c < columns; c++)
            {
                if (grid[walkTop, c] != null) Connect(grid[walkTop, c], boss);
            }

            return map;
        }

        /// <summary>
        /// 從 col 走到上一層的哪一欄。只能是相鄰的三欄之一，而且不能跟
        /// 這一層已經畫好的線交叉。
        ///
        /// 【交叉怎麼判斷】兩條線 a1→a2 與 b1→b2 交叉，等價於
        /// 「起點的左右關係」與「終點的左右關係」相反 ——
        /// 也就是 (a1-b1) 與 (a2-b2) 異號。相等（合流）不算交叉。
        /// </summary>
        private static int PickNextColumn(int col, int columns, List<int[]> layerEdges, System.Random rng)
        {
            var options = new List<int>();

            for (int d = -1; d <= 1; d++)
            {
                int c = col + d;
                if (c < 0 || c >= columns) continue;

                bool crosses = false;
                for (int i = 0; i < layerEdges.Count; i++)
                {
                    int b1 = layerEdges[i][0];
                    int b2 = layerEdges[i][1];
                    if ((col - b1) * (c - b2) < 0) { crosses = true; break; }
                }

                if (!crosses) options.Add(c);
            }

            // 全部都會交叉時只好原地直上 —— 直上永遠不會跟任何線交叉
            if (options.Count == 0) return col;

            return options[rng.Next(options.Count)];
        }

        private static RunNodeData MakeNode(
            MapGenerationSettings s, System.Random rng,
            int layer, int col, int columns, int layerCount)
        {
            float baseX = columns <= 1
                ? 50f
                : s.horizontalMargin + (100f - s.horizontalMargin * 2f) / (columns - 1) * col;

            // 抖動只是為了不要看起來像方格紙，**不負責製造不規則** ——
            // 不規則來自哪些格子活下來。所以這個值小一點比較好看
            float jitter = (float)(rng.NextDouble() * 2 - 1) * s.horizontalJitter;

            return new RunNodeData
            {
                nodeId = $"Node_{layer}_{col}",
                kind = PickKind(s, rng, layer, layerCount),
                layer = layer,
                xPercent = Mathf.Clamp(baseX + jitter, 8f, 92f),
                yPercent = s.verticalMargin
                           + (100f - s.verticalMargin * 2f) / (layerCount - 1) * layer,
            };
        }

        private static void Shuffle(List<int> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int tmp = list[i]; list[i] = list[j]; list[j] = tmp;
            }
        }

        private static MapNodeKind PickKind(MapGenerationSettings s, System.Random rng, int layer, int layerCount)
        {
            if (layer == layerCount - 1) return MapNodeKind.Boss;

            // 起點層固定給一般事件，避免一開場就被迫戰鬥
            if (layer == 0) return MapNodeKind.Event;

            double roll = rng.NextDouble();

            if (roll < s.combatChance) return MapNodeKind.Combat;
            roll -= s.combatChance;

            if (roll < s.shopChance) return MapNodeKind.Shop;
            roll -= s.shopChance;

            if (roll < s.specialEventChance) return MapNodeKind.SpecialEvent;

            return MapNodeKind.Event;
        }

        private static void Connect(RunNodeData from, RunNodeData to)
        {
            if (!from.nextNodeIds.Contains(to.nodeId))
            {
                from.nextNodeIds.Add(to.nodeId);
            }
        }

        // ==========================================
        // DEMO 固定路線
        // ==========================================
        private static MapData GenerateDemoRoute(MapGenerationSettings s, System.Random rng)
        {
            var map = new MapData();

            List<MapNodeKind> kinds = s.demoRouteKinds;
            if (kinds == null || kinds.Count == 0) return CreateFallback();

            RunNodeData previous = null;

            for (int i = 0; i < kinds.Count; i++)
            {
                float yPercent = kinds.Count <= 1
                    ? 50f
                    : s.verticalMargin + (100f - s.verticalMargin * 2f) / (kinds.Count - 1) * i;

                var node = new RunNodeData
                {
                    nodeId = $"Node_{i}",
                    kind = kinds[i],
                    layer = i,
                    xPercent = 50f + (float)(rng.NextDouble() * 6 - 3),
                    yPercent = yPercent,
                };

                map.allNodes.Add(node);

                if (previous != null) Connect(previous, node);
                previous = node;
            }

            return map;
        }

        private static MapData CreateFallback()
        {
            var map = new MapData();

            map.allNodes.Add(new RunNodeData
            {
                nodeId = "Node_0",
                kind = MapNodeKind.Event,
                layer = 0,
                xPercent = 50f,
                yPercent = 30f,
            });

            return map;
        }
    }
}
