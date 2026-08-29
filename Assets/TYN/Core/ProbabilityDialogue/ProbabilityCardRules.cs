using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core.ProbabilityDialogue
{
    /// <summary>
    /// 機率對話怎麼讀一張**探索牌**。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【⚠️ 沒有「機率牌」這種東西 —— 那是同一種牌】
    ///
    /// 機率對話用的牌與開寶箱用的牌是**同一個 `CardDataExplore`**，
    /// 差別只在玩法：
    ///
    /// | | 開寶箱／探索 | 機率對話 |
    /// |---|---|---|
    /// | 打在哪 | 目標上 | **回答**上 |
    /// | 判定時機 | 出牌即判定 | 出完牌**才**選回答判定 |
    /// | 屬性怎麼用 | 查相剋表決定**倍率** | 命中回答接受的屬性就**加機率** |
    /// | 失敗 | 手牌用盡才結案 | 移掉那個回答，其餘繼續 |
    ///
    /// 【為什麼要特別寫這一段】
    /// 早期版本另外做了一組 `ProbabilityCardData`（橘／藍、數值 10/25/40）。
    /// **那組顏色只是提案時的示意，不是設定**，數值也是我自己編的。
    /// 兩套牌並存會讓玩家的牌組跟對話用的牌無關 —— 那就不是套牌遊戲了。
    /// 已經移除，不要再加回來。
    ///
    /// 屬性與顏色一律以 <see cref="ExploreAttribute"/>（本我／超我／自我）
    /// 與 <see cref="AttributeChartData"/> 為準；牌面數字以卡片自己的
    /// `successProbability` 為準。
    /// </summary>
    public static class ProbabilityCardRules
    {
        /// <summary>
        /// 這張牌能替回答加幾個百分點。
        ///
        /// **牌面印的數字就是它** —— `successProbability` 是 0~1 的小數
        /// （0.4 = 40%），而牌面美術叫 `機率牌40`。兩者必須一致，
        /// 所以這裡只做單位換算，**不要在這裡加成或打折**。
        /// </summary>
        public static int ValueOf(CardDataExplore card)
        {
            if (card == null) return 0;
            return Mathf.RoundToInt(Mathf.Clamp01(card.successProbability) * 100f);
        }

        /// <summary>
        /// 把一張牌套到某個回答的目前機率上，回傳新的機率。
        ///
        /// ⚠️ **兩種成長公式只在這裡分岔** —— Session 不自己算，
        /// 不然「模擬用的算法」與「遊戲裡的算法」遲早會不一樣，
        /// 而那種不一致查起來非常痛苦（畫面上的數字對，實際判定卻不對）。
        ///
        /// 乘法會四捨五入到整數。**每一張牌各自進位一次**，不是最後才進 ——
        /// 玩家看得到每張牌打完的數字，累到最後才進位的話畫面與真值會對不上。
        /// </summary>
        public static int Apply(ProbabilityGrowth growth, int current, CardDataExplore card, int cap)
        {
            int value = ValueOf(card);

            int next = growth == ProbabilityGrowth.Multiplicative
                ? Mathf.RoundToInt(current * (1f + value / 100f))
                : current + value;

            return Mathf.Clamp(next, 0, cap);
        }

        /// <summary>
        /// 這張牌對這個回答有沒有效。
        ///
        /// `None`（黑白牌）對誰都有效 —— 那是 <see cref="ExploreAttribute.None"/>
        /// 一貫的語意（「不吃相剋，一律視為相符」），不是這裡開的特例。
        /// </summary>
        public static bool Affects(CardDataExplore card, List<ExploreAttribute> accepted)
        {
            if (card == null || accepted == null || accepted.Count == 0) return false;
            if (card.attribute == ExploreAttribute.None) return true;
            return accepted.Contains(card.attribute);
        }

        /// <summary>
        /// 從玩家的牌組發 count 張手牌。**允許重複** ——
        /// 牌組本來就可能有多張同名卡，而且牌組小的時候不允許重複會發不滿。
        /// </summary>
        public static List<CardDataExplore> Deal(IList<CardDataExplore> source, int count, System.Random rng)
        {
            var hand = new List<CardDataExplore>();
            if (source == null || source.Count == 0 || count <= 0 || rng == null) return hand;

            // 洗一份索引再依序取，這樣**牌組夠大時不會重複**，
            // 不夠時才從頭再繞 —— 直接每次隨機挑會常常發到三張一樣的
            var order = new List<int>();
            for (int i = 0; i < source.Count; i++) if (source[i] != null) order.Add(i);
            if (order.Count == 0) return hand;

            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int t = order[i]; order[i] = order[j]; order[j] = t;
            }

            for (int n = 0; n < count; n++) hand.Add(source[order[n % order.Count]]);
            return hand;
        }
    }
}
