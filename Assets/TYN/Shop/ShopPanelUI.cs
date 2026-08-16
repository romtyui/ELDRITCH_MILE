using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace EldritchMile.Shop
{
    using EldritchMile.Core;

    /// <summary>
    /// 商店的貨架。管一排格子、顯示錢、回報「玩家點了哪一格」。
    ///
    /// 【它不決定賣什麼】商品是 <see cref="LootService"/> 從 <see cref="LootTable"/> 抽出來的，
    /// 由 Stage 傳進來。這一層只負責「把資料擺到畫面上」。
    ///
    /// 【為什麼格子是預先擺好的，不是動態生成】貨架的格子要對齊背景圖上畫的層板，
    /// 那是美術決定的位置。動態生成的話位置由 LayoutGroup 算，
    /// 換一張背景圖就要重調程式。預先擺好 = 美術自己在 Scene 裡拖到對位。
    ///
    /// 格子不夠放的商品會被丟掉並警告 —— 分頁（PREV/NEXT）還沒做，見下方註記。
    /// </summary>
    public class ShopPanelUI : MonoBehaviour
    {
        [Header("格子")]
        [Tooltip("貨架上的格子，順序就是顯示順序。\n" +
                 "留空的話會在 Awake 自動抓子物件底下所有的 ShopSlotUI")]
        public List<ShopSlotUI> slots = new List<ShopSlotUI>();

        [Header("錢")]
        [Tooltip("顯示玩家身上的錢。可留空")]
        public TextMeshProUGUI moneyText;

        [Tooltip("錢的顯示格式。{0} = 數字")]
        public string moneyFormat = "{0}";

        /// 玩家點了一格（還沒判斷買不買得起）。
        public event Action<ShopSlotUI> OnSlotClicked;

        /// <summary>貨架上總共有幾格。Stage 靠它決定要抽幾件商品。</summary>
        public int SlotCount => slots.Count;

        private void Awake()
        {
            if (slots.Count == 0)
            {
                GetComponentsInChildren(true, slots);
            }

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] == null) continue;

                slots[i].OnClicked -= HandleSlotClicked;
                slots[i].OnClicked += HandleSlotClicked;
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] != null) slots[i].OnClicked -= HandleSlotClicked;
            }
        }

        /// <summary>
        /// 把一批商品擺上貨架。多出來的格子會清空。
        /// </summary>
        public void Bind(List<ItemStack> goods)
        {
            if (goods == null) goods = new List<ItemStack>();

            for (int i = 0; i < slots.Count; i++)
            {
                ShopSlotUI slot = slots[i];
                if (slot == null) continue;

                if (i < goods.Count && goods[i] != null && !string.IsNullOrEmpty(goods[i].id))
                {
                    ItemData data = GameFlowManager.Item(goods[i].id);

                    // 查不到資料時價格是 0 —— 白送比「無限貴」好，
                    // 至少玩家點得動，也看得出這筆資料沒設好
                    int price = data != null ? data.price : 0;

                    slot.Bind(goods[i].id, goods[i].count, price);
                }
                else
                {
                    slot.SetEmpty();
                }
            }

            if (goods.Count > slots.Count)
            {
                Debug.LogWarning(
                    $"[商店] 抽出了 {goods.Count} 件商品，但貨架只有 {slots.Count} 格。多的被丟掉了。\n" +
                    "分頁（PREV / NEXT）還沒做 —— 現在請讓 LootTable 抽的數量對齊格子數。");
            }

            RefreshAffordability();
        }

        /// <summary>依玩家現在的錢更新每一格的樣式，並更新錢的顯示。</summary>
        public void RefreshAffordability()
        {
            RunContext run = GameFlowManager.Instance != null ? GameFlowManager.Instance.Run : null;
            int money = run != null ? run.money : 0;

            if (moneyText != null) moneyText.text = string.Format(moneyFormat, money);

            for (int i = 0; i < slots.Count; i++)
            {
                ShopSlotUI slot = slots[i];
                if (slot == null || slot.IsEmpty || slot.SoldOut) continue;

                slot.SetAffordable(money >= slot.Price);
            }
        }

        public void HideAll()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] != null) slots[i].SetEmpty();
            }
        }

        /// <summary>貨架上還有沒有買得到的東西。全空了就可以讓 Stage 收尾。</summary>
        public bool HasStock()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                ShopSlotUI slot = slots[i];
                if (slot != null && !slot.IsEmpty && !slot.SoldOut) return true;
            }
            return false;
        }

        private void HandleSlotClicked(ShopSlotUI slot)
        {
            OnSlotClicked?.Invoke(slot);
        }
    }
}
