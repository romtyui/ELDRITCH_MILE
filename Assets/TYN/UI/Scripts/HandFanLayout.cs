using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.UI
{
    /// <summary>
    /// 手牌怎麼排。**探索打牌與機率對話共用這一支。**
    ///
    /// ────────────────────────────────────────────────────────
    /// 【為什麼要抽出來】
    /// 這兩個環節本來各自寫了一份排版，於是卡片尺寸、間距、重疊程度全都不一樣 ——
    /// 明明畫的是同一組卡面美術（`機率牌XX無框` ＋ `機率牌框紅/藍/綠`）。
    /// 規則只留一份，數字由呼叫端給，兩邊就不會再漂開。
    ///
    /// 【三個從探索那邊繼承的決定】改之前先讀完，每一條都是踩出來的：
    ///
    /// 1. **根物件永遠在 y = 0，上浮交給呼叫端動視覺層。**
    ///    把根物件往上移的話，游標會被從卡片底下抽走 → exit → 落下 → enter，
    ///    在卡片下緣會瘋狂閃爍。所以這支**只排水平位置**，不碰高度。
    ///
    /// 2. **疊放順序永遠是左→右（右側在上），不隨 hover／選取改變。**
    ///    把 hover 的牌提到最上層會讓整排的前後關係在滑鼠掃過去時一直重排，
    ///    看起來像在跳。固定住之後結構是穩定的，狀態變化只由**高度**表達。
    ///
    /// 3. **間距比卡片窄 ＝ 略有重疊**，那正是實體手牌攤開來的樣子。
    ///    被 hover 的牌右半邊仍被鄰居壓著，只露出上緣 ——
    ///    覺得看不清楚就把上浮距離調大，**不要改疊放順序**。
    ///
    /// ⚠️ 用這支的容器**不可以掛 LayoutGroup** —— 兩邊都在算位置會打架。
    /// </summary>
    public static class HandFanLayout
    {
        /// <summary>
        /// 水平置中排列，手牌多時自動壓縮間距。
        /// </summary>
        /// <param name="cards">要排的卡片。順序就是左→右的順序。</param>
        /// <param name="cardSpacing">相鄰兩張的中心距。比卡片寬度小就會重疊。</param>
        /// <param name="maxHandWidth">整排的最大寬度。超過就壓縮間距，不會擠出畫面。</param>
        public static void Arrange(IList<RectTransform> cards, float cardSpacing, float maxHandWidth)
        {
            if (cards == null) return;

            int n = cards.Count;
            if (n == 0) return;

            float spacing = cardSpacing;
            if (n > 1 && spacing * (n - 1) > maxHandWidth) spacing = maxHandWidth / (n - 1);

            float startX = -spacing * (n - 1) * 0.5f;

            for (int i = 0; i < n; i++)
            {
                RectTransform rt = cards[i];
                if (rt == null) continue;

                // 只動 x。y 留給呼叫端的上浮處理（見上面的第 1 點）
                rt.anchoredPosition = new Vector2(startX + spacing * i, 0f);
                rt.SetSiblingIndex(i);
            }
        }

        /// <summary>
        /// 在卡片底下插一層 `__Visual`，把原本的子物件全部搬進去。
        /// **上浮動這一層，根物件不動** —— 理由見上面的第 1 點。
        ///
        /// ⚠️ 只對**純 `Instantiate` 出來的複本**有效。
        /// `PrefabUtility.InstantiatePrefab` 會保持 prefab 連結，不准重組結構，
        /// 子物件會搬不動而且不會報錯。
        /// </summary>
        public static RectTransform BuildVisualRoot(RectTransform cardRoot)
        {
            if (cardRoot == null) return null;

            Transform existing = cardRoot.Find("__Visual");
            if (existing != null) return existing as RectTransform;

            var go = new GameObject("__Visual", typeof(RectTransform));
            var visual = go.GetComponent<RectTransform>();

            visual.SetParent(cardRoot, false);
            visual.anchorMin = Vector2.zero;
            visual.anchorMax = Vector2.one;
            visual.offsetMin = Vector2.zero;
            visual.offsetMax = Vector2.zero;
            visual.localScale = Vector3.one;

            // ⚠️ 順序必須保住。UI 的疊放靠 sibling 順序，而 SetParent 是**附加到最後** ——
            //    倒著迭代（避開 childCount 變動）會把整疊圖層翻過來，
            //    卡框從最底跑到最上，把卡面整個蓋掉。所以先收集、再依原順序搬。
            var children = new List<Transform>();
            for (int i = 0; i < cardRoot.childCount; i++)
            {
                Transform child = cardRoot.GetChild(i);
                if (child != visual) children.Add(child);
            }

            for (int i = 0; i < children.Count; i++)
            {
                // worldPositionStays: false —— 保留相對於卡片的排版，不是保留世界座標
                children[i].SetParent(visual, false);
            }

            visual.SetAsFirstSibling();
            return visual;
        }
    }
}
