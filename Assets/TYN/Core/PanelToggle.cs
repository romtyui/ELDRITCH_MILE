using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 面板顯示的三段 fallback，抽出來給多個地方共用。
    ///
    /// 專案裡的確認面板不一定都掛滿元件 —— 有的走 UIDirector 的堆疊、有的只想淡入、
    /// 有的就是個 SetActive。每個呼叫端各判斷一次的話，行為會慢慢長歪
    /// （某個面板換環節時忘了收、某個面板硬切）。收斂成同一份就不會。
    ///
    ///   1. 掛了 UIPanel(Dialog) → 交給 UIDirector 的堆疊管理，
    ///      換環節時 CloseAllDialogs() 會自動收掉，不必靠呼叫端記得關
    ///   2. 只掛了 FadePanel → 淡入淡出（與 MapBanner 外觀一致）
    ///   3. 都沒有 → SetActive
    ///
    /// 元件查詢的結果會快取，換了目標物件才重查。
    ///
    /// 【尚未收斂的一份】<see cref="ExploreStageController"/> 的
    /// <c>SetContinueAskVisible</c> 是同一套邏輯的手寫版。它目前可運作，
    /// 本輪不動；下次要改那一帶時可以直接換成這個型別。
    /// </summary>
    [System.Serializable]
    public class PanelToggle
    {
        private GameObject resolvedFor;
        private FadePanel fade;
        private UIPanel ui;

        /// <summary>目前這個 toggle 認為面板是開著的。</summary>
        public bool IsVisible { get; private set; }

        public void Set(GameObject panel, bool visible)
        {
            if (panel == null) return;

            Resolve(panel);
            IsVisible = visible;

            if (ui != null && ui.kind == UIKind.Dialog && UIDirector.Instance != null)
            {
                if (visible) UIDirector.Instance.PushDialog(ui);
                else UIDirector.Instance.CloseDialog(ui);
                return;
            }

            if (fade != null)
            {
                if (visible) fade.Show();
                else fade.Hide();
                return;
            }

            panel.SetActive(visible);
        }

        private void Resolve(GameObject panel)
        {
            // 同一個物件只查一次。GetComponent 不算貴，但這會在每次出牌後跑，
            // 而且快取也讓「中途換面板」這件事變得明確而不是碰巧能動。
            if (resolvedFor == panel) return;

            resolvedFor = panel;
            fade = panel.GetComponent<FadePanel>();
            ui = panel.GetComponent<UIPanel>();
        }
    }
}
