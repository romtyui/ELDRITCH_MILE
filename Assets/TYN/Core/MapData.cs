using System;
using System.Collections.Generic;

namespace EldritchMile.Core
{
    /// <summary>
    /// 節點類型。取代舊 MapNodeExplore 的 targetSceneName 字串驅動 ——
    /// 現在只有一個場景，改由類型決定要載入哪個 Stage prefab。
    /// </summary>
    /// <summary>
    /// 地圖節點的類型。
    ///
    /// ⚠️ **只能往後追加，不能插入或重排。** 序列化存的是整數，
    /// 動了順序等於把地圖上所有既有節點改成別的類型，而且不會有任何錯誤訊息。
    /// </summary>
    public enum MapNodeKind
    {
        Event = 0,
        Combat = 1,
        Boss = 2,
        Shop = 3,
        SpecialEvent = 4,

        /// <summary>C18/Phase 6：與角色對話。2026-08-16 追加</summary>
        Dialogue = 5,
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

        /// <summary>
        /// 這一站的戰鬥要打誰（`EnemyData.enemyId`）。只有戰鬥／Boss 節點會有值。
        ///
        /// 【為什麼存在節點上，不是打的時候才抽】
        /// 「保證出現半魚人祭司」這種需求，必須在**看得到整張地圖**的時候安排，
        /// 否則只能寄望隨機。而且存在節點上還有一個好處：
        /// **同一個節點重進是同一隻怪** —— 現場才抽的話玩家離開再進來就換人了。
        ///
        /// 由 <see cref="EncounterPlanner"/> 在地圖生成後填一次。
        /// 留空 = 交給戰鬥組自己的抽怪邏輯。
        /// </summary>
        public string enemyId;

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
