using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 地圖生成參數。做成 ScriptableObject 讓數值可在 Inspector 調，不必改程式。
    ///
    /// 【與舊版的差別】舊 PerspectiveMapGenerator 把這些參數跟 UI 引用、轉場邏輯
    /// 全混在同一個 MonoBehaviour 上，導致「調一個數值」要開場景。
    /// </summary>
    [CreateAssetMenu(fileName = "MapGenerationSettings", menuName = "Eldritch/Map Generation Settings")]
    public class MapGenerationSettings : ScriptableObject
    {
        [Header("結構（網格 ＋ 隨機遊走）")]
        [Tooltip("總層數，含起點層與最後的 Boss 層。**這就是一場 run 有多長**")]
        [Range(2, 16)] public int mapLayers = 8;

        [Tooltip("網格有幾欄。**這是畫面有多寬** ——\n" +
                 "欄數是「格子」不是「節點數」，實際每層有幾個節點由路徑走出來決定。\n" +
                 "欄少了路線會擠成一條，多了會變得鬆散、線很長")]
        [Range(3, 9)] public int gridColumns = 5;

        [Tooltip("走幾條路徑。**這是地圖有多密** ——\n" +
                 "路徑會自然合流與分岔，所以節點數不等於路徑數 × 層數。\n" +
                 "太少會變成幾條互不相干的線，太多會把整個網格填滿、又變回長條")]
        [Range(2, 8)] public int pathCount = 4;

        [Tooltip("第 0 層（起點）至少要有幾個不同的格子。\n" +
                 "前幾條路徑會被強制排到不同的起點欄，之後的隨機")]
        [Range(1, 5)] public int startNodeCount = 2;

        [Tooltip("⚠️ **新的網格演算法不使用這兩個欄位**（每層節點數由路徑決定）。\n" +
                 "留著只是為了不讓既有資產掉資料，日後確認沒人用可以移除")]
        [Range(1, 5)] public int midLayerMin = 2;
        [Range(1, 6)] public int midLayerMax = 3;

        [Header("節點類型機率（中間層）")]
        [Range(0f, 1f)] public float combatChance = 0.55f;
        [Range(0f, 1f)] public float shopChance = 0.15f;
        [Tooltip("C16：特殊事件，獲得神牌")]
        [Range(0f, 1f)] public float specialEventChance = 0.10f;
        // 其餘機率歸 Event

        [Header("版面")]
        [Tooltip("第一層與最後一層距離上下邊界的百分比")]
        [Range(0f, 30f)] public float verticalMargin = 10f;

        [Tooltip("節點水平分布的左右邊界百分比")]
        [Range(0f, 40f)] public float horizontalMargin = 20f;

        [Tooltip("節點水平位置的隨機抖動範圍（百分比）")]
        [Range(0f, 15f)] public float horizontalJitter = 5f;

        [Header("DEMO 路線")]
        [Tooltip("勾選後改用固定路線，忽略上方的隨機參數")]
        public bool useDemoRoute = false;

        [Tooltip("DEMO 路線每一層的節點類型，由起點排到 Boss")]
        public List<MapNodeKind> demoRouteKinds = new List<MapNodeKind>
        {
            MapNodeKind.Combat,
            MapNodeKind.Event,
            MapNodeKind.Combat,
            MapNodeKind.Boss,
        };
    }
}
