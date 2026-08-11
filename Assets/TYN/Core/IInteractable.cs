namespace EldritchMile.Core
{
    /// <summary>
    /// 場景中可互動的物件：寶箱、門、路人、商店招牌…
    ///
    /// 取代舊的 ICardInteractable —— 那個介面原本定義在 EnemyInteractable.cs 裡面
    /// （一個「敵人」腳本卻承載了全域介面），造成封存時的連鎖相依。
    ///
    /// 需要機率判定的物件，另外再實作 IProbabilityTarget。
    /// 不需判定的（例如流程圖上「不須賭博判定」的寶箱）只實作本介面。
    /// </summary>
    public interface IInteractable
    {
        string DisplayName { get; }

        /// 目前是否可互動。已開過的寶箱、已談過的 NPC 會回傳 false。
        bool CanInteract { get; }

        /// C8：滑鼠移上去時是否要顯示「可抓取」的手勢游標
        bool ShowGrabCursor { get; }

        /// 玩家確認互動（C8 的第二段：握拳抓取）
        void Interact();
    }
}
