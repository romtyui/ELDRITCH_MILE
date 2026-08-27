using UnityEngine;
using UnityEngine.UI;

// ⚠️ 這個專案的 Player Settings 切到 **Input System package**。
// 舊的 `UnityEngine.Input.mousePosition` 執行時會丟 InvalidOperationException，
// 而且**編譯期完全看不出來** —— 舊 API 仍然存在、仍然編得過。
// 寫法對齊 RunDebugPanel 與隊友的 BattleDebugHotkeys，全專案一致。
using UnityEngine.InputSystem;

namespace EldritchMile.UI.Shortcut
{
    /// <summary>
    /// 平時淡出收合，**滑鼠靠近畫面邊緣才淡入**。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【為什麼需要這個】操作介面要放在最上層才點得到，但放最上層又會一直擋在
    /// 畫面前面。「靠邊才出現」兩件事都解決了：平常不礙眼，要用的時候手往那邊
    /// 一移就出來。
    ///
    /// 【為什麼不是滑到物件上才淡入】那是雞生蛋 —— 淡出到 0 的時候
    /// `CanvasGroup.blocksRaycasts` 若一起關掉就永遠碰不到；不關的話又會
    /// 擋到底下的東西。改用「滑鼠在螢幕的哪一區」判斷就沒有這個問題，
    /// 完全不依賴射線。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class EdgeRevealUI : MonoBehaviour
    {
        public enum Edge { Right, Left, Top, Bottom }

        [Header("觸發")]
        [Tooltip("靠哪一邊")]
        public Edge edge = Edge.Right;

        [Tooltip("距離邊緣多少**比例**以內就算靠近。0.15 = 螢幕寬度的 15%")]
        [Range(0.02f, 0.5f)] public float triggerZone = 0.16f;

        [Tooltip("已經展開時，容忍範圍放大多少倍。\n" +
                 "**大於 1 是刻意的** —— 不然滑鼠在邊界上抖一下就會一直閃")]
        [Min(1f)] public float stickyMultiplier = 1.6f;

        [Header("淡入淡出")]
        [Range(0f, 1f)] public float hiddenAlpha = 0f;
        [Range(0f, 1f)] public float shownAlpha = 1f;

        [Min(0.01f)] public float fadeSeconds = 0.18f;

        [Header("除錯")]
        [Tooltip("勾起來就一直顯示，方便對位")]
        public bool alwaysShow = false;

        private CanvasGroup group;
        private bool shown;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            group.alpha = hiddenAlpha;
            ApplyInteractable(false);
        }

        private void Update()
        {
            bool want = alwaysShow || IsPointerNearEdge();

            if (want != shown)
            {
                shown = want;
                ApplyInteractable(shown);
            }

            float target = shown ? shownAlpha : hiddenAlpha;
            group.alpha = Mathf.MoveTowards(group.alpha, target,
                Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeSeconds));
        }

        private bool IsPointerNearEdge()
        {
            // 沒有滑鼠（手把、觸控裝置）時 Mouse.current 是 null，不是錯誤 ——
            // 那種情況下就當作「不靠近」，介面維持收合
            Mouse mouse = Mouse.current;
            if (mouse == null) return false;

            Vector2 m = mouse.position.ReadValue();

            // 滑鼠跑到視窗外時不要判定成「靠近邊緣」
            if (m.x < 0 || m.y < 0 || m.x > Screen.width || m.y > Screen.height) return false;

            float zone = triggerZone * (shown ? stickyMultiplier : 1f);

            switch (edge)
            {
                case Edge.Right:  return m.x >= Screen.width  * (1f - zone);
                case Edge.Left:   return m.x <= Screen.width  * zone;
                case Edge.Top:    return m.y >= Screen.height * (1f - zone);
                default:          return m.y <= Screen.height * zone;
            }
        }

        /// <summary>
        /// 淡出時**同時關掉 raycast** —— 不然一個看不見的面板會擋住底下的東西，
        /// 那種 bug 症狀是「某一區點不到」，非常難查。
        /// </summary>
        private void ApplyInteractable(bool on)
        {
            group.interactable = on;
            group.blocksRaycasts = on;
        }
    }
}
