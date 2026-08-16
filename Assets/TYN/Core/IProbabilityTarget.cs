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

        /// <summary>
        /// 「這張牌現在會打在我身上」的視覺回饋。
        ///
        /// 【為什麼需要】拖曳時卡片跟著游標走，玩家其實**看不出自己瞄到了什麼** ——
        /// 尤其目標被對話框壓住一半、或旁邊還有別的可投放區時。
        /// 結果是放開之後才發現打錯地方，而那一步是不可逆的（消耗手牌 + 目標衰減）。
        ///
        /// 兩種情境都會呼叫：拖曳中游標移到目標上、以及兩段式出牌選了卡之後移到目標上。
        ///
        /// 【實作建議】用**變暗**這種低調的變化就好。目標同時還要顯示機率數字，
        /// 太搶眼的效果會蓋過那個資訊。
        /// </summary>
        void SetTargeted(bool targeted);

        /// <summary>
        /// 手牌出完了仍未成功 —— 這次遭遇的嘗試機會用盡。
        ///
        /// 【為什麼一定要有這支】在此之前，判定失敗完全沒有「結案」的路徑：
        /// 只有成功會走 MarkDone()，所以徹底失敗的物件永遠 CanInteract == true。
        /// 造成兩件事：
        ///   · 玩家可以無限重點、重抽手牌 —— 但衰減已歸零，每次都是保證 0%（假迴圈）
        ///   · 物件永遠不回報房間，interactedCount 到不了總數，
        ///     **C13 的「要探索其他的東西嗎？」永遠不會自動跳出來**
        ///
        /// 【呼叫時機】由 ExploreStageController 在環節結束且 !HasCardsLeft 時呼叫。
        /// 中途按結束（還有手牌）**不會**呼叫 —— 那是暫停，不是用盡。
        ///
        /// 【實作責任】目標自行決定要結案、還是提供「付出代價重來」的機會
        /// （衰減重置、手牌重抽）。成功過的目標要自己判斷並忽略這次呼叫。
        /// </summary>
        void OnAttemptsExhausted();
    }
}
