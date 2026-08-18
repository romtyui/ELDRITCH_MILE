using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 教學訊號的**轉接頭**。我方要告訴新手教學「玩家剛剛做了那件事」時，走這裡。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【為什麼有這一層，而不是各處直接呼叫 TutorialEventBus】
    ///
    /// 新手教學那一整套（`TutorialManager` / `TutorialUI` / `TutorialStepData`…）
    /// 是 Romtyui 的，住在 `Assets/Romtyui/`。**我方不改他的檔案，只呼叫。**
    /// 所以把呼叫集中在這一支 —— 他哪天搬家或改名，要修的只有這裡一個地方。
    ///
    /// 這跟 `PlayerVitals` 對 `RunStateManager` 是完全一樣的處理方式。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【現況：訊號早就定義好了，但沒有人發】
    ///
    /// 他的 `TutorialSignals` 裡已經列了整條新手教學需要的訊號，
    /// **包含探索與地圖那一段**（`MapOpened` / `ExploreCardPlayed` / `WeaponObtained`…）。
    /// 但實際會發訊號的只有戰鬥那半邊（`BattleManager` / `CardDragUI`）——
    /// 因為另一半的觸發點在我方的檔案裡，他碰不到。
    ///
    /// 所以這一支補的就是那一半。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【沒在跑教學的時候會怎樣】什麼都不會發生。
    /// `TutorialEventBus` 只是把訊號丟給訂閱者，沒有人訂閱就散掉了。
    /// 所以這些呼叫**永遠是安全的**，不需要先判斷「現在是不是在教學中」。
    /// </summary>
    public static class TutorialSignal
    {
        /// <summary>
        /// 印出每一次發送。查「為什麼教學卡在這一步」時打開 ——
        /// 通常不是訊號沒發，是 `TutorialStepData` 上填的字串跟這裡對不起來。
        /// </summary>
        public static bool Verbose = false;

        // ⚠️ 訊號名稱**刻意不自己重打一份**，直接用他的常數。
        //    自己抄一份的話，他改字串我方不會知道，而且不會有編譯錯誤 ——
        //    症狀會是「教學卡在那一步不動」，非常難查。

        /// <summary>地圖下拉完成。大綱：【提示：地圖UI】</summary>
        public static void MapOpened() => Raise(TutorialSignals.MapOpened);

        /// <summary>地圖收起完成。大綱：【玩家操作：關閉地圖】</summary>
        public static void MapClosed() => Raise(TutorialSignals.MapClosed);

        /// <summary>探索／對話打出了一張機率卡並判定完畢。大綱：【玩家操作：使用卡牌】</summary>
        public static void ExploreCardPlayed() => Raise(TutorialSignals.ExploreCardPlayed);

        /// <summary>拿到了武器（不分來源）。大綱：【玩家操作：探索場地、獲得武器】</summary>
        public static void WeaponObtained() => Raise(TutorialSignals.WeaponObtained);

        private static void Raise(string signalId)
        {
            if (Verbose) Debug.Log($"[教學訊號] {signalId}");

            TutorialEventBus.Raise(signalId);
        }
    }
}
