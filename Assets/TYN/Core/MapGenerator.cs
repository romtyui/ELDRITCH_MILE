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

            MapData map = settings.useDemoRoute
                ? GenerateDemoRoute(settings, rng)
                : GenerateProcedural(settings, rng);

            EnsureGuaranteedKinds(map, settings, rng);
            WarnIfUnreachable(map);
            return map;
        }

        /// <summary>
        /// 清單裡的節點類型如果一個都沒抽到，就挑一個**中段的探索節點改成它**。
        ///
        /// ────────────────────────────────────────────────────────
        /// 【為什麼需要】純機率會漏。實測 200 張圖：
        /// **13% 完全沒有商店、6.5% 完全沒有對話**。
        /// 測試的人抽到那種圖就整個環節驗不到，而且會以為是功能壞了而不是運氣。
        ///
        /// 【為什麼是「改類型」而不是「插一個節點」】
        /// 插節點要重算連線，那會動到關卡形狀；改類型**完全不碰 `nextNodeIds`**，
        /// 所以連通性、交叉、層數全部不受影響 —— 這是最小的介入。
        ///
        /// 【為什麼只挑探索節點】探索房是「其餘機率歸它」的那一類，
        /// 本來就是填充用的，少一間最不痛。挑不到探索節點就放棄（不硬換戰鬥），
        /// 那種圖小到連填充節點都沒有，硬塞只會讓它更奇怪。
        ///
        /// 【為什麼不含首排與 Boss 層】那兩層的類型是**固定**的
        /// （`firstLayerKind` / `Boss`），改掉就破壞了「首排一定拿得到神牌」的保證。
        /// </summary>
        private static void EnsureGuaranteedKinds(MapData map, MapGenerationSettings s, System.Random rng)
        {
            if (map == null || s == null || s.guaranteedKinds == null || s.guaranteedKinds.Count == 0) return;

            int top = map.MaxLayer;

            for (int k = 0; k < s.guaranteedKinds.Count; k++)
            {
                MapNodeKind want = s.guaranteedKinds[k];

                bool present = false;
                for (int i = 0; i < map.allNodes.Count && !present; i++)
                {
                    if (map.allNodes[i].kind == want) present = true;
                }
                if (present) continue;

                // 候選：中段（不含首排與 Boss 層）的探索節點
                var candidates = new List<RunNodeData>();
                for (int i = 0; i < map.allNodes.Count; i++)
                {
                    RunNodeData n = map.allNodes[i];
                    if (n.layer <= 0 || n.layer >= top) continue;
                    if (n.kind != MapNodeKind.Event) continue;
                    candidates.Add(n);
                }

                if (candidates.Count == 0)
                {
                    Debug.LogWarning(
                        $"[地圖生成] 這張圖保證不了「{want}」—— 中段沒有可以讓出來的探索節點。\n" +
                        "地圖太小（層數或路徑數太少）。要嘛調大 Map Layers／Path Count，\n" +
                        "要嘛把這一項從 Guaranteed Kinds 拿掉。");
                    continue;
                }

                RunNodeData chosen = candidates[rng.Next(candidates.Count)];
                chosen.kind = want;

                Debug.Log($"[地圖生成] 補上保證的「{want}」：{chosen.nodeId} 由探索改成它（連線沒動）");
            }
        }

        /// <summary>
        /// 檢查有沒有「走不到」或「走進去出不來」的節點，有就在 Console 說清楚。
        ///
        /// 【為什麼一定要有這一關】節點少一條線**不會有任何錯誤訊息** ——
        /// 畫面上那個節點還在，只是永遠是暗的、點不下去。
        /// 玩家（跟我們）會以為那是「還沒解鎖」，而不是「地圖漏了一條線」。
        ///
        /// 【只是警告，不自動補線】補線會改變關卡形狀，那是設計決定。
        /// 這裡的職責是**讓問題現形**，不是替設計做主。
        /// </summary>
        private static void WarnIfUnreachable(MapData map)
        {
            if (map == null || map.allNodes.Count == 0) return;

            // ── 從第 0 層做一次 BFS ──
            var reached = new HashSet<string>();
            var queue = new Queue<RunNodeData>();

            for (int i = 0; i < map.allNodes.Count; i++)
            {
                if (map.allNodes[i].layer != 0) continue;
                reached.Add(map.allNodes[i].nodeId);
                queue.Enqueue(map.allNodes[i]);
            }

            while (queue.Count > 0)
            {
                RunNodeData n = queue.Dequeue();
                for (int i = 0; i < n.nextNodeIds.Count; i++)
                {
                    RunNodeData next = map.GetNode(n.nextNodeIds[i]);
                    if (next == null || !reached.Add(next.nodeId)) continue;
                    queue.Enqueue(next);
                }
            }

            var orphans = new List<string>();
            var deadEnds = new List<string>();
            int top = map.MaxLayer;

            for (int i = 0; i < map.allNodes.Count; i++)
            {
                RunNodeData n = map.allNodes[i];

                if (!reached.Contains(n.nodeId)) orphans.Add($"{n.nodeId}({n.kind})");

                // 最後一層本來就沒有出邊，那不是死路
                if (n.layer < top && n.nextNodeIds.Count == 0) deadEnds.Add($"{n.nodeId}({n.kind})");
            }

            if (orphans.Count == 0 && deadEnds.Count == 0) return;

            Debug.LogWarning(
                "[地圖生成] 這張圖有走不通的地方 —— 玩家會看到一個永遠是暗的節點。\n" +
                (orphans.Count > 0 ? $"　從起點走不到（{orphans.Count}）：{string.Join("、", orphans.ToArray())}\n" : "") +
                (deadEnds.Count > 0 ? $"　走進去出不來（{deadEnds.Count}）：{string.Join("、", deadEnds.ToArray())}\n" : "") +
                "　DEMO 分支路線的話檢查 Demo Route Layers；隨機生成的話是 MapGenerator 的 bug。");
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
                // 每個節點一個獨立的擺設種子。探索與戰鬥都讀它，
                // 所以同一站的背景在兩個 Stage 裡是同一套
                dressingSeed = rng.Next(),
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

            // 起點層固定 —— 避免一開場就被迫戰鬥。
            // 放什麼由 `firstLayerKind` 決定（預設 SpecialEvent ＝ 開場就挑神牌）
            if (layer == 0) return s.firstLayerKind;

            double roll = rng.NextDouble();

            if (roll < s.combatChance) return MapNodeKind.Combat;
            roll -= s.combatChance;

            if (roll < s.shopChance) return MapNodeKind.Shop;
            roll -= s.shopChance;

            if (roll < s.specialEventChance) return MapNodeKind.SpecialEvent;
            roll -= s.specialEventChance;

            // ⚠️ 對話節點以前**不在這個表裡** —— 隨機地圖因此從來不會長出對話，
            //    只有 DEMO 的固定路線寫死了幾個。換成隨機生成就整個環節測不到，
            //    而且不會報錯，只會「怎麼玩都沒遇到對話」
            if (roll < s.dialogueChance) return MapNodeKind.Dialogue;

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
            return s.demoRouteShape == DemoRouteShape.Branching
                ? GenerateDemoBranching(s, rng)
                : GenerateDemoStraight(s, rng);
        }

        /// <summary>
        /// 有分支的固定路線。一層可以有好幾個節點，玩家要選一個走。
        ///
        /// ────────────────────────────────────────────────────────
        /// 【連線怎麼算】見 <see cref="ConnectLayers"/> —— 重點是
        /// **每個節點都保證至少一條入邊與一條出邊**，不會出現走不到的節點。
        ///
        /// 【第 0 層為什麼被蓋掉】`firstLayerKind` 才是「首排放什麼」的單一真相
        /// （隨機生成也讀同一格）。在兩個地方各寫一次的話，改了一邊忘了另一邊，
        /// 症狀會是「隨機圖開場是神牌、DEMO 圖開場是探索房」，而且不會有錯誤訊息。
        /// </summary>
        private static MapData GenerateDemoBranching(MapGenerationSettings s, System.Random rng)
        {
            var map = new MapData();

            List<DemoLayer> layers = s.demoRouteLayers;
            if (layers == null || layers.Count == 0)
            {
                Debug.LogWarning(
                    "[地圖生成] Demo Route Shape 是 Branching，但 Demo Route Layers 是空的。" +
                    "改用直線那一份。");
                return GenerateDemoStraight(s, rng);
            }

            var built = new List<List<RunNodeData>>();

            for (int f = 0; f < layers.Count; f++)
            {
                List<MapNodeKind> row = layers[f] != null ? layers[f].nodes : null;
                int count = row != null ? row.Count : 0;
                if (count == 0) continue;

                var made = new List<RunNodeData>(count);

                for (int c = 0; c < count; c++)
                {
                    RunNodeData node = MakeNode(s, rng, f, c, count, layers.Count);
                    node.nodeId = $"Node_{f}_{c}";

                    // 第 0 層一律照 firstLayerKind，其餘照表填
                    node.kind = f == 0 ? s.firstLayerKind : row[c];

                    made.Add(node);
                    map.allNodes.Add(node);
                }

                built.Add(made);
            }

            for (int i = 0; i + 1 < built.Count; i++) ConnectLayers(built[i], built[i + 1]);

            return map;
        }

        /// <summary>
        /// 把相鄰兩層連起來，**兩個方向的保證各跑一次**：
        ///
        ///   1. 每個下層節點都連到上層「相對位置最近」的那一個 → 沒有死路
        ///   2. 每個上層節點都被下層「相對位置最近」的那一個連到 → 沒有孤兒
        ///
        /// 【為什麼要跑兩輪】只跑第 1 輪的話，上層比下層多時多出來的那些
        /// 永遠沒有人連過去（3 個節點的下一層有 5 個，就會有 2 個是孤兒）；
        /// 只跑第 2 輪則相反。兩輪都跑，兩種都不會發生。
        ///
        /// 【為什麼不會交叉】兩輪用的都是同一個**單調遞增**的對應
        /// （`NearestIndex` 是 i 的遞增函數），所以線不會打結 ——
        /// 這跟隨機生成那邊靠 `PickNextColumn` 擋交叉是同一個目的，
        /// 只是這裡用「本來就不可能交叉的算法」達成，不必事後檢查。
        ///
        /// `Connect` 自己會去重，所以兩輪算到同一條線也不會連兩次。
        /// </summary>
        private static void ConnectLayers(List<RunNodeData> lower, List<RunNodeData> upper)
        {
            if (lower == null || upper == null || lower.Count == 0 || upper.Count == 0) return;

            int n = lower.Count;
            int m = upper.Count;

            for (int i = 0; i < n; i++) Connect(lower[i], upper[NearestIndex(i, n, m)]);
            for (int j = 0; j < m; j++) Connect(lower[NearestIndex(j, m, n)], upper[j]);
        }

        /// <summary>
        /// 「一排 from 個裡的第 i 個」對應到「一排 to 個裡的第幾個」。
        /// 兩排都攤平成 0~1 再對過去，所以節點數不同也對得起來。
        ///
        /// from 只有一個時回 0 —— 那時另一輪會把剩下的補齊（見 ConnectLayers）。
        /// </summary>
        private static int NearestIndex(int i, int from, int to)
        {
            if (to <= 1 || from <= 1) return 0;
            return Mathf.Clamp(Mathf.RoundToInt(i * (to - 1f) / (from - 1f)), 0, to - 1);
        }

        /// <summary>一層一個節點的直線路線。舊的 DEMO 就是這個。</summary>
        private static MapData GenerateDemoStraight(MapGenerationSettings s, System.Random rng)
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
                    dressingSeed = rng.Next(),
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
