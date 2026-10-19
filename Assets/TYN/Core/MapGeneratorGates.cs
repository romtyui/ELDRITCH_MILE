using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 岔路通往其他地區：在地圖上放「通道」節點，以及穿越後落在新地圖的哪裡。（2026-10-18）
    ///
    /// ────────────────────────────────────────────────────────
    /// 【放在哪】面向目標地區的那一邊（見 <see cref="RegionData"/> 的說明）：
    ///   目標在西 → 西緣、目標在東 → 東緣、目標在北 → 最深幾層（首領那一頭）。
    /// 層數只挑中段（<see cref="GateLayerMin"/>~<see cref="GateLayerMax"/>），
    /// 太早出現等於這一區沒玩到，太晚就跟首領擠在一起。
    ///
    /// 【只改類型、不動連線】跟 EnsureGuaranteedKinds 同一個做法 —— 挑一個探索節點改成通道，
    /// `nextNodeIds` 原封不動。所以：
    ///   · 每條路照樣走得到首領（「至少一條路通首領」自動成立，不用另外檢查）
    ///   · 通道「還沒做好」時（RegionData.travelReady = false），播完提示照樣能從它往下走，不會卡死
    ///
    /// 【挑哪些節點】探索房優先（跟 EnsureGuaranteedKinds 同一個理由：填充用的，少一間最不痛），
    /// 不夠才用一般戰。商店、對話、特殊不動 —— 改掉的話那張圖可能就沒有了。
    ///
    /// 【硬條件】（2026-10-19 試玩：可選的兩站都是通道）
    ///   ① 落在那一側最外面 30% 的帶子裡（西的在西、東的在東，兩個通道自然隔開）
    ///   ② 不跟別的通道互連、不共用父節點（玩家不會同時看到兩個通道）
    ///   ③ 每一個通往它的節點都還有一條不是通道的路（不會「只能走通道」）
    /// 找不到合格的就**這一場不放**，不退而求其次。
    /// </summary>
    public static partial class MapGenerator
    {
        /// <summary>通道最早出現在第幾層（佔全圖層數的比例）。</summary>
        public const float GateLayerMin = 0.35f;

        /// <summary>通道最晚出現在第幾層（佔全圖層數的比例）。</summary>
        public const float GateLayerMax = 0.8f;

        /// <summary>
        /// 依 <paramref name="region"/> 的出口清單，把部分探索節點改成通道。回傳放了幾個。
        /// **要在 EncounterPlanner 之前呼叫**（通道不該被排到敵人）。
        /// </summary>
        public static int PlaceGates(MapData map, RegionData region, System.Random rng)
        {
            if (map == null || region == null || region.exits == null || rng == null) return 0;

            int maxLayer = map.MaxLayer;
            if (maxLayer < 3) return 0;   // 太淺的圖沒有「中段」

            // 誰連到誰（反查父節點用）
            var parents = new Dictionary<string, List<RunNodeData>>();
            float minX = float.MaxValue, maxX = float.MinValue;
            for (int i = 0; i < map.allNodes.Count; i++)
            {
                RunNodeData n = map.allNodes[i];
                if (n == null) continue;
                minX = Mathf.Min(minX, n.xPercent); maxX = Mathf.Max(maxX, n.xPercent);
                for (int k = 0; k < n.nextNodeIds.Count; k++)
                {
                    List<RunNodeData> list;
                    if (!parents.TryGetValue(n.nextNodeIds[k], out list)) parents[n.nextNodeIds[k]] = list = new List<RunNodeData>();
                    list.Add(n);
                }
            }
            float span = Mathf.Max(1f, maxX - minX);

            int placed = 0;
            for (int e = 0; e < region.exits.Count; e++)
            {
                RegionExit exit = region.exits[e];
                if (exit == null || exit.target == null || exit.target == region) continue;

                // ⚠️ 每條出口都擲一次，**不管前一條有沒有放成** —— 不然改一條的機率會連帶改掉後面所有的結果
                double roll = rng.NextDouble();
                if (roll >= exit.chance) continue;

                Vector2Int dir = exit.target.DirectionFrom(region);
                if (dir.y < 0 && dir.x == 0)
                {
                    // 往南 = 往回走。目前不能回頭（地圖上寫著 NO RETURN）
                    Debug.LogWarning($"[地圖生成] 「{region.regionId}」→「{exit.target.regionId}」在正南方，不能回頭，不放這條通道");
                    continue;
                }

                RunNodeData best = null;
                float bestScore = float.MaxValue;

                for (int i = 0; i < map.allNodes.Count; i++)
                {
                    RunNodeData n = map.allNodes[i];
                    if (n == null) continue;

                    // 只改探索房（優先）或沒有指定對手的一般戰 —— 理由見類別說明
                    if (n.kind != MapNodeKind.Event && n.kind != MapNodeKind.Combat) continue;

                    float t = (float)n.layer / maxLayer;
                    if (t < GateLayerMin || t > GateLayerMax) continue;

                    // ① 一定要在那一側的外緣帶裡（左右各 GateEdgeBand）。
                    //    以前只挑「最靠邊的」，地圖在那幾層剛好很窄時，西邊的通道會落在東半邊
                    float rel = (n.xPercent - minX) / span;
                    if (dir.x < 0 && rel > GateEdgeBand) continue;
                    if (dir.x > 0 && rel < 1f - GateEdgeBand) continue;

                    // ② 不跟別的通道相鄰：不互連、不共用父節點
                    if (TouchesGate(map, n, parents)) continue;

                    // ③ 走到它的每一個父節點，都還要留一條**不是通道**的路 ——
                    //    不然玩家在那一站只能選通道（實測 500 張圖有 332 張出現這種節點）
                    if (!EveryParentKeepsAnotherWay(map, n, parents)) continue;

                    float score = EdgeDistance(n, dir, maxLayer);
                    if (n.kind != MapNodeKind.Event) score += 15f;   // 能改探索房就不要吃掉一場戰鬥
                    if (score < bestScore) { bestScore = score; best = n; }
                }

                if (best == null)
                {
                    Debug.LogWarning($"[地圖生成] 這張圖找不到合格的通道位置（→「{exit.target.regionId}」：" +
                                     "要在那一側的外緣、不跟別的通道相鄰、前一站還有別條路），這一場沒有這條通道");
                    continue;
                }

                best.kind = MapNodeKind.Gate;
                best.targetRegionId = exit.target.regionId;
                best.enemyId = null;
                placed++;

                Debug.Log($"[地圖生成] 通道 {best.nodeId}（第 {best.layer} 層、x {best.xPercent:0}）→「{exit.target.regionId}」");
            }
            return placed;
        }

        /// <summary>通道只能落在那一側最外面這麼寬的一帶（佔地圖左右寬度的比例）。</summary>
        public const float GateEdgeBand = 0.3f;

        /// <summary>n 跟任何一個通道互連、或跟某個通道共用父節點。</summary>
        private static bool TouchesGate(MapData map, RunNodeData n, Dictionary<string, List<RunNodeData>> parents)
        {
            for (int k = 0; k < n.nextNodeIds.Count; k++)
            {
                RunNodeData c = map.GetNode(n.nextNodeIds[k]);
                if (c != null && c.kind == MapNodeKind.Gate) return true;
            }

            List<RunNodeData> ps;
            if (!parents.TryGetValue(n.nodeId, out ps)) return false;
            for (int i = 0; i < ps.Count; i++)
            {
                if (ps[i].kind == MapNodeKind.Gate) return true;
                for (int k = 0; k < ps[i].nextNodeIds.Count; k++)
                {
                    RunNodeData sib = map.GetNode(ps[i].nextNodeIds[k]);
                    if (sib != null && sib != n && sib.kind == MapNodeKind.Gate) return true;
                }
            }
            return false;
        }

        /// <summary>把 n 改成通道之後，它的每一個父節點是不是都還有一條不是通道的路。沒有父節點（起點層）回 false。</summary>
        private static bool EveryParentKeepsAnotherWay(MapData map, RunNodeData n, Dictionary<string, List<RunNodeData>> parents)
        {
            List<RunNodeData> ps;
            if (!parents.TryGetValue(n.nodeId, out ps) || ps.Count == 0) return false;
            for (int i = 0; i < ps.Count; i++)
            {
                bool other = false;
                for (int k = 0; k < ps[i].nextNodeIds.Count && !other; k++)
                {
                    RunNodeData c = map.GetNode(ps[i].nextNodeIds[k]);
                    if (c != null && c != n && c.kind != MapNodeKind.Gate) other = true;
                }
                if (!other) return false;
            }
            return true;
        }

        /// <summary>
        /// 從 <paramref name="gate"/> 穿越過來，落在新地圖的哪個節點。
        ///
        /// 往西走 → 落在新地圖的東緣、往東 → 西緣，南北位置跟通道差不多（「對應位置」）。
        /// 往北走 → 從新地圖的起點開始，回傳 null。
        ///
        /// ⚠️ 不會落在首領或首領前一層 —— 穿越不是捷徑，新地區還是要走一段。
        /// </summary>
        public static RunNodeData FindArrival(MapData map, RunNodeData gate, MapData fromMap, Vector2Int dir)
        {
            if (map == null || gate == null) return null;
            if (dir.x == 0) return null;   // 正北：從起點開始

            int maxLayer = map.MaxLayer;
            float targetY = RelativeY(fromMap, gate.yPercent);

            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < map.allNodes.Count; i++)
            {
                RunNodeData n = map.allNodes[i];
                if (n == null) continue;
                minY = Mathf.Min(minY, n.yPercent);
                maxY = Mathf.Max(maxY, n.yPercent);
            }
            float wantY = Mathf.Lerp(minY, maxY, targetY);

            RunNodeData best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < map.allNodes.Count; i++)
            {
                RunNodeData n = map.allNodes[i];
                if (n == null || n.kind == MapNodeKind.Boss || n.kind == MapNodeKind.Gate) continue;
                if (n.layer < 1 || n.layer > maxLayer - 2) continue;
                if (n.nextNodeIds == null || n.nextNodeIds.Count == 0) continue;

                // 從反方向那一緣進來：往西走 → 東緣（x 大）
                float edge = dir.x < 0 ? 100f - n.xPercent : n.xPercent;
                float score = edge + Mathf.Abs(n.yPercent - wantY);

                // 落點會被標成「走過了」，所以盡量落在探索節點上 ——
                // 落在商店的話，那間店玩家一次都沒進去就算走過了
                if (n.kind != MapNodeKind.Event) score += 30f;
                if (score < bestScore) { bestScore = score; best = n; }
            }
            return best;
        }

        /// <summary>離目標那一緣多遠（越小越靠邊）。</summary>
        private static float EdgeDistance(RunNodeData n, Vector2Int dir, int maxLayer)
        {
            if (dir.x < 0) return n.xPercent;            // 西緣
            if (dir.x > 0) return 100f - n.xPercent;     // 東緣
            return (maxLayer - n.layer) * 100f;          // 北：越深越好
        }

        /// <summary>y 在這張地圖的南北範圍裡佔幾成（0 = 最南）。</summary>
        private static float RelativeY(MapData map, float y)
        {
            if (map == null) return 0.5f;
            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < map.allNodes.Count; i++)
            {
                RunNodeData n = map.allNodes[i];
                if (n == null) continue;
                minY = Mathf.Min(minY, n.yPercent);
                maxY = Mathf.Max(maxY, n.yPercent);
            }
            return maxY > minY ? Mathf.InverseLerp(minY, maxY, y) : 0.5f;
        }
    }
}
