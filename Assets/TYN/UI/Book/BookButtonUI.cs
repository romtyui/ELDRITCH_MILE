using UnityEngine;
using UnityEngine.UI;

namespace EldritchMile.UI
{
    /// <summary>
    /// 「BOOK」按鈕 —— 打開那本書（背包／圖鑑）。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【為什麼只有一個事件，沒有真的打開書】（2026-09-23）
    /// 那本書現在**只存在於戰鬥裡**，而戰鬥是 Romtyui 的範圍。
    /// 這支只負責「玩家說他要開書」這件事，誰來開、怎麼開，由訂閱的人決定 ——
    /// 等戰鬥那邊把開書的入口開放出來，訂一下 <see cref="OnOpenRequested"/> 就接上了，
    /// 商店、探索、對話節點三邊都不用再改。
    ///
    /// 沒有人訂閱的時候會跳一個提示，而不是靜靜地什麼都不發生 ——
    /// 按了沒反應玩家會以為是壞的。
    ///
    /// 【擺法跟 EXIT 一樣】縮在畫面邊緣、滑鼠靠過去才滑出來（<see cref="SlideOutTab"/>）。
    /// 感應區、滑出的那一層都照 EXIT 那組做，這支只管按下去之後的事。
    /// </summary>
    public class BookButtonUI : MonoBehaviour
    {
        /// <summary>
        /// 玩家要開書了。**訂閱的人負責真的把書打開。**
        ///
        /// ⚠️ 這是 static 事件 —— 訂閱之後記得在自己被銷毀時退訂，
        /// 不然換場景時會抓著已經死掉的物件。
        /// </summary>
        public static event System.Action OnOpenRequested;

        [Header("元件")]
        [Tooltip("被點的那顆。留空會自己找同物件上的 Button")]
        public Button button;

        [Header("還沒接上的時候")]
        [Tooltip("沒有人處理開書時跳的提示。留空則不提示（不建議）")]
        public string notReadyMessage = "背包還沒接上";

        /// <summary>有沒有人真的會處理開書。UI 想先把按鈕變灰的話可以看這個。</summary>
        public static bool HasHandler { get { return OnOpenRequested != null; } }

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();

            if (button == null)
            {
                Debug.LogWarning($"[BOOK] 「{name}」上沒有 Button，點不動", this);
                return;
            }

            button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
        }

        private void HandleClick()
        {
            System.Action handler = OnOpenRequested;

            if (handler != null)
            {
                handler();
                return;
            }

            if (!string.IsNullOrEmpty(notReadyMessage)) ToastUI.TryShow(notReadyMessage);
            Debug.Log("[BOOK] 有人按了開書，但還沒有人接（見 BookButtonUI.OnOpenRequested）");
        }
    }
}
