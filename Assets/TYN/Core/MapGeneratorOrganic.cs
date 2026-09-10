using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 【Organic 擺法】把節點當成「地圖上散落的地標」而不是方格紙上的格子。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【舊網格為什麼是扁的】
    /// `MakeNode` 裡 `yPercent = margin + 間距 * layer` —— y 是 layer 的**純函數**，
    /// 所以同一層的節點必然落在同一條水平線上。x 又被吸附到 gridColumns 個
    /// 固定欄位、只抖動 ±5%，於是連直的都對齊。看起來當然像表格。
    ///
    /// 【這一版怎麼做】從底部的一個原點放射生長：
    ///   · 半徑隨深度增加 → 深度仍然對應「往前推進」，玩家看得出方向
    ///   · 角度隨深度張開 → 樹冠展開的感覺
    ///   · **半徑帶抖動** → 相鄰深度的節點互相交錯，水平線就消失了
    ///   · Poisson 排斥 → 不會擠在一起，疏密不均才像真的地標
    ///
    /// 【為什麼中間寬兩頭窄】起點只有一兩個、Boss 只有一個，
    /// 中段最寬 —— 那就是「樹狀擴散」的形狀。純粹每層等寬會退回柵欄。
    ///
    /// 【橫向連線】除了父子之外再加一些同深度附近的連線（organicCrossLink），
    /// 讓路徑會重新匯合。純樹的話每一次選擇都是永久分岔，
    /// 玩家會覺得「選錯就回不去」而不是「路變多了」。
    /// </summary>
    public static partial class MapGenerator
    {
        /// <summary>一個生長中的節點：資料 + 它在平面上的位置。</summary>
        private class Grown
        {
            public RunNodeData data;
            public Vector2 pos;      // xPercent / yPercent
            public float angle;      // 相對原點的角度，連線時用來找鄰居
        }

        private static MapData GenerateOrganic(MapGenerationSettings s, System.Random rng)
        {
            var map = new MapData();
            int depths = Mathf.Max(2, s.mapLayers);

            // 原點在底部中央。半徑從這裡往上長
            Vector2 origin = new Vector2(50f, s.verticalMargin);
            float rEnd = 100f - s.verticalMargin * 2f;
            float band = rEnd / Mathf.Max(1, depths - 1);

            var byDepth = new List<List<Grown>>();
            var placed = new List<Grown>();

            for (int d = 0; d < depths; d++)
            {
                int want = WidthAt(s, d, depths);
                float rBase = band * d;
                float spread = s.organicSpread * Mathf.Lerp(0.25f, 1f, depths <= 1 ? 1f : (float)d / (depths - 1));

                var row = new List<Grown>();

                for (int i = 0; i < want; i++)
                {
                    Grown g = null;

                    // ⚠️ 試幾次就放棄。生不出來時**少一個節點**比硬塞一個
                    //    貼在別人身上好 —— 疏密不均本來就是我們要的
                    for (int attempt = 0; attempt < 24 && g == null; attempt++)
                    {
                        float t = want <= 1 ? 0.5f : (i + 0.5f) / want;
                        float aJit = (float)(rng.NextDouble() * 2 - 1) * s.organicAngleJitter / Mathf.Max(1, want);
                        float ang = Mathf.Lerp(-spread * 0.5f, spread * 0.5f, Mathf.Clamp01(t + aJit));

                        float rJit = (float)(rng.NextDouble() * 2 - 1) * s.organicRadialJitter * band;
                        float r = Mathf.Max(0f, rBase + rJit);

                        float rad = ang * Mathf.Deg2Rad;
                        Vector2 p = origin + new Vector2(Mathf.Sin(rad) * r, Mathf.Cos(rad) * r);

                        // 起點層與 Boss 層不抖半徑，不然會跑到邊界外或穿過彼此
                        if (d == 0 || d == depths - 1)
                            p = origin + new Vector2(Mathf.Sin(rad) * rBase, Mathf.Cos(rad) * rBase);

                        p.x = Mathf.Clamp(p.x, s.horizontalMargin * 0.5f, 100f - s.horizontalMargin * 0.5f);
                        p.y = Mathf.Clamp(p.y, 6f, 94f);

                        if (TooClose(placed, p, s.organicMinSpacing)) continue;

                        g = new Grown { pos = p, angle = ang };
                    }

                    if (g == null) continue;

                    g.data = new RunNodeData
                    {
                        nodeId = "Node_" + d + "_" + i,
                        kind = PickKind(s, rng, d, depths),
                        layer = d,
                        dressingSeed = rng.Next(),
                        xPercent = g.pos.x,
                        yPercent = g.pos.y,
                    };

                    row.Add(g);
                    placed.Add(g);
                    map.allNodes.Add(g.data);
                }

                // 一個都沒生出來會斷層 —— 放寬間距硬放一個在正中央
                if (row.Count == 0)
                {
                    Vector2 p = origin + new Vector2(0f, rBase);
                    var g = new Grown { pos = p, angle = 0f };
                    g.data = new RunNodeData
                    {
                        nodeId = "Node_" + d + "_0",
                        kind = PickKind(s, rng, d, depths),
                        layer = d,
                        dressingSeed = rng.Next(),
                        xPercent = p.x,
                        yPercent = p.y,
                    };
                    row.Add(g); placed.Add(g); map.allNodes.Add(g.data);

                    Debug.LogWarning("[地圖生成] 第 " + d + " 層擠不下任何節點（Min Spacing "
                        + s.organicMinSpacing + " 太大？），硬放了一個在中央。");
                }

                byDepth.Add(row);
            }

            ConnectOrganic(byDepth, s, rng);
            return map;
        }

        /// <summary>
        /// 這個深度要幾個節點。**中間寬、兩頭窄** —— 那就是樹狀擴散的形狀。
        /// 起點用 startNodeCount，最後一層固定 1（Boss 只有一個）。
        /// </summary>
        private static int WidthAt(MapGenerationSettings s, int d, int depths)
        {
            if (d == 0) return Mathf.Max(1, s.startNodeCount);
            if (d == depths - 1) return 1;

            // 0 → 1 → 0 的鐘形，中段最寬
            float t = (float)d / Mathf.Max(1, depths - 1);
            float bell = Mathf.Sin(t * Mathf.PI);
            return Mathf.Max(2, Mathf.RoundToInt(Mathf.Lerp(2f, s.organicWidthMax, bell)));
        }

        private static bool TooClose(List<Grown> placed, Vector2 p, float minDist)
        {
            for (int i = 0; i < placed.Count; i++)
                if ((placed[i].pos - p).sqrMagnitude < minDist * minDist) return true;
            return false;
        }

        // ==========================================
        // 連線
        // ==========================================

        /// <summary>
        /// 逐層連線。三條規則，順序不能反：
        ///   ① 每個上層節點**至少要有一個父親** —— 少了就是走不到的死節點
        ///   ② 每個下層節點**至少要有一個孩子** —— 少了就是走進去出不來的死路
        ///   ③ 剩下的看 organicCrossLink 加，加之前先檢查會不會交叉
        ///
        /// 【為什麼用角度找鄰居而不是距離】半徑帶了抖動，用距離會把
        /// 「更深但偏一邊」的節點誤判成鄰居，連出來的線會斜著穿過整張圖。
        /// 角度是「同一個方向上的下一站」，那才是玩家心裡的鄰接。
        /// </summary>
        private static void ConnectOrganic(
            List<List<Grown>> byDepth, MapGenerationSettings s, System.Random rng)
        {
            var edges = new List<(Vector2 a, Vector2 b)>();

            // 一條連線最長容許多長。超過一層半的距離就不是「下一站」而是「橫跨半張圖」
            float bandLen = (100f - s.verticalMargin * 2f) / Mathf.Max(1, byDepth.Count - 1);
            float maxLen = bandLen * 1.8f;

            for (int d = 0; d + 1 < byDepth.Count; d++)
            {
                List<Grown> lower = byDepth[d];
                List<Grown> upper = byDepth[d + 1];
                if (lower.Count == 0 || upper.Count == 0) continue;

                // ① 每個上層節點認一個父親
                for (int u = 0; u < upper.Count; u++)
                {
                    Grown parent = PickPartner(lower, upper[u], edges, maxLen);
                    Link(parent, upper[u], edges);
                }

                // ② 沒有孩子的下層節點補一條
                for (int l = 0; l < lower.Count; l++)
                {
                    if (lower[l].data.nextNodeIds.Count > 0) continue;
                    Grown child = PickPartner(upper, lower[l], edges, maxLen);
                    Link(lower[l], child, edges);
                }

                // ③ 額外的橫向連線 —— 路徑會重新匯合，選擇才有意義
                if (s.organicCrossLink <= 0f) continue;

                for (int l = 0; l < lower.Count; l++)
                {
                    for (int u = 0; u < upper.Count; u++)
                    {
                        if (lower[l].data.nextNodeIds.Contains(upper[u].data.nodeId)) continue;
                        if (rng.NextDouble() > s.organicCrossLink) continue;

                        // ⚠️ 交叉的線在畫面上會變成一團毛線，而且玩家看不出誰通到誰
                        if ((upper[u].pos - lower[l].pos).magnitude > maxLen) continue;
                        if (WouldCross(lower[l].pos, upper[u].pos, edges)) continue;

                        Link(lower[l], upper[u], edges);
                    }
                }
            }
        }

        private static void Link(Grown from, Grown to, List<(Vector2 a, Vector2 b)> edges)
        {
            if (from == null || to == null) return;
            if (from.data.nextNodeIds.Contains(to.data.nodeId)) return;

            from.data.nextNodeIds.Add(to.data.nodeId);
            edges.Add((from.pos, to.pos));
        }

        /// <summary>
        /// 從 pool 裡挑一個對象連。**照角度由近到遠試，挑第一個不會交叉的。**
        ///
        /// 【為什麼不能只挑角度最近的】半徑帶了抖動，角度最近的那個有可能
        /// 位置上在另一條線的另一側 —— 連下去就是一條橫穿整張圖的線。
        /// 交叉的線在畫面上是一團毛線，玩家看不出哪一站通到哪一站。
        ///
        /// 【全部都會交叉時】還是要連 —— 連通性優先於好看。
        /// 那時挑角度最近的，至少是最短的那一條。
        /// </summary>
        private static Grown PickPartner(
            List<Grown> pool, Grown from, List<(Vector2 a, Vector2 b)> edges, float maxLen)
        {
            if (pool.Count == 0) return null;

            // ⚠️ 照**距離**排，不是角度。半徑帶了抖動之後角度最近的可能離很遠，
            //    連下去就是一條橫穿整張圖的線 —— 那正是這一版第一次出圖時的問題
            var order = new List<Grown>(pool);
            Vector2 f = from.pos;
            order.Sort(delegate (Grown x, Grown y)
            {
                return (x.pos - f).sqrMagnitude.CompareTo((y.pos - f).sqrMagnitude);
            });

            // 先找「夠近而且不交叉」的
            for (int i = 0; i < order.Count; i++)
            {
                if ((order[i].pos - from.pos).magnitude > maxLen) continue;
                if (WouldCross(order[i].pos, from.pos, edges)) continue;
                return order[i];
            }

            // 退一步：不交叉就好，長一點認了
            for (int i = 0; i < order.Count; i++)
                if (!WouldCross(order[i].pos, from.pos, edges)) return order[i];

            // 再退一步：連通性優先於好看，挑最短的
            Grown shortest = order[0];
            float best = (order[0].pos - from.pos).sqrMagnitude;
            for (int i = 1; i < order.Count; i++)
            {
                float dd = (order[i].pos - from.pos).sqrMagnitude;
                if (dd >= best) continue;
                best = dd; shortest = order[i];
            }
            return shortest;
        }

        /// <summary>
        /// 這條新線會不會跟已經有的線交叉。
        /// 共用端點的不算交叉（那是正常的分岔與匯合）。
        /// </summary>
        private static bool WouldCross(Vector2 a, Vector2 b, List<(Vector2 a, Vector2 b)> edges)
        {
            for (int i = 0; i < edges.Count; i++)
            {
                Vector2 c = edges[i].a, e = edges[i].b;
                if (a == c || a == e || b == c || b == e) continue;
                if (SegmentsIntersect(a, b, c, e)) return true;
            }
            return false;
        }

        private static bool SegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            float d1 = Cross(p3, p4, p1);
            float d2 = Cross(p3, p4, p2);
            float d3 = Cross(p1, p2, p3);
            float d4 = Cross(p1, p2, p4);

            return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0))
                && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
        }

        private static float Cross(Vector2 o, Vector2 a, Vector2 b)
        {
            return (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
        }
    }
}
