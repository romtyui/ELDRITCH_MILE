using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 遊戲啟動時把解析度鎖在 **1920×1080**。（2026-10-14 定案）
    ///
    /// ────────────────────────────────────────────────────────
    /// 【為什麼要鎖】Romtyui 回報畫面變形：我們的 UI 都以 1920×1080 為設計尺寸、
    /// 跟著解析度等比縮放；他戰鬥的幾塊畫布原本是固定像素、不縮放。
    /// 1080p 下兩邊一模一樣，一換解析度就對不上。處理分兩層：
    ///   1. Stage_Battle_v3 裡他那四塊畫布改成 1920×1080 等比縮放（任何解析度都一致）
    ///   2. 這裡把解析度鎖死，所有人看到的都是 1080p 的版面，由系統放大縮小 ——
    ///      就算哪裡還有寫死像素的地方，也不會因為玩家的螢幕不同而跑掉
    /// 搭配 `LetterboxBars`（全環節常駐）：螢幕不是 16:9 時多出來的地方蓋成黑邊。
    ///
    /// 【為什麼 Player Settings 設好了還要這支】Unity 會把玩家上次的解析度存進登錄檔，
    /// **之後每次啟動都沿用舊值**，Player Settings 的預設只有第一次啟動才吃得到。
    /// 改版前就玩過的人（還有我們自己）會一直卡在舊解析度上。
    ///
    /// 編輯器裡不動 —— Game 視窗的大小是開發者自己選的，也可能正在故意測別的比例。
    /// </summary>
    public static class ResolutionLock
    {
        public const int Width = 1920;
        public const int Height = 1080;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
#if !UNITY_EDITOR
            // 視窗模式就維持視窗（Alt+Enter 切過去的人），其他一律無邊框全螢幕
            FullScreenMode mode = Screen.fullScreenMode == FullScreenMode.Windowed
                ? FullScreenMode.Windowed
                : FullScreenMode.FullScreenWindow;

            if (Screen.width != Width || Screen.height != Height || Screen.fullScreenMode != mode)
            {
                Screen.SetResolution(Width, Height, mode);
                Debug.Log($"[解析度] 鎖回 {Width}x{Height}（{mode}）—— 原本是 {Screen.width}x{Screen.height}");
            }
#endif
        }
    }
}
