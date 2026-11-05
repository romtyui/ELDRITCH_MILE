using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 地圖「組成」的規則：每條路線上的商店數、探索與戰鬥的比例。（2026-10-25 企劃）
    ///
    /// ────────────────────────────────────────────────────────
    /// 【為什麼是生成完再調，不是擲類型時就控制】
    /// 「每條路線 1~2 間商店」是**整條路**的性質 —— 擲單一節點時看不到它在哪幾條路上。
    /// 所以先照機率擲完（不擲商店），再看整張圖把商店補進去。
    ///
    /// 【只改類型、不動連線】跟 EnsureGuaranteedKinds、PlaceGates 同一個做法 ——
    /// 連通性、交叉、層數全部不受影響。
    /// </summary>
    public static partial class MapGenerator
    {
        private static void ComposeKinds(MapData map, MapGenerationSettings s, System.Random rng)
        {
            if (map == null || s == null || map.allNodes.Count == 0) return;

            if (s.shopsPerRoute) PlaceShopsPerRoute(map, s, rng);
            if (s.balanceExploreCombat) BalanceExploreCombat(map, rng);
        }

        // ==========================================
        // 商店：每條路線 1~2 間，不在開頭
        // ==========================================

        /// <summary>
        /// 先把商店全部清掉，再一間一間放回去，直到「每一條從起點到首領的路都至少經過一間」。
        /// 每放一間都確認「沒有任何一條路因此超過上限」。
        ///
        /// 挑哪一個：**能讓最多條「還沒有商店的路」經過的那一個** —— 一間顧到越多路，總數就越少。
        /// 路徑數用 double 算（40 個節點的圖路徑數可以上萬，int 會溢位）。
        /// </summary>
        private static void PlaceShopsPerRoute(MapData map, MapGenerationSettings s, System.Random rng)
        {
            List<RunNodeData> order = TopoOrder(map);
            if (order == null) { Debug.LogWarning("[地圖生成] 圖有環，商店規則跳過"); return; }

            int top = map.MaxLayer;
            int minLayer = Mathf.Max(1, Mathf.CeilToInt(top * s.shopMinLayerRatio));
            int maxPer = Mathf.Max(1, s.maxShopsPerRoute);

            // 先清掉 —— 機率擲出來的、EnsureGuaranteedKinds 補的，都重新安排
            for (int i = 0; i < map.allNodes.Count; i++)
                if (map.allNodes[i].kind == MapNodeKind.Shop) map.allNodes[i].kind = MapNodeKind.Event;

            var parents = Parents(map);

            // 一次一間的貪婪放法偶爾會把自己逼進死角（剩下的路上每一格放下去都會讓別條路超過上限）。
            // 實測約 5% 的圖會這樣 —— 換一組隨機順序重放幾次，留最好的那一次
            var original = new Dictionary<string, MapNodeKind>();
            for (int i = 0; i < map.allNodes.Count; i++) original[map.allNodes[i].nodeId] = map.allNodes[i].kind;

            double bestRemaining = double.MaxValue;
            List<string> bestShops = null;
            for (int attempt = 0; attempt < 8 && bestRemaining > 0; attempt++)
            {
                for (int i = 0; i < map.allNodes.Count; i++) map.allNodes[i].kind = original[map.allNodes[i].nodeId];
                double remaining = PlaceShopsOnce(map, order, parents, minLayer, maxPer, rng, attempt == 0 ? 0.25 : 1.0);
                if (remaining < bestRemaining)
                {
                    bestRemaining = remaining;
                    bestShops = new List<string>();
                    for (int i = 0; i < map.allNodes.Count; i++) if (map.allNodes[i].kind == MapNodeKind.Shop) bestShops.Add(map.allNodes[i].nodeId);
                }
            }

            for (int i = 0; i < map.allNodes.Count; i++) map.allNodes[i].kind = original[map.allNodes[i].nodeId];
            if (bestShops != null)
                for (int i = 0; i < bestShops.Count; i++) { RunNodeData n = map.GetNode(bestShops[i]); n.kind = MapNodeKind.Shop; n.enemyId = null; }

            if (bestRemaining > 0)
                Debug.LogWarning($"[地圖生成] 重放 8 次之後還有 {bestRemaining:0} 條路沒有商店（第 {minLayer} 層以後、不讓任何路超過 {maxPer} 間）");
        }

        /// <summary>貪婪放一輪。回傳放完之後還有幾條路沒有商店。</summary>
        private static double PlaceShopsOnce(MapData map, List<RunNodeData> order, Dictionary<string, List<RunNodeData>> parents,
                                             int minLayer, int maxPer, System.Random rng, double jitter)
        {
            int top = map.MaxLayer;
            double remaining = 0;

            for (int guard = 0; guard < 60; guard++)
            {
                var zeroFrom = new Dictionary<string, double>();
                var maxFrom = new Dictionary<string, int>();
                for (int i = 0; i < order.Count; i++)
                {
                    RunNodeData n = order[i];
                    bool shop = n.kind == MapNodeKind.Shop;
                    double zf = 0; int mf = 0; bool hasParent = false;
                    List<RunNodeData> ps;
                    if (parents.TryGetValue(n.nodeId, out ps))
                        for (int k = 0; k < ps.Count; k++) { hasParent = true; zf += zeroFrom[ps[k].nodeId]; mf = Mathf.Max(mf, maxFrom[ps[k].nodeId]); }
                    if (!hasParent) zf = 1;   // 起點
                    zeroFrom[n.nodeId] = shop ? 0 : zf;
                    maxFrom[n.nodeId] = mf + (shop ? 1 : 0);
                }

                var zeroTo = new Dictionary<string, double>();
                var maxTo = new Dictionary<string, int>();
                for (int i = order.Count - 1; i >= 0; i--)
                {
                    RunNodeData n = order[i];
                    bool shop = n.kind == MapNodeKind.Shop;
                    double zt = 0; int mt = 0; bool hasChild = false;
                    for (int k = 0; k < n.nextNodeIds.Count; k++)
                    {
                        double v; if (!zeroTo.TryGetValue(n.nextNodeIds[k], out v)) continue;
                        hasChild = true; zt += v; mt = Mathf.Max(mt, maxTo[n.nextNodeIds[k]]);
                    }
                    if (!hasChild) zt = 1;    // 終點（首領，或通道之類的盡頭）
                    zeroTo[n.nodeId] = shop ? 0 : zt;
                    maxTo[n.nodeId] = mt + (shop ? 1 : 0);
                }

                remaining = 0;
                for (int i = 0; i < order.Count; i++)
                    if (order[i].nextNodeIds.Count == 0) remaining += zeroFrom[order[i].nodeId];
                if (remaining <= 0) return 0;

                RunNodeData best = null; double bestScore = 0;
                for (int i = 0; i < order.Count; i++)
                {
                    RunNodeData n = order[i];
                    if (n.layer < minLayer || n.layer >= top) continue;
                    // 探索、戰鬥優先；遭遇與休息也可以讓出來（只限這兩種的話，
                    // 某些路在可放的層數裡剛好全是遭遇／休息，那條路就永遠沒有商店）
                    bool filler = n.kind == MapNodeKind.Event || n.kind == MapNodeKind.Combat;
                    if (!filler && n.kind != MapNodeKind.Dialogue && n.kind != MapNodeKind.Rest) continue;

                    double through = zeroFrom[n.nodeId] * zeroTo[n.nodeId];
                    if (through <= 0) continue;

                    // 放下去之後，經過它的路最多有幾間
                    int before = 0, after = 0;
                    List<RunNodeData> ps;
                    if (parents.TryGetValue(n.nodeId, out ps)) for (int k = 0; k < ps.Count; k++) before = Mathf.Max(before, maxFrom[ps[k].nodeId]);
                    for (int k = 0; k < n.nextNodeIds.Count; k++) { int v; if (maxTo.TryGetValue(n.nextNodeIds[k], out v)) after = Mathf.Max(after, v); }
                    if (before + 1 + after > maxPer) continue;

                    double score = through * (1.0 + rng.NextDouble() * jitter);   // 一點點隨機，免得每張圖都挑一樣的位置
                    if (!filler) score *= 0.5;
                    if (score > bestScore) { bestScore = score; best = n; }
                }

                if (best == null) return remaining;

                best.kind = MapNodeKind.Shop;
            }
            return remaining;
        }

        // ==========================================
        // 探索：戰鬥 ≈ 1:1
        // ==========================================

        /// <summary>中段的探索與戰鬥，多的那一邊隨機改幾個成少的那一邊，差距壓到 1 以內。</summary>
        private static void BalanceExploreCombat(MapData map, System.Random rng)
        {
            int top = map.MaxLayer;
            var ev = new List<RunNodeData>();
            var cb = new List<RunNodeData>();
            for (int i = 0; i < map.allNodes.Count; i++)
            {
                RunNodeData n = map.allNodes[i];
                if (n.layer <= 0 || n.layer >= top) continue;
                if (n.kind == MapNodeKind.Event) ev.Add(n);
                else if (n.kind == MapNodeKind.Combat) cb.Add(n);
            }

            while (Mathf.Abs(ev.Count - cb.Count) > 1)
            {
                List<RunNodeData> from = ev.Count > cb.Count ? ev : cb;
                List<RunNodeData> to = from == ev ? cb : ev;
                RunNodeData n = from[rng.Next(from.Count)];
                from.Remove(n);
                n.kind = to == cb ? MapNodeKind.Combat : MapNodeKind.Event;
                to.Add(n);
            }
        }

        // ==========================================

        private static Dictionary<string, List<RunNodeData>> Parents(MapData map)
        {
            var parents = new Dictionary<string, List<RunNodeData>>();
            for (int i = 0; i < map.allNodes.Count; i++)
            {
                RunNodeData n = map.allNodes[i];
                for (int k = 0; k < n.nextNodeIds.Count; k++)
                {
                    List<RunNodeData> list;
                    if (!parents.TryGetValue(n.nextNodeIds[k], out list)) parents[n.nextNodeIds[k]] = list = new List<RunNodeData>();
                    list.Add(n);
                }
            }
            return parents;
        }

        /// <summary>拓撲排序（父節點一定排在子節點前面）。有環回 null。</summary>
        private static List<RunNodeData> TopoOrder(MapData map)
        {
            var indeg = new Dictionary<string, int>();
            for (int i = 0; i < map.allNodes.Count; i++) indeg[map.allNodes[i].nodeId] = 0;
            for (int i = 0; i < map.allNodes.Count; i++)
                for (int k = 0; k < map.allNodes[i].nextNodeIds.Count; k++)
                {
                    string c = map.allNodes[i].nextNodeIds[k];
                    if (indeg.ContainsKey(c)) indeg[c]++;
                }

            var q = new Queue<RunNodeData>();
            for (int i = 0; i < map.allNodes.Count; i++) if (indeg[map.allNodes[i].nodeId] == 0) q.Enqueue(map.allNodes[i]);

            var order = new List<RunNodeData>();
            while (q.Count > 0)
            {
                RunNodeData n = q.Dequeue();
                order.Add(n);
                for (int k = 0; k < n.nextNodeIds.Count; k++)
                {
                    string c = n.nextNodeIds[k];
                    if (!indeg.ContainsKey(c)) continue;
                    if (--indeg[c] == 0) { RunNodeData cn = map.GetNode(c); if (cn != null) q.Enqueue(cn); }
                }
            }
            return order.Count == map.allNodes.Count ? order : null;
        }
    }
}
