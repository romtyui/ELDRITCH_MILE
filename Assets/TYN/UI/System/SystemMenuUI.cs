using UnityEngine;
using UnityEngine.UI;

namespace EldritchMile.UI
{
    /// <summary>
    /// 左上角那顆齒輪 —— **整個遊戲都看得到、都按得動**。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【它本來只在戰鬥裡】（2026-10-12 組員回報）
    /// 齒輪與功能表原本是 `Stage_Battle_v2` 的一部分，所以只有打仗時才有。
    /// 現在整組搬到場景層的獨立畫布上，探索、商店、地圖、對話一律都在。
    ///
    /// 【這一支不是功能表本身】功能表是 Romtyui 的 <see cref="OptionMenuUI"/>，
    /// 這裡只做「把它搬出戰鬥之後會缺的那一塊膠水」，**不去改他的檔案**：
    ///
    ///   · `battleManager` 以前是 prefab 內部的引用，搬出來就接不到了。
    ///     改成**開啟的當下現找** —— 不需要任何 Stage 事件，也不怕戰鬥重生。
    ///   · 不在戰鬥裡的時候「重新開始」沒有意義（`RestartGame()` 會對 null 什麼都不做，
    ///     按了沒反應玩家會以為壞了），所以沒有 BattleManager 時直接把那顆藏起來。
    ///
    /// 【為什麼用輪詢而不是接事件】開啟的路徑有兩條：點齒輪，以及
    /// `OptionMenuUI.Update()` 裡的 Esc。後者在 Romtyui 的檔案裡，
    /// 要接事件就得改他的碼。看 `panelRoot` 的開關邊緣兩條路都涵蓋得到，
    /// 一幀一個 bool 比較便宜，也不會跟他的改動打架。
    /// </summary>
    public class SystemMenuUI : MonoBehaviour
    {
        public static SystemMenuUI Instance { get; private set; }

        [Header("元件")]
        [Tooltip("Romtyui 的功能表。留空會自己在子物件裡找")]
        public OptionMenuUI option;

        [Tooltip("左上角的齒輪。想在某些畫面藏起來時用 SetGearVisible()")]
        public GameObject gearButton;

        [Header("回到主畫面")]
        [Tooltip("把功能表的「回到主畫面」改接到 GameFlowManager.GoToMenu()。"
                 + "原本是 SceneManager.LoadScene(\"test_Scene\")，但 build settings 裡只有 EventScene，"
                 + "那顆按下去只會噴「場景沒加進 build」。本專案的主選單是 EventScene 裡的一個 Stage，不是獨立場景。")]
        public bool rerouteMainMenuToFlow = true;

        /// <summary>功能表現在開著嗎。別處要擋輸入時可以看這個。</summary>
        public bool IsOpen
        {
            get { return option != null && option.panelRoot != null && option.panelRoot.activeSelf; }
        }

        private bool wasOpen;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // 場景裡只該有一顆。重複的直接收掉，不要兩顆同時吃 Esc
                Debug.LogWarning($"[系統選單] 場景裡有兩個 SystemMenuUI，關掉後來的「{name}」", this);
                gameObject.SetActive(false);
                return;
            }

            Instance = this;

            if (option == null) option = GetComponentInChildren<OptionMenuUI>(true);

            if (option == null)
                Debug.LogWarning($"[系統選單]「{name}」底下沒有 OptionMenuUI，齒輪按了不會有事", this);
        }

        /// <summary>
        /// 接在 Start 而不是 Awake：要等 <see cref="OptionMenuUI"/> 自己的 Awake
        /// 先把它的 ReturnToMainMenu 掛上去，才有東西可以拆。
        /// </summary>
        private void Start()
        {
            if (!rerouteMainMenuToFlow) return;
            if (option == null || option.mainMenuButton == null) return;

            option.mainMenuButton.onClick.RemoveListener(option.ReturnToMainMenu);
            option.mainMenuButton.onClick.AddListener(GoToMenuViaFlow);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            if (option != null && option.mainMenuButton != null)
                option.mainMenuButton.onClick.RemoveListener(GoToMenuViaFlow);
        }

        /// <summary>
        /// 回主選單。**不換場景** —— 主選單是 EventScene 裡的一個 Stage。
        /// </summary>
        private void GoToMenuViaFlow()
        {
            option.Close();

            var flow = EldritchMile.Core.GameFlowManager.Instance;

            if (flow == null)
            {
                Debug.LogWarning("[系統選單] 場上沒有 GameFlowManager，回不了主選單", this);
                return;
            }

            flow.GoToMenu();
        }

        private void Update()
        {
            bool open = IsOpen;

            // 只在「剛打開」那一幀做事 —— 關著的時候不用每幀去找 BattleManager
            if (open && !wasOpen) OnOpened();

            wasOpen = open;
        }

        private void OnOpened()
        {
            // ⚠️ 這裡一定要現找。戰鬥 Stage 是動態生成的，
            //    而且每場戰鬥都是新的實例，記快取只會抓到上一場的殘骸
            BattleManager bm = FindFirstObjectByType<BattleManager>();

            option.battleManager = bm;
            option.showRestartButton = bm != null;

            // showRestartButton 改了要重畫一次按鈕可見度。
            // OpenMainPage() 是公開的，而且裡面就會呼叫 RefreshButtonVisibility()
            option.OpenMainPage();
        }

        /// <summary>需要暫時藏起齒輪時用（例如片頭、結算畫面）。功能表本身不受影響。</summary>
        public void SetGearVisible(bool visible)
        {
            if (gearButton != null) gearButton.SetActive(visible);
        }
    }
}
