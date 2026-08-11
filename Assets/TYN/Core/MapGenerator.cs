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
        private static MapData GenerateProcedural(MapGenerationSettings s, System.Random rng)
        {
            var map = new MapData();
            int layerCount = Mathf.Max(2, s.mapLayers);

            // 每層節點數
            var layers = new List<List<RunNodeData>>();

            for (int layer = 0; layer < layerCount; layer++)
            {
                int count;
                if (layer == 0) count = Mathf.Max(1, s.startNodeCount);
                else if (layer == layerCount - 1) count = 1;                 // Boss 只有一個
                else count = rng.Next(s.midLayerMin, s.midLayerMax + 1);

                float yPercent = s.verticalMargin
                    + (100f - s.verticalMargin * 2f) / (layerCount - 1) * layer;

                var nodesInLayer = new List<RunNodeData>();

                for (int i = 0; i < count; i++)
                {
                    float baseX = count == 1
                        ? 50f
                        : s.horizontalMargin
                          + (100f - s.horizontalMargin * 2f) / (count - 1) * i;

                    float jitter = count == 1
                        ? 0f
                        : (float)(rng.NextDouble() * 2 - 1) * s.horizontalJitter;

                    var node = new RunNodeData
                    {
                        nodeId = $"Node_{layer}_{i}",
                        kind = PickKind(s, rng, layer, layerCount),
                        layer = layer,
                        xPercent = Mathf.Clamp(baseX + jitter, 8f, 92f),
                        yPercent = yPercent,
                    };

                    nodesInLayer.Add(node);
                    map.allNodes.Add(node);
                }

                layers.Add(nodesInLayer);
            }

            BuildConnections(layers, rng);
            return map;
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

        /// <summary>
        /// 連線：每個節點連到下一層 x 距離最近的 1~2 個；
        /// 之後補救沒有任何入邊的孤兒節點，確保每個節點都到得了。
        /// </summary>
        private static void BuildConnections(List<List<RunNodeData>> layers, System.Random rng)
        {
            for (int i = 0; i < layers.Count - 1; i++)
            {
                List<RunNodeData> current = layers[i];
                List<RunNodeData> next = layers[i + 1];

                foreach (RunNodeData node in current)
                {
                    // 依 x 距離排序（純數值，不碰 Transform）
                    next.Sort((a, b) =>
                        Mathf.Abs(a.xPercent - node.xPercent)
                            .CompareTo(Mathf.Abs(b.xPercent - node.xPercent)));

                    int connections = (next.Count > 1 && rng.NextDouble() > 0.5) ? 2 : 1;

                    for (int c = 0; c < connections && c < next.Count; c++)
                    {
                        Connect(node, next[c]);
                    }
                }

                // 補救孤兒：沒有任何上一層節點指向它
                foreach (RunNodeData orphan in next)
                {
                    bool hasIncoming = current.Exists(n => n.nextNodeIds.Contains(orphan.nodeId));
                    if (hasIncoming) continue;

                    current.Sort((a, b) =>
                        Mathf.Abs(a.xPercent - orphan.xPercent)
                            .CompareTo(Mathf.Abs(b.xPercent - orphan.xPercent)));

                    Connect(current[0], orphan);
                }
            }
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
