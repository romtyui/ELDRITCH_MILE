using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// UI 元素的分類。取自業界慣用的 Panel / Dialog / Widget 三分法：
    /// </summary>
    public enum UIKind
    {
        /// <summary>
        /// **面板** ── 依當前 Stage 自動開關，不參與堆疊。
        /// 例：探索的手牌區、地圖覆蓋層、商店櫃台。
        /// 判準：「它屬於某個環節，換環節就該消失」。
        /// </summary>
        Panel,

        /// <summary>
        /// **對話框** ── 模態，用堆疊管理（後開的先關，LIFO）。
        /// 例：系統提示、確認視窗、設定選單。
        /// 判準：「它疊在當前畫面上，關掉之後要回到原本的地方」。
        /// </summary>
        Dialog,

        /// <summary>
        /// **元件** ── 由它的父物件自行控制，UIDirector 完全不管。
        /// 例：對話框裡的選項框、血條、卡片。
        /// 判準：「它不是獨立的畫面，是別人的一部分」。
        /// </summary>
        Widget,
    }

    /// <summary>
    /// 掛在每一個需要被統一管理的 UI 根物件上。
    ///
    /// 【為什麼需要】原本每個系統各自 SetActive，很容易出現
    /// 「換了環節但上一個環節的 UI 還開著」這種漏關的 bug ——
    /// 而且會愈來愈難查，因為沒有任何一個地方說得出「現在應該有哪些 UI」。
    ///
    /// 改成宣告式：**每個面板自己宣告「我屬於哪些環節」**，
    /// 由 UIDirector 在環節切換時統一套用。
    /// 這比在中央維護一份清單好 —— 加新面板時不必回頭改別的檔案。
    /// </summary>
    public class UIPanel : MonoBehaviour
    {
        [Header("分類")]
        public UIKind kind = UIKind.Panel;

        [Tooltip("Panel 專用：屬於哪些環節。\n" +
                 "清單內的環節會自動開啟，其餘自動關閉。\n" +
                 "留空 = 任何環節都不自動開（等於完全手動控制）")]
        public List<StageType> visibleInStages = new List<StageType>();

        [Tooltip("Panel 專用：勾選則不論在哪個環節都保持開啟（常駐 HUD）")]
        public bool alwaysVisible = false;

        [Header("行為")]
        [Tooltip("有 FadePanel 就用淡入淡出，沒有就直接 SetActive")]
        public bool useFadeIfAvailable = true;

        private FadePanel fade;
        private bool fadeResolved;

        public bool IsVisible => gameObject.activeSelf;

        private FadePanel Fade
        {
            get
            {
                if (!fadeResolved)
                {
                    fadeResolved = true;
                    if (useFadeIfAvailable) fade = GetComponent<FadePanel>();
                }
                return fade;
            }
        }

        public void Show()
        {
            if (Fade != null) Fade.Show();
            else gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (Fade != null) Fade.Hide();
            else gameObject.SetActive(false);
        }

        public void SetVisibleImmediate(bool visible)
        {
            if (Fade != null && !visible) Fade.HideImmediate();
            else gameObject.SetActive(visible);
        }

        /// <summary>由 UIDirector 呼叫。回傳這個面板在該環節是否該顯示。</summary>
        public bool ShouldBeVisibleIn(StageType stage)
        {
            if (alwaysVisible) return true;
            return visibleInStages != null && visibleInStages.Contains(stage);
        }
    }
}
