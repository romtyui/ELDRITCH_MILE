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
        [Tooltip("節點怎麼擺。Grid = 舊的網格（每層一條水平線）；Organic = 放射生長")]
        public MapLayout layout = MapLayout.Grid;

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
        [Header("Organic 擺法（layout = Organic 才生效）")]
        [Tooltip("扇形張開幾度。越大越像樹冠展開，太大會貼到左右邊界")]
        [Range(20f, 170f)] public float organicSpread = 110f;

        [Tooltip("中段最多幾個節點。樹狀感來自「中間寬、兩頭窄」")]
        [Range(2, 7)] public int organicWidthMax = 4;

        [Tooltip("半徑抖動，佔一層間距的比例。**這個值就是在打散水平線** —— 0 會退回並排")]
        [Range(0f, 0.9f)] public float organicRadialJitter = 0.45f;

        [Tooltip("角度抖動，佔一格角距的比例。讓同一層的節點不要等距排開")]
        [Range(0f, 0.9f)] public float organicAngleJitter = 0.4f;

        [Tooltip("兩個節點至少要距離多遠（百分比）。太小會擠成一團，太大會生不出節點")]
        [Range(3f, 20f)] public float organicMinSpacing = 9f;

        [Tooltip("額外橫向連線的機率。這是「選擇更自由」的來源 —— 0 就是純樹、只能往前")]
        [Range(0f, 1f)] public float organicCrossLink = 0.35f;

        [Header("Terrain 擺法（layout = Terrain 才生效）")]
        [Tooltip("拿來判斷地形的底圖。要打開 Read/Write。留空則退回 Organic")]
        public Texture2D terrainMap;

        [Tooltip("亮度低於這個值算水域，不放節點。底圖實測雙峰分在 0.5 左右")]
        [Range(0f, 1f)] public float waterThreshold = 0.5f;

        [Tooltip("離水多近算「海岸」。真實地圖的聚落多半靠水，這裡加權")]
        [Range(0f, 30f)] public float coastRange = 8f;

        [Tooltip("海岸的權重倍率。1 = 不特別偏好，3 = 明顯往岸邊聚")]
        [Range(1f, 6f)] public float coastBias = 2.5f;

        [Tooltip("每個節點要試幾個候選點。越多分佈越均勻（藍雜訊），代價是生成變慢")]
        [Range(4, 60)] public int candidatesPerNode = 24;

        [Tooltip("每一站最多幾條往前的路。太多會變成三角網格，太少會退回一直線")]
        [Range(1, 5)] public int maxForwardLinks = 2;

        [Tooltip("連線最多容許幾層落差。1 = 只能往前一層，2 = 可以跳一層（路更自由）")]
        [Range(1, 3)] public int maxLayerJump = 1;

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
    /// <summary>
    /// 節點的擺法。
    ///
    /// 【Grid】舊的網格：`yPercent` 是 `layer` 的**純函數**，
    /// 所以同一層必然落在同一條水平線上 —— 那就是「並排」與「扁平」的來源。
    /// x 也被吸附到固定欄位，連直的都對齊。
    ///
    /// 【Organic】從底部放射生長：半徑隨深度增加、角度隨深度張開，
    /// 而且**半徑帶抖動**讓相鄰深度的節點互相交錯 —— 沒有水平線，
    /// 看起來像地圖上散落的地標而不是方格紙。
    /// </summary>
    public enum MapLayout
    {
        Grid = 0,
        Organic = 1,

        /// <summary>
        /// 讀底圖的地形來擺節點：水域不放、海岸邊加權，
        /// 再用 best-candidate 取樣（藍雜訊）避免擠在一起，
        /// 最後用 Delaunay 三角化連線 —— **三角化是平面圖，連線保證不交叉**。
        /// </summary>
        Terrain = 2,
    }

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
