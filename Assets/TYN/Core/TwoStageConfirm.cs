using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace EldritchMile.Core
{
    /// <summary>
    /// C8/C14：兩段式確認。
    ///
    /// 專案裡有兩處用同一種互動語彙：
    ///   · ExitTag  ── hover 標籤下拉提示 → 再點一次確認退出
    ///   · 可拾取道具 ── hover 手張開 → 點擊握拳抓取
    ///
    /// 【與 BookmarkHover 的分工】ExitTag 上既有的 BookmarkHover 已經做完
    /// 「hover → 從上緣滑下」這第一段，而且做得很乾淨（單一職責）。
    /// 本元件只補第二段的點擊確認，兩者掛在同一個物件上互不干涉。
    /// </summary>
    [AddComponentMenu("Eldritch/Two Stage Confirm")]
    public class TwoStageConfirm : MonoBehaviour, IPointerClickHandler, IPointerExitHandler
    {
        [Header("行為")]
        [Tooltip("進入待確認狀態後，多久沒有第二次點擊就自動解除（秒）。0 = 不自動解除")]
        public float armedTimeout = 3f;

        [Tooltip("滑鼠移開時是否立即解除待確認狀態")]
        public bool disarmOnPointerExit = true;

        [Header("事件")]
        [Tooltip("第一次點擊：進入待確認。用來播提示動畫或換文字")]
        public UnityEvent onArmed;

        [Tooltip("待確認狀態被解除（逾時或滑鼠移開）")]
        public UnityEvent onDisarmed;

        [Tooltip("第二次點擊：確認執行")]
        public UnityEvent onConfirmed;

        public bool IsArmed { get; private set; }

        private float armedAt;

        private void Update()
        {
            if (!IsArmed || armedTimeout <= 0f) return;

            if (Time.unscaledTime - armedAt >= armedTimeout)
            {
                Disarm();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsArmed)
            {
                IsArmed = false;
                onConfirmed?.Invoke();
            }
            else
            {
                Arm();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (disarmOnPointerExit) Disarm();
        }

        public void Arm()
        {
            if (IsArmed) return;

            IsArmed = true;
            armedAt = Time.unscaledTime;
            onArmed?.Invoke();
        }

        public void Disarm()
        {
            if (!IsArmed) return;

            IsArmed = false;
            onDisarmed?.Invoke();
        }

        private void OnDisable()
        {
            IsArmed = false;
        }
    }
}
