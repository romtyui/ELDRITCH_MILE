using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EldritchMile.UI.Shortcut
{
    using EldritchMile.Core;

    /// <summary>
    /// 一條快捷欄：平常收合成一個圖示，滑鼠移上去（或點）就**往下一字排開**。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【對應美術稿】
    ///   方案 1／2 —— 收合的圖示展開成一列。這裡做的是方案 2 的變體：
    ///                 由上往下逐格展開，比「一次散開」好做也比較不會閃。
    ///   方案 3　　 —— 滑鼠移上去或點開。兩種都支援，見 openOnHover。
    ///   方案 4／5 —— hover 顯示文字。文字框在這裡，圖案突出在 ShortcutSlotUI。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【⚠️ 這一版只做「看得到」，不做「用得了」】
    ///
    /// 食物要能注射，需要「使用道具」這個動作 —— **那個動作目前不存在**，
    /// 回多少 HP、扣多少 SAN 也還沒有數值。硬做只會做出一個按了會亂跳的假功能。
    ///
    /// 所以 <see cref="OnItemUsed"/> 留在這裡當接口：等「使用道具」做好，
    /// 接上去就會動，UI 不用重做。在那之前點下去只會發一則 Log。
    /// </summary>
    public class ShortcutBarUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("要顯示哪一類")]
        [Tooltip("道具標籤。Food＝食物（針筒）、Curio＝收藏品／遺物（卡冊）。\n" +
                 "留空 = 全部")]
        public string filterTag = "Food";

        [Header("元件")]
        [Tooltip("收合時看得到的那顆圖示。點它或移上去就展開")]
        public GameObject collapsedIcon;

        [Tooltip("展開後那些格子的容器")]
        public RectTransform expandedRoot;

        public ShortcutSlotUI slotPrefab;

        [Header("預設圖")]
        [Tooltip("道具自己沒有 icon 時用這張。食物欄放針管、遺物欄放卡冊。\n\n" +
                 "⚠️ 目前所有道具都還沒有自己的圖 —— 少了這張，整條欄會是一排空框，\n" +
                 "看起來像壞掉。之後某個道具填了自己的 icon 就會自動蓋過這張")]
        public Sprite fallbackIcon;

        [Tooltip("格子外框。留空就用 SC_Slot prefab 上原本的")]
        public Sprite slotFrame;

        [Header("說明框（hover 時）")]
        public GameObject tooltipRoot;
        public TextMeshProUGUI tooltipTitle;
        public TextMeshProUGUI tooltipBody;

        [Header("行為")]
        [Tooltip("勾選＝滑鼠移上去就展開；取消＝要點一下（美術稿方案 3 的兩種）")]
        public bool openOnHover = true;

        [Tooltip("每一格出現的間隔秒數。0 = 同時出現。\n" +
                 "**逐格出現比一次散開好看**，而且格數多的時候不會一片閃")]
        [Min(0f)] public float staggerSeconds = 0.04f;

        [Tooltip("空的時候要不要顯示收合圖示。\n" +
                 "取消勾選＝身上沒有這類道具時整條藏起來")]
        public bool showWhenEmpty = true;

        [Header("空的時候")]
        public string emptyText = "（沒有）";

        // ==========================================
        /// <summary>
        /// 玩家點了某一格。**接口留著，目前沒有人訂閱** ——
        /// 等「使用道具」的動作做好再接上，UI 不用重做。
        /// </summary>
        public event System.Action<ItemData> OnItemUsed;

        private readonly List<ShortcutSlotUI> slots = new List<ShortcutSlotUI>();
        private bool expanded;
        private Coroutine reveal;

        private void OnEnable() => Refresh();

        // ==========================================
        /// <summary>依目前身上的道具重建。</summary>
        /// <param name="collapseAfter">
        /// 重建後要不要收合。展開途中重讀時要傳 false —— 傳 true 會把剛要展開的收掉。
        /// </param>
        public void Refresh(bool collapseAfter = true)
        {
            for (int i = 0; i < slots.Count; i++) if (slots[i] != null) Destroy(slots[i].gameObject);
            slots.Clear();

            RunContext run = GameFlowManager.Instance != null ? GameFlowManager.Instance.Run : null;
            ItemDatabase db = GameFlowManager.Instance != null ? GameFlowManager.Instance.itemDatabase : null;

            if (run == null || db == null || expandedRoot == null || slotPrefab == null)
            {
                if (collapsedIcon != null) collapsedIcon.SetActive(showWhenEmpty);
                return;
            }

            foreach (ItemStack stack in run.inventory)
            {
                if (stack == null || stack.count <= 0) continue;

                ItemData d = db.GetById(stack.id);

                // 沒登記的道具（查不到 ItemData）在**有指定標籤**時跳過 ——
                // 不知道它是不是食物，硬塞進食物欄會誤導。
                // 但它不會消失：F1 除錯面板會把它標成「沒登記」，那才是抓這種錯的地方
                if (d == null) continue;
                if (!string.IsNullOrEmpty(filterTag) && !d.HasTag(filterTag)) continue;

                ShortcutSlotUI s = Instantiate(slotPrefab, expandedRoot);
                s.Bind(d, stack.count, fallbackIcon, slotFrame);
                s.OnHoverChanged += HandleSlotHover;
                s.OnClicked += HandleSlotClicked;
                slots.Add(s);
            }

            if (collapsedIcon != null)
                collapsedIcon.SetActive(showWhenEmpty || slots.Count > 0);

            // 空的時候要看得出「是真的沒有」，而不是「壞了」——
            // 沒有這個提示的話，展開一個空欄跟功能失效長得一模一樣
            if (slots.Count == 0)
            {
                Debug.Log($"[快捷欄] {name}：身上沒有標籤「{filterTag}」的道具，展開會是空的");
            }

            if (collapseAfter) SetExpanded(false, true);
        }

        // ==========================================
        public void SetExpanded(bool on, bool instant = false)
        {
            // ⚠️ **展開前一定要重讀背包。**
            //
            // 原本只在 OnEnable 建一次格子 —— 那是場景載入的時候，背包還是空的，
            // 所以永遠是 0 個格子，之後撿到東西也不會出現。
            // 症狀是「看得到欄位但點不到」，因為根本沒有東西可以點。
            //
            // 放在展開的時機而不是每幀 —— 只有要看的時候才重建，代價可以忽略
            if (on && !expanded) Refresh(false);

            expanded = on;

            if (reveal != null) { StopCoroutine(reveal); reveal = null; }

            if (!on)
            {
                for (int i = 0; i < slots.Count; i++) if (slots[i] != null) slots[i].gameObject.SetActive(false);
                HideTooltip();
                return;
            }

            if (instant || staggerSeconds <= 0f)
            {
                for (int i = 0; i < slots.Count; i++) if (slots[i] != null) slots[i].gameObject.SetActive(true);
                return;
            }

            reveal = StartCoroutine(RevealRoutine());
        }

        private IEnumerator RevealRoutine()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] != null) slots[i].gameObject.SetActive(true);
                yield return new WaitForSecondsRealtime(staggerSeconds);
            }
            reveal = null;
        }

        /// <summary>給按鈕綁的（openOnHover 取消勾選時用點的）。</summary>
        public void Toggle() => SetExpanded(!expanded);

        /// <summary>
        /// hover 判定掛在**整條欄**上，不是只掛收合圖示。
        ///
        /// 【為什麼】只掛圖示的話，滑鼠一往下移到展開的格子上就離開了圖示 ——
        /// 欄位會在你正要點的瞬間收起來。掛整條就不會有那個縫。
        /// 所以這個物件的 RectTransform 要**涵蓋收合圖示與展開區**，
        /// 而且要有一個 raycastTarget 的圖（可以是全透明）才收得到事件。
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (openOnHover) SetExpanded(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (openOnHover) SetExpanded(false);
        }

        // ==========================================
        private void HandleSlotHover(ShortcutSlotUI s)
        {
            if (!s.IsHovered) { HideTooltip(); return; }

            if (tooltipRoot == null) return;
            tooltipRoot.SetActive(true);

            if (tooltipTitle != null)
                tooltipTitle.text = s.Item != null ? s.Item.Label : "（沒登記的道具）";

            if (tooltipBody != null)
            {
                tooltipBody.text = s.Item != null && !string.IsNullOrEmpty(s.Item.description)
                    ? s.Item.description
                    : emptyText;
            }
        }

        private void HideTooltip()
        {
            if (tooltipRoot != null) tooltipRoot.SetActive(false);
        }

        private void HandleSlotClicked(ShortcutSlotUI s)
        {
            if (s.Item == null) return;

            // ⚠️ 目前沒有人訂閱 —— 「使用道具」那個動作還不存在。
            //    這則 Log 是刻意的：讓測試的人知道「點到了，但功能還沒接」，
            //    而不是以為自己點錯地方
            Debug.Log($"[快捷欄] 點了「{s.Item.Label}」—— 使用道具的動作還沒做，這一版只顯示不執行");
            OnItemUsed?.Invoke(s.Item);
        }
    }
}
