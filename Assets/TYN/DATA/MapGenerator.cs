using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public int mapLayers     = 6;
    public int startNodeCount = 3; // 起始可選節點數 (1~4)
    public int seed          = -1; // -1 = 每局隨機

    private int nodeIdCounter;

    public List<MapNode> GenerateMap()
    {
        if (seed >= 0) Random.InitState(seed);

        nodeIdCounter = 0;
        var allNodes = new List<MapNode>();
        var layers   = new List<List<MapNode>>();

        // 1. 生成節點
        for (int i = 0; i < mapLayers; i++)
        {
            int count;
            if (i == 0)              count = Mathf.Clamp(startNodeCount, 1, 4);
            else if (i == mapLayers - 1) count = 1;
            else                     count = Random.Range(2, 5);

            float yPercent = 10f + 80f / (mapLayers - 1) * i;
            var layer = new List<MapNode>(count);

            for (int j = 0; j < count; j++)
            {
                float baseX = count == 1 ? 50f : 15f + 70f / (count - 1) * j;
                float jitter = count == 1 ? 0f : Random.Range(-4f, 4f);
                float x = Mathf.Clamp(baseX + jitter, 5f, 95f);

                var node = new MapNode($"node-{nodeIdCounter++}", i, GetRandomType(i), x, yPercent);
                layer.Add(node);
                allNodes.Add(node);
            }
            layers.Add(layer);
        }

        // 2. 連接層
        for (int i = 0; i < mapLayers - 1; i++)
        {
            var current = layers[i];
            var next    = layers[i + 1];

            foreach (var node in current)
            {
                var sorted      = next.OrderBy(n => Mathf.Abs(n.x - node.x)).ToList();
                int connections = (Random.value > 0.6f && sorted.Count > 1) ? 2 : 1;
                for (int c = 0; c < connections; c++)
                    Connect(node, sorted[c]);
            }

            // 確保下一層每個節點至少有一個父節點
            foreach (var orphan in next.Where(n => n.parents.Count == 0))
            {
                var nearest = current.OrderBy(n => Mathf.Abs(n.x - orphan.x)).First();
                Connect(nearest, orphan);
            }
        }

        return allNodes;
    }

    private static void Connect(MapNode parent, MapNode child)
    {
        if (!parent.children.Contains(child.id))
        {
            parent.children.Add(child.id);
            child.parents.Add(parent.id);
        }
    }

    private NodeType GetRandomType(int layer)
    {
        if (layer == 0)              return NodeType.Start;
        if (layer == mapLayers - 1)  return NodeType.Boss;

        float rand = Random.value;
        if (layer == 1) return rand > 0.5f ? NodeType.Combat : NodeType.Event;

        if (rand < 0.45f) return NodeType.Combat;
        if (rand < 0.65f) return NodeType.Event;
        if (rand < 0.80f) return NodeType.Elite;
        if (rand < 0.90f) return NodeType.Rest;
        return NodeType.Shop;
    }
}
