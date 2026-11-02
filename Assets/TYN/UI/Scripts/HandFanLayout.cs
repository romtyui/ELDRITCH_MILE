using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    ///
    /// 【2026-11-02 改成扇形】寶箱與遭遇的手牌改成跟戰鬥一樣：畫面正下方、扇形、
    /// 滑上去的那張轉正放大並把兩邊推開。公式抄 Romtyui 的 `HandFanLayout`
    /// （`Romtyui/codes/CardUI`，綁他的 CardHoverUI 所以不能直接掛）。
    /// 上面第 1 點照舊（根物件不往上移，游標不會被抽走）；第 2 點只差在**被指到的那張會蓋到最上面**，
    /// 扇形重疊得比較多，不提上來就看不到整張。
    /// **出牌＝拖出牌區**，範圍見 <see cref="LeftHandZone"/>。
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
        /// 扇形排列（戰鬥手牌的公式）。只動根物件的位置、旋轉與疊放順序；被指到的那張怎麼浮起來見 <see cref="PoseVisual"/>。
        /// </summary>
        /// <param name="cards">要排的卡片（拖曳中的不要放進來）。順序就是左→右。</param>
        /// <param name="activeIndex">被指到的那張。-1 ＝ 沒有。</param>
        /// <param name="curveHeight">兩側下彎多少。戰鬥的預設值是 60。</param>
        /// <param name="angleStep">相鄰兩張差幾度。戰鬥的預設值是 6。</param>
        /// <param name="pushDistance">被指到時兩邊的牌讓開多遠。</param>
        public static void ArrangeFan(IList<RectTransform> cards, int activeIndex, float cardSpacing, float maxHandWidth,
                                      float curveHeight, float angleStep, float pushDistance)
        {
            if (cards == null) return;

            int n = cards.Count;
            if (n == 0) return;

            float spacing = cardSpacing;
            if (n > 1 && spacing * (n - 1) > maxHandWidth) spacing = maxHandWidth / (n - 1);

            float center = (n - 1) * 0.5f;

            for (int i = 0; i < n; i++)
            {
                RectTransform rt = cards[i];
                if (rt == null) continue;

                float offset = i - center;
                float x = offset * spacing;
                if (activeIndex >= 0 && i != activeIndex) x += i < activeIndex ? -pushDistance : pushDistance;

                rt.anchoredPosition = new Vector2(x, -(offset * offset) * curveHeight / 10f);
                rt.localRotation = Quaternion.Euler(0f, 0f, -offset * angleStep);
                rt.SetSiblingIndex(i);
            }

            if (activeIndex >= 0 && activeIndex < n && cards[activeIndex] != null) cards[activeIndex].SetAsLastSibling();
        }

        /// <summary>
        /// 視覺層的姿勢。被指到 ＝ 轉正、放大、浮到 <paramref name="hoverY"/>（手牌區座標，每張都浮到同一個高度）；
        /// 其他 ＝ 貼回根物件。根物件是斜的，所以這裡要把旋轉抵銷掉。
        /// </summary>
        public static void PoseVisual(RectTransform visual, RectTransform cardRoot, bool active, float hoverY, float hoverScale)
        {
            if (visual == null || cardRoot == null) return;

            if (!active)
            {
                visual.anchoredPosition = Vector2.zero;
                visual.localRotation = Quaternion.identity;
                visual.localScale = Vector3.one;
                return;
            }

            Quaternion undo = Quaternion.Inverse(cardRoot.localRotation);
            visual.anchoredPosition = undo * new Vector3(0f, hoverY - cardRoot.anchoredPosition.y, 0f);
            visual.localRotation = undo;
            visual.localScale = Vector3.one * hoverScale;
        }

        /// <summary>
        /// 這一點在牌區外面了嗎（放開就算出牌）。
        ///
        /// 【範圍】牌區 ＝ 畫面下方 <paramref name="zoneTopRatio"/> 這一段，整個畫面寬。**只看上緣**，不看左右：
        /// 打牌的動作一定是往上拖；往旁邊拖多半是在猶豫，一張牌的代價（消耗＋衰減）不該賠在這上面。
        /// 預設 1/3，剛好比滑上去放大的牌頂再高一點 —— 把牌從手上抽出來就過線了，放回手牌區就是取消。
        /// </summary>
        public static bool LeftHandZone(Vector2 screenPoint, float zoneTopRatio)
        {
            return screenPoint.y > Screen.height * zoneTopRatio;
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

            // 透明的可點區域跟著視覺層走：牌浮起來之後，游標移到上半截也還算指在這張牌上
            Image hit = go.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;

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
