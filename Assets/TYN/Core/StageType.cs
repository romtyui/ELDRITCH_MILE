namespace EldritchMile.Core
{
    /// <summary>
    /// 舞台類型。互斥 —— 同一時間只會有一個 Stage 存在。
    /// 地圖不在此列，因為它是常駐覆蓋層 (MapOverlay)，與 Stage 正交。詳見設計文件 §4.1。
    /// </summary>
    public enum StageType
    {
        None,
        Menu,
        Intro,          // C9：新手介紹。由 Romtyui 製作，我方只保留位置
        Explore,
        Battle,
        Shop,
        Dialogue,
        SpecialEvent,   // C16：獲得神牌

        /// <summary>
        /// 隨機事件（大綱〈事件〉那一章）。
        ///
        /// ⚠️ 它與其他 Stage 不同：**不是地圖上的一種節點**，而是「進節點之前插播的一段」。
        /// 播完會照常進原本那個節點的 Stage —— 前置，不覆蓋。
        /// 所以地圖不會生成 Event 節點，`StageTypeForNode` 也不會回傳它。
        /// </summary>
        Event,

        /// <summary>
        /// 機率卡牌對話（Romtyui 規格書《對話機率卡牌功能》）。
        ///
        /// ⚠️ **與 <see cref="Dialogue"/> 是兩套，刻意並存。**
        /// 舊的對話（卡打在目標上、出牌即判定、屬性相剋）已經驗收過，
        /// 而且探索打牌跟它共用同一支 controller —— 直接取代會連帶弄壞探索。
        /// 兩個 StageType 並存的成本只是多一個 enum 值，換到的是「舊的還能跑」。
        ///
        /// 等新的這套驗收完、舊對話的資產也都轉過來之後，再考慮退役舊的。
        /// </summary>
        ProbabilityDialogue,
    }

    /// <summary>
    /// Stage 結束時的回報結果。
    /// C2：Stage 結束是「自動」回報，不是玩家按了返回按鈕。
    /// </summary>
    public enum StageResult
    {
        /// 正常完成，接著地圖下拉
        Completed,

        /// 玩家死亡。A4：進入下個輪迴，遺產結算走 RunContext.ContributeToMeta()
        PlayerDied,

        /// 整場 run 走完（打完 Boss）
        RunFinished,
    }
}
