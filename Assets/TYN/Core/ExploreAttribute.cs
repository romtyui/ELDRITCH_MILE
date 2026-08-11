namespace EldritchMile.Core
{
    /// <summary>
    /// C17：機率卡與互動目標的屬性。
    /// 【Q7a 待定】名稱與數量尚未定案，先用佔位值。改名不影響任何邏輯，
    /// 因為所有倍率都由 AttributeChartData 這個 ScriptableObject 決定。
    /// </summary>
    public enum ExploreAttribute
    {
        None = 0,
        AttrA = 1,
        AttrB = 2,
        AttrC = 3,
        AttrD = 4,
    }

    /// <summary>
    /// C19：相剋效果。三級，**沒有 2×**。
    ///
    /// 卡片自身的 successProbability 就是機率上限，相剋只會往下扣 ——
    /// 這是懲罰制，不是寶可夢那種雙向增減。實作時不要自作主張加獎勵層。
    /// </summary>
    public enum Effectiveness
    {
        /// 1.0× 相符
        Match = 0,

        /// 0.5× 模糊相關。
        /// 存在目的是「避免抽到的卡完全不能操作」，所以應該是常見情況。
        Partial = 1,

        /// 0.0× 無效。
        /// 因為 Partial 就是為了避免死鎖而設，所以 None 應該少用，
        /// 只保留給設計上真的要封死的組合。
        None = 2,
    }
}
