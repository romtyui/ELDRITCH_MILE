using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 管理對話框裡那一排選項（`answer_1/2/3`）。
    ///
    /// 【為什麼要有這一層】三個 Stage 都要用同一組選項物件：
    ///   · 對話 —— 選項是可以用機率卡打的判定目標
    ///   · 商店 —— 選項是商品，點了就買
    ///   · 特殊事件 —— 選項是可挑的牌
    ///
    /// 讓每個 Stage 各自去抓 `answer_1/2/3` 會變成三份幾乎一樣的程式，
    /// 而且「現在有幾個選項要顯示」這種狀態會散在三個地方。
    ///
    /// 【常駐場景】選項物件住在對話框裡（場景），Stage 住在 prefab ——
    /// prefab 不能在 Inspector 引用場景物件，所以走單例讓 Stage 找得到。
    /// 與 `PopupService` / `ExploreHandUI` 是同一個模式。
    /// </summary>
    public class DialogueOptionsPanel : MonoBehaviour
    {
        public static DialogueOptionsPanel Instance { get; private set; }

        [Header("繫結")]
        [Tooltip("整排選項的開關對象（通常是 option_box）。留空則用自身")]
        public GameObject root;

        [Tooltip("三個選項槽，順序就是畫面上的順序。\n" +
                 "⚠️ 數量就是「一次最多能顯示幾個選項」—— 目前美術只有三格")]
        public List<DialogueOptionUI> slots = new List<DialogueOptionUI>();

        /// 玩家點了某個選項（尚未決定要怎麼處理）。由 Stage 訂閱。
        public event Action<DialogueOptionUI> OnOptionClicked;

        /// 某個選項的判定結束了。由 Stage 訂閱。
        public event Action<DialogueOptionUI, bool, float> OnOptionResolved;

        public int SlotCount => slots.Count;

        /// 目前顯示中的選項（依畫面順序）。
        public IReadOnlyList<DialogueOptionUI> Active => active;

        private readonly List<DialogueOptionUI> active = new List<DialogueOptionUI>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            if (root == null) root = gameObject;

            // 選項槽在 Awake 就把事件接好 —— 它們不會被生滅，只會顯示／隱藏
            for (int i = 0; i < slots.Count; i++)
            {
                DialogueOptionUI slot = slots[i];
                if (slot == null) continue;

                slot.OnClicked -= HandleClicked;
                slot.OnResolved -= HandleResolved;

                slot.OnClicked += HandleClicked;
                slot.OnResolved += HandleResolved;
            }

            HideAll();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// 顯示一組選項。回傳實際顯示出來的（超過槽數的會被丟掉並警告）。
        /// </summary>
        public IReadOnlyList<DialogueOptionUI> Show(
            IReadOnlyList<string> texts,
            IReadOnlyList<ExploreAttribute> attributes,
            DialogueOptionUI.Mode mode)
        {
            HideAll();

            if (texts == null || texts.Count == 0) return active;

            if (texts.Count > slots.Count)
            {
                Debug.LogWarning(
                    $"[選項] 要顯示 {texts.Count} 個選項，但只有 {slots.Count} 個槽 —— " +
                    "多的會被忽略。要更多選項就得先增加 answer_N 的美術與物件。", this);
            }

            int n = Mathf.Min(texts.Count, slots.Count);

            for (int i = 0; i < n; i++)
            {
                DialogueOptionUI slot = slots[i];
                if (slot == null) continue;

                ExploreAttribute attr = attributes != null && i < attributes.Count
                    ? attributes[i]
                    : ExploreAttribute.None;

                slot.Bind(i, texts[i], attr, mode);
                active.Add(slot);
            }

            // ⚠️ 必須擋住 DialogueBoxUI.Open() 對選項框的自動關閉，
            //    否則玩家出第一張牌（會即時替換正文）的瞬間選項就整排消失了
            SetHold(true);

            if (root != null) root.SetActive(true);
            return active;
        }

        public void HideAll()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] != null) slots[i].Dismiss();
            }

            active.Clear();

            SetHold(false);
            if (root != null) root.SetActive(false);
        }

        private void SetHold(bool value)
        {
            DialogueBoxUI box = PopupService.Instance != null ? PopupService.Instance.dialogueBox : null;
            if (box != null) box.HoldOptions = value;
        }

        /// <summary>C18①：把某個選項標成主要目標，其餘取消。傳 null 則全部取消。</summary>
        public void SetSelected(DialogueOptionUI target)
        {
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i] != null) active[i].SetSelected(active[i] == target);
            }
        }

        private void HandleClicked(DialogueOptionUI option) => OnOptionClicked?.Invoke(option);

        private void HandleResolved(DialogueOptionUI option, bool success, float rate)
            => OnOptionResolved?.Invoke(option, success, rate);
    }
}
