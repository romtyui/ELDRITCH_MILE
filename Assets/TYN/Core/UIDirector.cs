using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 全專案的 UI 開關總管。
    ///
    /// 【採用的模式】業界處理 UI 可見性主要有兩種做法，這裡兩種都用，各管各的：
    ///
    ///   1. **狀態機（宣告式）** → 給 `UIKind.Panel`
    ///      每個面板宣告「我屬於哪些環節」，換環節時統一套用：該開的開、該關的關。
    ///      解決「換環節但上個環節的 UI 還開著」這類漏關 bug —— 因為關閉不再依賴
    ///      「有人記得去呼叫」，而是狀態切換的必然結果。
    ///
    ///   2. **堆疊（LIFO）** → 給 `UIKind.Dialog`
    ///      模態視窗一層層疊上去，關閉時反序退回。
    ///      對應「暫停 → 設定 → 畫面設定」這種進去幾層、出來就幾層的導覽。
    ///
    /// `UIKind.Widget` 完全不管 —— 它屬於某個父物件，不是獨立畫面。
    ///
    /// 【為什麼用掃描而不是註冊】UI 面板常常是預設隱藏的，而 Unity 對
    /// 「一開始就 inactive」的物件不會執行 Awake/OnEnable，註冊式會直接漏掉它們。
    /// 改成環節切換時掃一次（含 inactive），對 UI 規模的專案成本可以忽略。
    /// </summary>
    public class UIDirector : MonoBehaviour
    {
        public static UIDirector Instance { get; private set; }

        [Header("除錯")]
        public bool verboseLog = false;

        private readonly List<UIPanel> panels = new List<UIPanel>();
        private readonly List<UIPanel> dialogStack = new List<UIPanel>();

        public bool HasOpenDialog => dialogStack.Count > 0;
        public int DialogDepth => dialogStack.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ==========================================
        // 狀態機：Panel 依環節開關
        // ==========================================

        /// <summary>
        /// 套用某個環節的 UI 狀態。由 GameFlowManager 在 Stage 切換時呼叫。
        ///
        /// 會先關掉所有堆疊中的 Dialog —— 換環節時不該有上個環節的模態視窗殘留。
        /// </summary>
        public void ApplyStage(StageType stage)
        {
            CloseAllDialogs();
            Rescan();

            int shown = 0, hidden = 0;

            foreach (UIPanel p in panels)
            {
                if (p == null || p.kind != UIKind.Panel) continue;

                bool should = p.ShouldBeVisibleIn(stage);

                if (should && !p.IsVisible) { p.Show(); shown++; }
                else if (!should && p.IsVisible) { p.Hide(); hidden++; }
            }

            if (verboseLog)
            {
                Debug.Log($"[UI] 套用環節 {stage}：開 {shown}、關 {hidden}（共掃到 {panels.Count} 個面板）");
            }
        }

        /// <summary>重新掃描場景中所有的 UIPanel，含 inactive。</summary>
        public void Rescan()
        {
            panels.Clear();
            panels.AddRange(
                FindObjectsByType<UIPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            );
        }

        // ==========================================
        // 堆疊：Dialog
        // ==========================================

        /// <summary>開啟一個模態視窗並疊到最上層。</summary>
        public void PushDialog(UIPanel dialog)
        {
            if (dialog == null) return;

            if (dialog.kind != UIKind.Dialog)
            {
                Debug.LogWarning($"[UI] {dialog.name} 的 Kind 不是 Dialog，不該用 PushDialog", dialog);
            }

            if (dialogStack.Contains(dialog))
            {
                // 已在堆疊中：移到最上層而非疊兩次
                dialogStack.Remove(dialog);
            }

            dialogStack.Add(dialog);
            dialog.Show();

            if (verboseLog) Debug.Log($"[UI] 開啟 {dialog.name}（深度 {dialogStack.Count}）");
        }

        /// <summary>關閉最上層的模態視窗。</summary>
        public void PopDialog()
        {
            if (dialogStack.Count == 0) return;

            int last = dialogStack.Count - 1;
            UIPanel top = dialogStack[last];
            dialogStack.RemoveAt(last);

            if (top != null) top.Hide();

            if (verboseLog) Debug.Log($"[UI] 關閉 {(top != null ? top.name : "?")}（剩 {dialogStack.Count}）");
        }

        /// <summary>關閉指定的模態視窗，不論它在堆疊哪一層。</summary>
        public void CloseDialog(UIPanel dialog)
        {
            if (dialog == null || !dialogStack.Remove(dialog)) return;
            dialog.Hide();
        }

        public void CloseAllDialogs()
        {
            for (int i = dialogStack.Count - 1; i >= 0; i--)
            {
                if (dialogStack[i] != null) dialogStack[i].Hide();
            }
            dialogStack.Clear();
        }

        // ==========================================
        // 便利方法
        // ==========================================

        /// <summary>轉場前把畫面清乾淨。GameFlowManager 在淡黑之後呼叫。</summary>
        public void CloseEverything()
        {
            CloseAllDialogs();
            Rescan();

            foreach (UIPanel p in panels)
            {
                if (p != null && p.kind != UIKind.Widget && p.IsVisible && !p.alwaysVisible)
                {
                    p.Hide();
                }
            }
        }
    }
}
