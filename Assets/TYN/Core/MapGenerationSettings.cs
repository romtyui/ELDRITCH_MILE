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
        [Header("結構")]
        [Tooltip("總層數，含起點層與最後的 Boss 層")]
        [Range(2, 12)] public int mapLayers = 5;

        [Tooltip("第 0 層（起點）的節點數")]
        [Range(1, 5)] public int startNodeCount = 2;

        [Tooltip("中間層的節點數下限")]
        [Range(1, 5)] public int midLayerMin = 2;

        [Tooltip("中間層的節點數上限")]
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
