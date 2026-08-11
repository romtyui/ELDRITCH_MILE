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
