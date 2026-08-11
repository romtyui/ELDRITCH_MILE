using System;
using System.Collections.Generic;

namespace EldritchMile.Core
{
    /// <summary>
    /// 節點類型。取代舊 MapNodeExplore 的 targetSceneName 字串驅動 ——
    /// 現在只有一個場景，改由類型決定要載入哪個 Stage prefab。
    /// </summary>
    public enum MapNodeKind
    {
        Event,
        Combat,
        Boss,
        Shop,
        SpecialEvent,
    }

    /// <summary>
    /// 單一地圖節點的執行期資料。純資料，不含任何 UI 或場景引用。
    /// </summary>
    [Serializable]
    public class RunNodeData
    {
        public string nodeId;
        public MapNodeKind kind = MapNodeKind.Event;

        public int layer;
        public float xPercent;
        public float yPercent;

        public List<string> nextNodeIds = new List<string>();

        /// 房間內容的資料 id。Phase 3 接上實際的房間資料庫。
        public string contentId;

        public bool visited;
    }

    /// <summary>
    /// 一整張地圖的拓撲與進度。
    ///
    /// 【重要】此類別刻意不含任何 MonoBehaviour 或 UI 引用 ——
    /// 舊版把 MapData 放在 PerspectiveMapGenerator 裡面，導致地圖 UI 一被停用，
    /// 整場 run 的進度就消失。這是初版最主要的架構病根（設計文件 §3 病根 1）。
    /// </summary>
    [Serializable]
    public class MapData
    {
        public List<RunNodeData> allNodes = new List<RunNodeData>();
        public string currentNodeId = "";
        public List<string> historyNodeIds = new List<string>();

        public RunNodeData GetNode(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            for (int i = 0; i < allNodes.Count; i++)
            {
                if (allNodes[i].nodeId == id) return allNodes[i];
            }
            return null;
        }

        public RunNodeData CurrentNode => GetNode(currentNodeId);

        /// <summary>
        /// 地圖的最大層數。
        ///
        /// 【修正舊 bug】舊版 PerspectiveMapGenerator 判斷 Boss 節點時寫的是
        ///   currentNode.layer == allNodes.Count - 1
        /// 但 allNodes.Count 是「節點總數」(例如 12)，不是「層數」(例如 5)，
        /// 兩者根本不同量級，導致 Boss 判定幾乎永遠為 false。
        /// </summary>
        public int MaxLayer
        {
            get
            {
                int max = 0;
                for (int i = 0; i < allNodes.Count; i++)
                {
                    if (allNodes[i].layer > max) max = allNodes[i].layer;
                }
                return max;
            }
        }

        public bool IsFinalLayer(RunNodeData node)
        {
            return node != null && node.layer >= MaxLayer;
        }

        /// 從目前位置可以前往的節點。currentNodeId 為空時代表尚未出發，回傳第 0 層。
        public List<RunNodeData> GetReachableNodes()
        {
            var result = new List<RunNodeData>();

            RunNodeData current = CurrentNode;
            if (current == null)
            {
                for (int i = 0; i < allNodes.Count; i++)
                {
                    if (allNodes[i].layer == 0) result.Add(allNodes[i]);
                }
                return result;
            }

            for (int i = 0; i < current.nextNodeIds.Count; i++)
            {
                RunNodeData next = GetNode(current.nextNodeIds[i]);
                if (next != null) result.Add(next);
            }
            return result;
        }

        public void MoveTo(string nodeId)
        {
            if (!string.IsNullOrEmpty(currentNodeId))
            {
                historyNodeIds.Add(currentNodeId);
            }

            currentNodeId = nodeId;

            RunNodeData node = GetNode(nodeId);
            if (node != null) node.visited = true;
        }
    }
}
