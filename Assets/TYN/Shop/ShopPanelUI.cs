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

        [Header("說明框（hover 商品時）")]
        [Tooltip("整個框。留空則不顯示說明。\n" +
                 "⚠️ 框本身不能擋 raycast（CanvasGroup.blocksRaycasts = false）——\n" +
                 "擋到的話框一出現就蓋住格子，格子收到 exit，框消失，然後又 enter……會一直閃")]
        public RectTransform tooltipPanel;
        public TextMeshProUGUI tooltipTitle;

        [Tooltip("道具說明。留空則只顯示名稱")]
        public TextMeshProUGUI tooltipBody;

        [Tooltip("框的底邊離格子頂端多遠")]
        public float tooltipGap = 12f;

        [Tooltip("與畫面邊緣至少保留多少距離")]
        public float tooltipScreenPadding = 16f;

        private ShopSlotUI tooltipSlot;

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
                slots[i].OnHoverChanged -= HandleSlotHover;
                slots[i].OnHoverChanged += HandleSlotHover;
            }

            HideTooltip();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] == null) continue;
                slots[i].OnClicked -= HandleSlotClicked;
                slots[i].OnHoverChanged -= HandleSlotHover;
            }
        }

        // ==========================================
        // 說明框
        // ==========================================

        private void HandleSlotHover(ShopSlotUI slot, bool on)
        {
            if (tooltipPanel == null) return;

            if (!on)
            {
                // 只收「自己那一格」的框 —— 從 A 移到 B 時，B 的 enter 可能比 A 的 exit 先到
                if (tooltipSlot == slot) HideTooltip();
                return;
            }

            if (slot == null || slot.IsEmpty) return;

            ItemData data = GameFlowManager.Item(slot.ItemId);

            if (tooltipTitle != null)
                tooltipTitle.text = data != null ? data.Label : slot.ItemId;

            if (tooltipBody != null)
            {
                string body = data != null ? data.description : "";
                tooltipBody.text = body;
                tooltipBody.gameObject.SetActive(!string.IsNullOrEmpty(body));
            }

            tooltipSlot = slot;
            tooltipPanel.gameObject.SetActive(true);
            tooltipPanel.SetAsLastSibling();

            // 先讓 layout 定出尺寸再定位，不然會用到上一件商品的框大小
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipPanel);
            PlaceTooltipAbove(slot.transform as RectTransform);
        }

        private void HideTooltip()
        {
            tooltipSlot = null;
            if (tooltipPanel != null) tooltipPanel.gameObject.SetActive(false);
        }

        /// <summary>
        /// 擺在格子正上方；上面放不下就換到下方；左右夾進畫面。
        /// 與地圖的 MapTooltipUI.RepositionAbove 同一套算法（用世界座標設定，與框的錨點無關）。
        /// </summary>
        private void PlaceTooltipAbove(RectTransform target)
        {
            if (target == null) return;

            Canvas canvas = tooltipPanel.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            RectTransform canvasRect = canvas.rootCanvas.transform as RectTransform;

            Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRect, target);
            Vector2 size = tooltipPanel.rect.size;
            Rect area = canvasRect.rect;
            float pad = tooltipScreenPadding;

            float left = (b.min.x + b.max.x) * 0.5f - size.x * 0.5f;
            float bottom = b.max.y + tooltipGap;

            if (bottom + size.y > area.yMax - pad)
                bottom = b.min.y - tooltipGap - size.y;

            left = Mathf.Clamp(left, area.xMin + pad, area.xMax - size.x - pad);
            bottom = Mathf.Clamp(bottom, area.yMin + pad, area.yMax - size.y - pad);

            Vector2 pivotLocal = new Vector2(
                left + size.x * tooltipPanel.pivot.x,
                bottom + size.y * tooltipPanel.pivot.y);
            tooltipPanel.position = canvasRect.TransformPoint(pivotLocal);
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
