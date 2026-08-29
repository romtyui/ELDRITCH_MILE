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
        [Tooltip("C16：特殊事件，獲得神牌。\n\n" +
                 "⚠️ **首排已經由 First Layer Kind 保證有一次了**，這一格是「中段還會不會再長」。\n" +
                 "目前設 0 —— `Stage_SpecialEvent` 只有一份、兩張牌固定、也沒有 once 保護，\n" +
                 "再長出來玩家就是重看同一場祭壇戲。神牌內容變多了再調回來。")]
        [Range(0f, 1f)] public float specialEventChance = 0f;

        [Tooltip("與同行角色的機率卡牌對話（`dialogueNodeStage`）。\n\n" +
                 "⚠️ **這一格以前不存在，所以隨機地圖從來不會長出對話節點** ——\n" +
                 "只有 DEMO 的固定路線寫死了幾個，換成隨機生成就整個環節測不到了。\n" +
                 "那種漏法不會報錯，只會「怎麼玩都沒遇到對話」。")]
        [Range(0f, 1f)] public float dialogueChance = 0.15f;
        // 其餘機率歸 Event

        [Tooltip("**一定要出現至少一次**的節點類型。\n\n" +
                 "純機率的話 200 張圖裡會有 13% 完全沒有商店、6.5% 完全沒有對話 ——\n" +
                 "測試的人抽到那種圖就整個環節驗不到，而且不會知道是運氣問題。\n\n" +
                 "缺的話會挑一個**中段的探索節點改成它**：\n" +
                 "只改類型、不動任何連線，所以連通性不受影響。\n\n" +
                 "⚠️ 不必列 Boss 與神牌 —— 那兩個由層數固定，本來就一定有。\n" +
                 "要純隨機就把這個清單清空。")]
        public List<MapNodeKind> guaranteedKinds = new List<MapNodeKind>
        {
            MapNodeKind.Shop,
            MapNodeKind.Dialogue,
        };

        [Header("版面")]
        [Tooltip("第一層與最後一層距離上下邊界的百分比")]
        [Range(0f, 30f)] public float verticalMargin = 10f;

        [Tooltip("節點水平分布的左右邊界百分比")]
        [Range(0f, 40f)] public float horizontalMargin = 20f;

        [Tooltip("節點水平位置的隨機抖動範圍（百分比）")]
        [Range(0f, 15f)] public float horizontalJitter = 5f;

        [Header("首排")]
        [Tooltip("第 0 層固定放哪一種節點。\n\n" +
                 "**預設 SpecialEvent ＝ 一開場就挑神牌**（`Stage_SpecialEvent`）——\n" +
                 "神牌是主玩法，讓它在任何戰鬥之前拿到，測試才不會因為死在半路而卡住。\n\n" +
                 "⚠️ 隨機生成與 DEMO 的分支路線**都吃這一格**。\n" +
                 "改成 Event 就回到舊行為（開場是一間探索房）。")]
        public MapNodeKind firstLayerKind = MapNodeKind.SpecialEvent;

        [Header("DEMO 路線")]
        [Tooltip("勾選後改用固定路線，忽略上方的隨機參數")]
        public bool useDemoRoute = false;

        [Tooltip("固定路線長什麼樣。\n\n" +
                 "· **Straight** —— 一層一個節點的直線，讀 Demo Route Kinds。\n" +
                 "　最省事，但**沒有選擇** —— 玩家不會用到地圖，也驗不到連線。\n" +
                 "· **Branching** —— 一層可以有好幾個節點，讀 Demo Route Layers。\n" +
                 "　連線由程式算（見 MapGenerator.ConnectLayers），保證每個節點都走得到。")]
        public DemoRouteShape demoRouteShape = DemoRouteShape.Straight;

        [Tooltip("**Straight 用**：每一層一個節點，由起點排到 Boss")]
        public List<MapNodeKind> demoRouteKinds = new List<MapNodeKind>
        {
            MapNodeKind.Combat,
            MapNodeKind.Event,
            MapNodeKind.Combat,
            MapNodeKind.Boss,
        };

        [Tooltip("**Branching 用**：一列 ＝ 一層，由起點排到 Boss。\n\n" +
                 "第 0 層會被 First Layer Kind 蓋掉（那一格才是「首排放什麼」的真相），\n" +
                 "所以這裡第 0 層填什麼都行。\n\n" +
                 "⚠️ **最後一層建議只放一個 Boss** —— 打完 Boss 這場 run 就結束了" +
                 "（`MapData.IsFinalLayer`），放兩個的話另一個永遠走不到。")]
        public List<DemoLayer> demoRouteLayers = new List<DemoLayer>();
    }

    /// <summary>
    /// DEMO 路線的形狀。
    ///
    /// 【為什麼要有分支】直線路線驗不到地圖本身 ——
    /// 只有一條路的時候「連線」「可前往／去不了」「選節點」全部沒有作用，
    /// 而那些正是地圖這一層要測的東西。
    /// </summary>
    public enum DemoRouteShape
    {
        /// <summary>一層一個節點，前後相連。讀 `demoRouteKinds`。</summary>
        Straight = 0,

        /// <summary>一層可以有好幾個節點，玩家要選。讀 `demoRouteLayers`。</summary>
        Branching = 1,
    }

    /// <summary>
    /// DEMO 分支路線的一層。
    ///
    /// 【為什麼要包一層】Unity 序列化不了 `List&lt;List&lt;T&gt;&gt;` ——
    /// 巢狀泛型不會出現在 Inspector 上，而且**不會報錯**，只會靜靜地是空的。
    /// 包成一個 `[Serializable]` 的小類別是這個限制的標準解法。
    /// </summary>
    [System.Serializable]
    public class DemoLayer
    {
        [Tooltip("這一層由左到右有哪些節點")]
        public List<MapNodeKind> nodes = new List<MapNodeKind>();
    }
}
