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
    /// 【為什麼只挑探索節點】跟 EnsureGuaranteedKinds 同一個理由：探索房是填充用的，少一間最不痛。
    /// 改到商店的話，那張圖可能就沒有商店了。
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
                    if (n == null || n.kind != MapNodeKind.Event) continue;

                    float t = (float)n.layer / maxLayer;
                    if (t < GateLayerMin || t > GateLayerMax) continue;

                    float score = EdgeDistance(n, dir, maxLayer);
                    if (score < bestScore) { bestScore = score; best = n; }
                }

                if (best == null)
                {
                    Debug.LogWarning($"[地圖生成] 找不到能改成通道的探索節點（→「{exit.target.regionId}」），這一場沒有這條通道");
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
