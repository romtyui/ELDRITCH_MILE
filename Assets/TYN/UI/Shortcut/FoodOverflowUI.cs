using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EldritchMile.Core;
using EldritchMile.UI.Shortcut;

namespace EldritchMile.UI
{
    /// <summary>
    /// 食物欄滿了又拿到新的 —— 讓玩家決定去留。（2026-10-18 定案）
    ///
    /// ────────────────────────────────────────────────────────
    /// 【規則】食物同種類不疊加、一個佔一格，最多 <see cref="RunContext.FoodSlots"/> 格（3）。
    /// 滿了再拿到時跳這個視窗，二選一：
    ///   1. **吃掉**任一個舊的 → 效果照常發生，騰出的那格給新的
    ///   2. **丟掉**任一個 —— 舊的（新的進來）或新的（什麼都不變）
    /// 一次拿到好幾個（寶箱 ×2）會一個一個問。
    ///
    /// 【吃東西走快捷欄那一支】<see cref="ShortcutBarUI.TryUse"/>：戰鬥中改走戰鬥單位、
    /// 付不起代價就不吃、播報實際變化 —— 規則只能有一份。
    /// 這裡傳 respectTiming = false：平常地圖上、對話中不能吃，
    /// 但被迫選擇的這一刻「吃掉一個」本身就是選項。
    ///
    /// 【視窗不能直接關】一定要選一個。關掉的話新的那個要放哪裡沒有答案。
    /// </summary>
    public class FoodOverflowUI : MonoBehaviour
    {
        [Header("元件")]
        public CanvasGroup group;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI hintText;

        [Tooltip("列的父物件（有 VerticalLayoutGroup）")]
        public RectTransform rowsRoot;

        [Tooltip("一列的樣板。保持關閉，執行時複製")]
        public FoodOverflowRow rowTemplate;

        [Header("文字")]
        public string titleFormat = "食物欄滿了（{0}/{0}）";
        public string hintLine = "吃掉一個舊的騰出位子，或丟掉其中一個";
        public string eatLabel = "吃掉";
        public string discardLabel = "丟掉";

        private RunContext run;
        private readonly List<FoodOverflowRow> rows = new List<FoodOverflowRow>();

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            RunContext.OnFoodOverflow += HandleOverflow;
            if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
            SetVisible(false);
        }

        private void OnDestroy()
        {
            RunContext.OnFoodOverflow -= HandleOverflow;
        }

        private void HandleOverflow(RunContext context, string newId)
        {
            run = context;
            Rebuild();
        }

        // ==========================================
        private void Rebuild()
        {
            Clear();

            string pending = run != null ? run.PendingFood : null;
            if (pending == null) { SetVisible(false); return; }

            if (titleText != null) titleText.text = string.Format(titleFormat, run.FoodSlots);
            if (hintText != null) hintText.text = hintLine;

            // 舊的：每一格各一列（同種類不疊，所以兩個乾麵包是兩列）
            var snapshot = new List<ItemStack>(run.inventory);
            for (int i = 0; i < snapshot.Count; i++)
            {
                ItemStack st = snapshot[i];
                if (st == null || st.count <= 0) continue;
                ItemData d = GameFlowManager.Item(st.id);
                if (d == null || !d.HasTag(RunContext.FoodTag)) continue;

                ItemStack captured = st;
                ItemData capturedData = d;
                AddRow(d, false,
                       () => Eat(capturedData),
                       () => DiscardOld(captured));
            }

            // 新的：只能丟
            ItemData nd = GameFlowManager.Item(pending);
            AddRow(nd, true, null, DiscardNew);

            SetVisible(true);
        }

        private void AddRow(ItemData d, bool isNew, System.Action onEat, System.Action onDiscard)
        {
            if (rowTemplate == null || rowsRoot == null) return;

            FoodOverflowRow r = Instantiate(rowTemplate, rowsRoot);
            r.gameObject.SetActive(true);

            if (r.icon != null) { r.icon.sprite = d != null ? d.icon : null; r.icon.enabled = r.icon.sprite != null; }
            if (r.nameText != null) r.nameText.text = d != null ? d.Label : "?";
            if (r.descText != null) r.descText.text = d != null ? d.description : "";
            if (r.newBadge != null) r.newBadge.SetActive(isNew);

            if (r.eatButton != null)
            {
                r.eatButton.gameObject.SetActive(onEat != null && d != null && d.IsUsable);
                SetLabel(r.eatButton, eatLabel);
                if (onEat != null) r.eatButton.onClick.AddListener(() => onEat());
            }
            if (r.discardButton != null)
            {
                SetLabel(r.discardButton, discardLabel);
                r.discardButton.onClick.AddListener(() => onDiscard());
            }

            rows.Add(r);
        }

        // ==========================================
        private void Eat(ItemData d)
        {
            ShortcutBarUI bar = FoodBar();
            // 吃不下去（付不起代價）時 TryUse 自己會說明原因，視窗留著讓玩家改選
            bool eaten = bar != null ? bar.TryUse(d, false) : run.ConsumeItem(d.id, 1);
            if (!eaten) return;

            run.AcceptPendingFood();
            Next();
        }

        private void DiscardOld(ItemStack stack)
        {
            run.DiscardStack(stack);
            run.AcceptPendingFood();
            Next();
        }

        private void DiscardNew()
        {
            run.DiscardPendingFood();
            Next();
        }

        /// <summary>這一個處理完。佇列裡還有的話接著問下一個。</summary>
        private void Next()
        {
            ShortcutBarUI bar = FoodBar();
            if (bar != null) bar.Refresh(false);

            if (run != null && run.PendingFood != null)
            {
                // 騰出位子之後佇列裡的下一個可能直接放得進去（例如丟了兩個）
                while (run.PendingFood != null && run.AcceptPendingFood()) { }
                if (bar != null) bar.Refresh(false);
            }

            Rebuild();
        }

        // ==========================================
        private static ShortcutBarUI FoodBar()
        {
            foreach (ShortcutBarUI b in FindObjectsByType<ShortcutBarUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (b.filterTag == RunContext.FoodTag) return b;
            return null;
        }

        private void Clear()
        {
            for (int i = 0; i < rows.Count; i++) if (rows[i] != null) Destroy(rows[i].gameObject);
            rows.Clear();
        }

        private void SetVisible(bool on)
        {
            IsOpen = on;
            if (group != null)
            {
                group.alpha = on ? 1f : 0f;
                group.blocksRaycasts = on;
                group.interactable = on;
            }
        }

        private static void SetLabel(Button b, string text)
        {
            TextMeshProUGUI t = b.GetComponentInChildren<TextMeshProUGUI>(true);
            if (t != null) t.text = text;
        }
    }
}
