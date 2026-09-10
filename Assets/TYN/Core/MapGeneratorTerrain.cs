using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 【Terrain 擺法】讓節點長在**地圖真的畫出來的地形**上。
    ///
    /// ────────────────────────────────────────────────────────
    /// 為什麼前一版還是像「灑在紙上」：它完全不看底圖。
    /// 底下畫著海、河、等高線，節點卻可能正好站在水中央 ——
    /// 那一眼就看得出是程式亂放的，不是地標。
    ///
    /// 這一版分三步，每一步解一個具體抱怨：
    ///   ① **適宜度圖**（水域不放、海岸加權）→ 「像真實的地圖」
    ///   ② **best-candidate 取樣**（藍雜訊）→ 解「擠在一起」
    ///   ③ **Delaunay 三角化連線** → 連線**保證不交叉**（三角化是平面圖），
    ///      而且每個節點自然會有數個鄰居 → 「選擇更自由」
    ///
    /// 【業界對照】這一套是 Amit Patel（Red Blob Games）那篇
    /// Polygonal Map Generation 的簡化版：他用 Poisson disc 取點、
    /// Voronoi/Delaunay 建拓撲，再依地形分類。差別是我們的地形不是
    /// 程式生成的，而是**直接讀美術畫好的底圖**。
    /// </summary>
    public static partial class MapGenerator
    {
        /// <summary>
        /// 地形適宜度。0 = 不能放（水），越大越想放。
        /// 解析度刻意壓到 128 —— 2048 的原圖逐點掃是幾百萬次，
        /// 而我們只需要「這一帶能不能站人」的精度。
        /// </summary>
        private class Suitability
        {
            public const int Res = 128;

            public float[,] weight = new float[Res, Res];
            public bool[,] water = new bool[Res, Res];
            public bool anyLand;

            /// <summary>xPercent / yPercent（0~100）換算成格子座標。</summary>
            public static Vector2Int ToCell(Vector2 pct)
            {
                return new Vector2Int(
                    Mathf.Clamp(Mathf.RoundToInt(pct.x / 100f * (Res - 1)), 0, Res - 1),
                    Mathf.Clamp(Mathf.RoundToInt(pct.y / 100f * (Res - 1)), 0, Res - 1));
            }

            public float At(Vector2 pct)
            {
                Vector2Int c = ToCell(pct);
                return weight[c.x, c.y];
            }
        }

        /// <summary>
        /// 掃一次底圖建適宜度。
        ///
        /// 【為什麼海岸要加權】真實地圖上的聚落幾乎都貼著水 ——
        /// 港口、渡口、水源。均勻灑點看起來像抽樣調查，不像村落。
        ///
        /// 【距水距離用 BFS 而不是逐點量】逐點對所有水格算距離是 O(n²)，
        /// 128×128 就要一億次。多源 BFS 一次掃完，是 O(n)。
        /// </summary>
        private static Suitability BuildSuitability(MapGenerationSettings s)
        {
            var su = new Suitability();
            int R = Suitability.Res;

            Texture2D tex = s.terrainMap;
            if (tex == null) return null;

            if (!tex.isReadable)
            {
                Debug.LogWarning(
                    "[地圖生成] 地形底圖「" + tex.name + "」沒有打開 Read/Write，讀不到像素。"
                    + " 這一次退回 Organic 擺法。");
                return null;
            }

            // ── 水陸判定 ──
            var frontier = new Queue<Vector2Int>();
            var dist = new int[R, R];
            for (int y = 0; y < R; y++)
                for (int x = 0; x < R; x++)
                {
                    float u = (float)x / (R - 1);
                    float vv = (float)y / (R - 1);
                    Color c = tex.GetPixelBilinear(u, vv);
                    float lum = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;

                    bool isWater = lum < s.waterThreshold;
                    su.water[x, y] = isWater;
                    dist[x, y] = isWater ? 0 : int.MaxValue;

                    if (isWater) frontier.Enqueue(new Vector2Int(x, y));
                    else su.anyLand = true;
                }

            if (!su.anyLand)
            {
                Debug.LogWarning("[地圖生成] 地形底圖整張都低於 Water Threshold（"
                    + s.waterThreshold + "），沒有陸地可以站。這一次退回 Organic 擺法。");
                return null;
            }

            // ── 多源 BFS：每一格離最近的水有多遠 ──
            int[] dx = new int[] { 1, -1, 0, 0 };
            int[] dy = new int[] { 0, 0, 1, -1 };
            while (frontier.Count > 0)
            {
                Vector2Int p = frontier.Dequeue();
                for (int k = 0; k < 4; k++)
                {
                    int nx = p.x + dx[k], ny = p.y + dy[k];
                    if (nx < 0 || ny < 0 || nx >= R || ny >= R) continue;
                    if (dist[nx, ny] != int.MaxValue) continue;

                    dist[nx, ny] = dist[p.x, p.y] + 1;
                    frontier.Enqueue(new Vector2Int(nx, ny));
                }
            }

            // ── 換算成權重 ──
            float coastCells = Mathf.Max(1f, s.coastRange / 100f * R);
            for (int y = 0; y < R; y++)
                for (int x = 0; x < R; x++)
                {
                    if (su.water[x, y]) { su.weight[x, y] = 0f; continue; }

                    // 貼著水的那一圈也不放 —— 節點會有一半泡在水裡
                    if (dist[x, y] <= 1) { su.weight[x, y] = 0f; continue; }

                    float d = dist[x, y];
                    float near = Mathf.Clamp01(1f - (d - 2f) / coastCells);   // 1 = 緊鄰海岸
                    su.weight[x, y] = Mathf.Lerp(1f, s.coastBias, near);
                }

            return su;
        }

        // ==========================================
        // 取樣：best-candidate（藍雜訊）
        // ==========================================

        /// <summary>
        /// 每放一個點，先隨機丟 K 個候選，挑「離已放的點最遠 × 地形分數最高」那一個。
        ///
        /// 【為什麼不是純隨機】純隨機會結塊 —— 那就是「擠在一起」的來源。
        /// 隨機分佈本來就會有空洞和團塊，人眼一看就覺得亂而不是自然。
        ///
        /// 【為什麼不是 Poisson disc】Poisson 要指定固定半徑，
        /// 但我們要的密度隨地形變（海岸密、內陸疏）。best-candidate
        /// 用「離最近鄰居的距離」當分數，密度自然跟著權重走，而且
        /// 節點數是我們說了算 —— Poisson 是生到滿為止，數量不好控。
        ///
        /// 【K 越大越均勻】K=1 等於純隨機，K→∞ 逼近完美藍雜訊。
        /// 24 已經看不太出結塊了。
        /// </summary>
        private static List<Vector2> SampleTerrain(
            Suitability su, MapGenerationSettings s, System.Random rng, int count)
        {
            var picked = new List<Vector2>();
            int K = Mathf.Max(1, s.candidatesPerNode);

            float marginX = s.horizontalMargin * 0.5f;
            float marginY = 6f;

            for (int n = 0; n < count; n++)
            {
                Vector2 best = Vector2.zero;
                float bestScore = -1f;

                for (int k = 0; k < K; k++)
                {
                    var c = new Vector2(
                        Mathf.Lerp(marginX, 100f - marginX, (float)rng.NextDouble()),
                        Mathf.Lerp(marginY, 100f - marginY, (float)rng.NextDouble()));

                    float w = su.At(c);
                    if (w <= 0f) continue;              // 粗篩：格子是水

                    // ⚠️ 粗篩不夠。適宜度圖只有 128x128，一格代表原圖 16x16 像素，
                    //    平均起來是陸地、實際落點卻可能正好在河道上。
                    //    最後一定要拿原圖再驗一次
                    if (IsWaterExact(s, c)) continue;

                    // 離最近的已放點多遠。第一個點沒有鄰居，給一個大值
                    float nearest = float.MaxValue;
                    for (int i = 0; i < picked.Count; i++)
                    {
                        float d = (picked[i] - c).sqrMagnitude;
                        if (d < nearest) nearest = d;
                    }
                    if (picked.Count == 0) nearest = 10000f;

                    float score = Mathf.Sqrt(nearest) * w;
                    if (score <= bestScore) continue;

                    bestScore = score;
                    best = c;
                }

                // K 個候選全在水裡 —— 放寬到「只要不是水就好」再試一輪
                if (bestScore < 0f)
                {
                    for (int k = 0; k < K * 4 && bestScore < 0f; k++)
                    {
                        var c = new Vector2(
                            Mathf.Lerp(marginX, 100f - marginX, (float)rng.NextDouble()),
                            Mathf.Lerp(marginY, 100f - marginY, (float)rng.NextDouble()));
                        if (su.At(c) <= 0f) continue;
                        if (IsWaterExact(s, c)) continue;
                        best = c; bestScore = 0f;
                    }
                }

                if (bestScore < 0f) break;    // 真的放不下了，少幾個節點也比放進海裡好
                picked.Add(best);
            }

            return picked;
        }

        /// <summary>拿原圖精確判斷這個點是不是水。粗篩之後的最後一道關。</summary>
        private static bool IsWaterExact(MapGenerationSettings s, Vector2 pct)
        {
            if (s.terrainMap == null || !s.terrainMap.isReadable) return false;

            Color c = s.terrainMap.GetPixelBilinear(pct.x / 100f, pct.y / 100f);
            float lum = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
            return lum < s.waterThreshold;
        }

        // ==========================================
        // 分層：依「離起點多遠」而不是格線
        // ==========================================

        /// <summary>
        /// 用**三角圖上的 BFS 跳數**當層數，而不是直線距離。
        ///
        /// 【為什麼這件事很關鍵】前一版照直線距離分桶，結果一個節點的
        /// 三角化鄰居常常全部落在同一桶 —— 那個節點就沒有「往前」的邊，
        /// 只好補一條最近的，而補出來的線不在三角圖上，**平面性就破了**。
        /// 實測：三角化本身 0 交叉，補完洞變成每張圖 3.5 處。
        ///
        /// 改用 BFS 跳數之後這在定義上不可能發生：跳數 k 的節點一定有一個
        /// 鄰居跳數是 k-1（BFS 就是這樣走到它的）。所以連線只留「跳數差 1」，
        /// **完全不需要補洞，也就完全不會交叉**。
        ///
        /// 【一場 run 有多長】= 最大跳數。節點越多跳數越多，
        /// 所以節點數就是用來逼近 mapLayers 的旋鈕。
        /// </summary>
        private static int[] LayersByHops(List<Vector2> pts, List<List<int>> adj, out int maxHop)
        {
            int n = pts.Count;
            var hop = new int[n];
            for (int i = 0; i < n; i++) hop[i] = -1;

            int start = 0;
            for (int i = 1; i < n; i++) if (pts[i].y < pts[start].y) start = i;

            var q = new Queue<int>();
            hop[start] = 0;
            q.Enqueue(start);
            maxHop = 0;

            while (q.Count > 0)
            {
                int cur = q.Dequeue();
                for (int k = 0; k < adj[cur].Count; k++)
                {
                    int nb = adj[cur][k];
                    if (hop[nb] >= 0) continue;
                    hop[nb] = hop[cur] + 1;
                    if (hop[nb] > maxHop) maxHop = hop[nb];
                    q.Enqueue(nb);
                }
            }

            // ⚠️ 這裡**故意不補** hop < 0。濾掉長邊之後圖可能斷開，
            //    -1 就是「連不到起點」的記號，呼叫端要靠它把那些點丟掉
            return hop;
        }

        // ==========================================
        // 連線：Delaunay 三角化
        // ==========================================

        /// <summary>
        /// Bowyer–Watson 增量三角化。
        ///
        /// 【為什麼是它】Delaunay 三角化是**平面圖** —— 邊除了端點以外不會相交。
        /// 前一版花了三輪在修交叉，這裡是從根本上不可能交叉。
        /// 而且它連的是「幾何上真正相鄰」的點，那正是玩家心裡的「下一站」。
        /// </summary>
        private static List<int[]> Delaunay(List<Vector2> pts)
        {
            var tris = new List<int[]>();
            int n = pts.Count;
            if (n < 3) return tris;

            var work = new List<Vector2>(pts);
            work.Add(new Vector2(-1000f, -1000f));
            work.Add(new Vector2(3000f, -1000f));
            work.Add(new Vector2(-1000f, 3000f));
            tris.Add(new int[] { n, n + 1, n + 2 });

            for (int i = 0; i < n; i++)
            {
                var bad = new List<int[]>();
                for (int t = 0; t < tris.Count; t++)
                    if (InCircumcircle(work[i], work[tris[t][0]], work[tris[t][1]], work[tris[t][2]]))
                        bad.Add(tris[t]);

                var edges = new List<int[]>();
                for (int b = 0; b < bad.Count; b++)
                {
                    int[] tr = bad[b];
                    AddEdge(edges, tr[0], tr[1]);
                    AddEdge(edges, tr[1], tr[2]);
                    AddEdge(edges, tr[2], tr[0]);
                }
                for (int b = 0; b < bad.Count; b++) tris.Remove(bad[b]);

                for (int e = 0; e < edges.Count; e++)
                    if (edges[e] != null) tris.Add(new int[] { edges[e][0], edges[e][1], i });
            }

            var keep = new List<int[]>();
            for (int t = 0; t < tris.Count; t++)
            {
                int[] tr = tris[t];
                if (tr[0] >= n || tr[1] >= n || tr[2] >= n) continue;
                keep.Add(tr);
            }
            return keep;
        }

        /// 邊出現第二次就把兩次都作廢 —— 剩下的就是壞三角形的邊界
        private static void AddEdge(List<int[]> edges, int a, int b)
        {
            for (int i = 0; i < edges.Count; i++)
            {
                if (edges[i] == null) continue;
                if ((edges[i][0] == a && edges[i][1] == b) || (edges[i][0] == b && edges[i][1] == a))
                {
                    edges[i] = null;
                    return;
                }
            }
            edges.Add(new int[] { a, b });
        }

        private static bool InCircumcircle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float ax = a.x - p.x, ay = a.y - p.y;
            float bx = b.x - p.x, by = b.y - p.y;
            float cx = c.x - p.x, cy = c.y - p.y;

            float det =
                (ax * ax + ay * ay) * (bx * cy - cx * by)
              - (bx * bx + by * by) * (ax * cy - cx * ay)
              + (cx * cx + cy * cy) * (ax * by - bx * ay);

            // 環繞方向決定符號，先判方向才不會被順逆時針影響
            float orient = (b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y);
            return orient > 0 ? det > 0 : det < 0;
        }

        // ==========================================
        // 入口
        // ==========================================

        /// <summary>
        /// 把長度超過 maxDist 的邊濾掉。
        ///
        /// Delaunay 的外圍邊（凸包附近）本來就會很長 —— 那些邊在畫面上
        /// 是橫跨半張圖的斜線，看起來像結構圖的輔助線而不是路。
        ///
        /// 副作用是好的：邊短了之後要走更多步才到得了對岸，
        /// **BFS 跳數自然變多**，也就是一場 run 變長。
        /// </summary>
        private static List<List<int>> FilterByLength(
            List<List<int>> adj, List<Vector2> pts, float maxDist)
        {
            var outAdj = new List<List<int>>();
            float max2 = maxDist * maxDist;

            for (int i = 0; i < adj.Count; i++)
            {
                var row = new List<int>();
                for (int k = 0; k < adj[i].Count; k++)
                {
                    int nb = adj[i][k];
                    if ((pts[nb] - pts[i]).sqrMagnitude > max2) continue;
                    row.Add(nb);
                }
                outAdj.Add(row);
            }
            return outAdj;
        }

        /// <summary>三角形清單 → 每個點的鄰居清單。分層與連線都靠它。</summary>
        private static List<List<int>> BuildAdjacency(List<int[]> tris, int n)
        {
            var adj = new List<List<int>>();
            for (int i = 0; i < n; i++) adj.Add(new List<int>());

            for (int t = 0; t < tris.Count; t++)
            {
                int[] tr = tris[t];
                for (int a = 0; a < 3; a++)
                    for (int b = 0; b < 3; b++)
                    {
                        if (a == b) continue;
                        if (!adj[tr[a]].Contains(tr[b])) adj[tr[a]].Add(tr[b]);
                    }
            }
            return adj;
        }

        private static MapData GenerateTerrain(MapGenerationSettings s, System.Random rng)
        {
            Suitability su = BuildSuitability(s);
            if (su == null) return GenerateOrganic(s, rng);   // 底圖有問題就退回上一版

            int depths = Mathf.Max(3, s.mapLayers);
            // ⚠️ 節點數決定圖的**直徑**，而直徑就是一場 run 有多長。
            //    Delaunay 圖的直徑大約是 sqrt(N)，所以要 8 層得要 N≈50 上下。
            //    玩家只會走過其中一條路徑（約 mapLayers 站），其餘是沒選的岔路
            int count = Mathf.Clamp(depths * depths / 2 + depths, depths * 2, 60);

            List<Vector2> pts = SampleTerrain(su, s, rng, count);
            if (pts.Count < 3) return GenerateOrganic(s, rng);

            // ⚠️ 先三角化，再用圖上的跳數分層。順序不能反 —— 見 LayersByHops
            List<int[]> tris = Delaunay(pts);
            List<List<int>> adjAll = BuildAdjacency(tris, pts.Count);

            // ⚠️ **把太長的邊濾掉**，這一步同時解兩件事：
            //    ① 三角化的外圍邊本來就很長，畫出來是橫跨半張圖的斜線
            //    ② 邊短了之後要走更多步才到得了對岸 → BFS 跳數變多 → 層數接近 mapLayers
            //    不濾的話 40 個節點只會有 4 層，每層 10 個，「下一層」的鄰居隔半張圖
            List<List<int>> adj = FilterByLength(adjAll, pts, s.maxLinkDistance);

            int maxHop;
            int[] hop = LayersByHops(pts, adj, out maxHop);

            // 濾完之後圖可能斷開 —— 連不到起點的那些點直接不要，
            // 硬留著就是玩家永遠走不到的節點
            var remap = new int[pts.Count];
            for (int i = 0; i < pts.Count; i++) remap[i] = -1;

            var map = new MapData();
            var nodes = new List<RunNodeData>();
            var keptPts = new List<Vector2>();
            var keptLayer = new List<int>();

            for (int i = 0; i < pts.Count; i++)
            {
                if (hop[i] < 0) continue;

                remap[i] = nodes.Count;
                keptPts.Add(pts[i]);
                keptLayer.Add(hop[i]);

                var d = new RunNodeData
                {
                    nodeId = "Node_" + hop[i] + "_" + i,
                    kind = PickKind(s, rng, hop[i], maxHop + 1),
                    layer = hop[i],
                    dressingSeed = rng.Next(),
                    xPercent = pts[i].x,
                    yPercent = pts[i].y,
                };
                nodes.Add(d);
                map.allNodes.Add(d);
            }

            if (nodes.Count < 3) return GenerateOrganic(s, rng);

            // 鄰接表換成新的索引
            var adjK = new List<List<int>>();
            for (int i = 0; i < nodes.Count; i++) adjK.Add(new List<int>());
            for (int i = 0; i < pts.Count; i++)
            {
                if (remap[i] < 0) continue;
                for (int k = 0; k < adj[i].Count; k++)
                {
                    int nb = adj[i][k];
                    if (remap[nb] < 0) continue;
                    adjK[remap[i]].Add(remap[nb]);
                }
            }

            pts = keptPts;
            adj = adjK;
            int[] layer = keptLayer.ToArray();

            // 只留「跳數差剛好 1」的三角化邊。因為層數就是 BFS 跳數，
            // 每個節點必然有前有後 —— 不需要補洞，平面性完整保留
            if (s.linkMode == TerrainLinkMode.SpanningTree)
            {
                // 先只連必要的，再依機率加回 —— 順序與 Triangulation 相反，見那一支的說明
                LinkSpanningTree(nodes, pts, layer, adj, maxHop, s, rng);

                // ⚠️ 這一行不能省。主幹只保證「有比自己深的鄰居就連過去」，
                //    但 BFS 樹的葉子整圈鄰居都不比自己深 —— 那就是死路。
                //    實測少了這一行有 1046 個死路節點
                EnsureReachesGoal(nodes, layer, adj, maxHop);
            }
            else
            {
                var seen = new HashSet<long>();
                for (int t = 0; t < tris.Count; t++)
                {
                    int[] tr = tris[t];
                    HopLink(nodes, layer, tr[0], tr[1], seen);
                    HopLink(nodes, layer, tr[1], tr[2], seen);
                    HopLink(nodes, layer, tr[2], tr[0], seen);
                }

                PruneLinks(nodes, s.maxForwardLinks);
                EnsureReachesGoal(nodes, layer, adj, maxHop);
            }
            return map;
        }

        /// <summary>
        /// 讓每個節點都走得到終點。
        ///
        /// 【為什麼需要】BFS 保證「跳數 k 的節點有一個 k-1 的鄰居」，
        /// 但**不保證有 k+1 的鄰居** —— BFS 樹的葉子就沒有。
        /// 那種節點玩家走進去就出不來了。實測 200 張圖有 405 個。
        ///
        /// 【為什麼不會產生迴圈】只往「已經確定走得到終點」的節點連。
        /// 目標本身能到終點，所以連過去之後也能到，而且不可能繞回來。
        ///
        /// 【為什麼只連三角化的鄰居】保住平面性。隨便連最近的就會交叉 ——
        /// 那正是上一版每張圖 3.5 處交叉的原因。
        /// </summary>
        private static void EnsureReachesGoal(
            List<RunNodeData> nodes, int[] layer, List<List<int>> adj, int maxHop)
        {
            int n = nodes.Count;
            var reaches = new bool[n];

            for (int i = 0; i < n; i++) if (layer[i] == maxHop) reaches[i] = true;

            // 由深往淺傳播：只要有一個出邊通到「能到終點」的，自己就能到
            bool changed = true;
            int guard = 0;
            while (changed && guard++ < 64)
            {
                changed = false;
                for (int i = 0; i < n; i++)
                {
                    if (reaches[i]) continue;
                    for (int k = 0; k < nodes[i].nextNodeIds.Count; k++)
                    {
                        int idx = IndexOfId(nodes, nodes[i].nextNodeIds[k]);
                        if (idx < 0 || !reaches[idx]) continue;
                        reaches[i] = true; changed = true; break;
                    }
                }
            }

            // 補邊：到不了的，往鄰居裡「能到終點且跳數最大」的那一個連
            changed = true;
            guard = 0;
            while (changed && guard++ < 64)
            {
                changed = false;
                for (int i = 0; i < n; i++)
                {
                    if (reaches[i]) continue;

                    int best = -1;
                    for (int k = 0; k < adj[i].Count; k++)
                    {
                        int nb = adj[i][k];
                        if (!reaches[nb]) continue;
                        if (best < 0 || layer[nb] > layer[best]) best = nb;
                    }

                    if (best < 0) continue;
                    if (!nodes[i].nextNodeIds.Contains(nodes[best].nodeId))
                        nodes[i].nextNodeIds.Add(nodes[best].nodeId);

                    reaches[i] = true;
                    changed = true;
                }
            }
        }

        /// <summary>
        /// 每個節點最多留 `keep` 條出邊，優先留短的。
        ///
        /// 【為什麼要剪】Delaunay 每個點平均有 6 個鄰居，過濾成「往前」之後
        /// 還有 2~3 條。全部畫出來是一張三角網，看起來像結構圖不像地圖上的路。
        ///
        /// 【為什麼優先留短的】短邊是「隔壁那一站」，長邊是「橫跨半張圖」。
        /// 留短的路網才像真的走出來的路。
        ///
        /// ⚠️ **不能剪掉目標的唯一入邊** —— 那個節點就變成永遠走不到了。
        /// </summary>
        private static void PruneLinks(List<RunNodeData> nodes, int keep)
        {
            if (keep < 1) return;

            // 先數每個節點有幾條入邊
            var inCount = new Dictionary<string, int>();
            for (int i = 0; i < nodes.Count; i++)
                for (int k = 0; k < nodes[i].nextNodeIds.Count; k++)
                {
                    string id = nodes[i].nextNodeIds[k];
                    inCount[id] = (inCount.ContainsKey(id) ? inCount[id] : 0) + 1;
                }

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].nextNodeIds.Count <= keep) continue;

                RunNodeData from = nodes[i];
                var ids = new List<string>(from.nextNodeIds);

                ids.Sort(delegate (string x, string y)
                {
                    return DistTo(nodes, from, x).CompareTo(DistTo(nodes, from, y));
                });

                for (int k = ids.Count - 1; k >= keep; k--)
                {
                    string id = ids[k];
                    if (inCount[id] <= 1) continue;      // 這是它唯一的入邊，不能剪

                    from.nextNodeIds.Remove(id);
                    inCount[id] = inCount[id] - 1;
                }
            }
        }

        private static float DistTo(List<RunNodeData> nodes, RunNodeData from, string id)
        {
            int idx = IndexOfId(nodes, id);
            if (idx < 0) return float.MaxValue;

            float dx = nodes[idx].xPercent - from.xPercent;
            float dy = nodes[idx].yPercent - from.yPercent;
            return dx * dx + dy * dy;
        }

        private static int IndexOfId(List<RunNodeData> nodes, string id)
        {
            for (int i = 0; i < nodes.Count; i++) if (nodes[i].nodeId == id) return i;
            return -1;
        }

        /// <summary>
        /// 【SpanningTree】稀疏連法：每個節點只保證**一條**主幹出邊，其餘依機率加回。
        ///
        /// 【為什麼要有這一版】三角化把「跳數差 1」的邊全留，40 個節點會有 69 條線，
        /// 畫出來是一張三角網 —— 像結構圖不像地圖上的路。
        ///
        /// 【為什麼先前的「事後剪枝」行不通】剪枝要保護「別人唯一的入邊」，
        /// 而大多數節點本來就只有一條入邊，於是幾乎剪不動（實測 68 → 69，等於沒剪）。
        /// **順序要反過來**：先只連必要的，再依機率加回 —— 這一版就是那個順序。
        ///
        /// 三步，缺一不可：
        ///   ① 主幹：每個非終點節點挑一條「跳數 +1 且最近」的出邊 → 不會有死路
        ///   ② 補入邊：沒有人連進來的節點，從跳數 -1 的最近鄰居拉一條 → 不會走不到
        ///   ③ 額外：其餘的邊依 extraLinkChance 加回 → 這才是「選擇」
        ///
        /// 三步都只用 Delaunay 的邊，所以平面性完整保留、連線不會交叉。
        /// </summary>
        private static void LinkSpanningTree(
            List<RunNodeData> nodes, List<Vector2> pts, int[] layer, List<List<int>> adj,
            int maxHop, MapGenerationSettings s, System.Random rng)
        {
            int n = nodes.Count;

            // ── ① 主幹 ──
            for (int i = 0; i < n; i++)
            {
                if (layer[i] >= maxHop) continue;

                int best = NearestNeighbourAtLayer(pts, adj, layer, i, layer[i] + 1);

                // 沒有 +1 的鄰居（BFS 樹的葉子）→ 挑跳數最大的那個鄰居
                if (best < 0)
                    for (int k = 0; k < adj[i].Count; k++)
                    {
                        int nb = adj[i][k];
                        if (layer[nb] <= layer[i]) continue;
                        if (best < 0 || layer[nb] > layer[best]) best = nb;
                    }

                if (best >= 0) nodes[i].nextNodeIds.Add(nodes[best].nodeId);
            }

            // ── ② 補入邊 ──
            var hasIn = new bool[n];
            for (int i = 0; i < n; i++)
                for (int k = 0; k < nodes[i].nextNodeIds.Count; k++)
                {
                    int idx = IndexOfId(nodes, nodes[i].nextNodeIds[k]);
                    if (idx >= 0) hasIn[idx] = true;
                }

            for (int i = 0; i < n; i++)
            {
                if (layer[i] == 0 || hasIn[i]) continue;

                int from = NearestNeighbourAtLayer(pts, adj, layer, i, layer[i] - 1);
                if (from < 0) continue;

                if (!nodes[from].nextNodeIds.Contains(nodes[i].nodeId))
                {
                    nodes[from].nextNodeIds.Add(nodes[i].nodeId);
                    hasIn[i] = true;
                }
            }

            // ── ③ 額外的岔路 ──
            if (s.extraLinkChance <= 0f) return;

            for (int i = 0; i < n; i++)
            {
                if (nodes[i].nextNodeIds.Count >= s.maxForwardLinks) continue;

                for (int k = 0; k < adj[i].Count; k++)
                {
                    int nb = adj[i][k];
                    if (layer[nb] - layer[i] != 1) continue;
                    if (nodes[i].nextNodeIds.Contains(nodes[nb].nodeId)) continue;
                    if (rng.NextDouble() > s.extraLinkChance) continue;

                    nodes[i].nextNodeIds.Add(nodes[nb].nodeId);
                    if (nodes[i].nextNodeIds.Count >= s.maxForwardLinks) break;
                }
            }
        }

        /// <summary>adj[from] 裡跳數剛好是 wantLayer 的鄰居中，距離最近的那一個。</summary>
        private static int NearestNeighbourAtLayer(
            List<Vector2> pts, List<List<int>> adj, int[] layer, int from, int wantLayer)
        {
            int best = -1; float bd = float.MaxValue;
            for (int k = 0; k < adj[from].Count; k++)
            {
                int nb = adj[from][k];
                if (layer[nb] != wantLayer) continue;

                float d = (pts[nb] - pts[from]).sqrMagnitude;
                if (d >= bd) continue;
                bd = d; best = nb;
            }
            return best;
        }

        private static void HopLink(
            List<RunNodeData> nodes, int[] layer, int a, int b, HashSet<long> seen)
        {
            int lo = layer[a] <= layer[b] ? a : b;
            int hi = lo == a ? b : a;

            if (layer[hi] - layer[lo] != 1) return;   // 同層或跳太多都不是「下一站」

            long key = (long)lo * 100000 + hi;
            if (!seen.Add(key)) return;

            nodes[lo].nextNodeIds.Add(nodes[hi].nodeId);
        }
    }
}
