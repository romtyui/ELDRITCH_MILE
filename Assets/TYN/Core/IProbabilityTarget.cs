namespace EldritchMile.Core
{
    /// <summary>
    /// 需要機率判定的互動目標：對話選項、上鎖寶箱、路人、NPC。
    ///
    /// 【C18 關鍵】衰減狀態掛在「目標」上，不是掛在卡片上 ——
    /// 同一個選項被反覆嘗試會越來越難，換一個選項則各自獨立計算。
    ///
    /// 【為什麼舊碼被封存】DialogueOptionInteractable 用 hasResolved、
    /// EnemyInteractable 用 hasTriggered，都是「結算過就鎖死」的一次性旗標，
    /// 與「可以反覆嘗試」的設計根本衝突，無法改造，只能重寫。
    /// </summary>
    public interface IProbabilityTarget
    {
        /// 顯示名稱，用於 log 與 UI
        string DisplayName { get; }

        /// C17：本目標的屬性，決定與卡片的相剋結果
        ExploreAttribute Attribute { get; }

        /// <summary>
        /// C18④⑤：當前衰減倍率。初始 1.0，每次對此目標出牌後降一級。
        /// 【注意】hover 預覽顯示的機率必須把這個值算進去，
        /// 否則玩家反覆嘗試後看到的是過期數字。
        /// </summary>
        float CurrentDecayMultiplier { get; }

        /// <summary>
        /// 出牌後呼叫，讓此目標下降一級。
        /// step 由 DialogueEncounterController 依手牌總數算好傳入 ——
        /// 目標自己不需要知道「一共有幾張牌」這種環節層級的資訊。
        /// </summary>
        void ApplyDecay(float step);

        /// <summary>
        /// C18③：出牌結果 → 即時更新選項內文與角色對話框。
        /// 【注意】這裡**不可**判定「成功就結束環節」——
        /// 蓄意失敗是合法策略(C18⑦)，結束與否只能由玩家按結束鈕決定。
        /// </summary>
        void OnCheckResult(bool success, float usedRate);

        /// C17：hover 預覽。rate 已包含相剋與衰減。
        void ShowPreview(float rate, Effectiveness eff);

        void HidePreview();
    }
}
