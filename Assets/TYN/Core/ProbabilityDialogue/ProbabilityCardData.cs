using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core.ProbabilityDialogue
{
    /// <summary>
    /// 機率卡。**這是 Romtyui 規格書 §7.3 的 ProbabilityCard。**
    ///
    /// ⚠️ 這一整個命名空間是**新機制，跟現有的探索打牌是兩套**。
    ///
    /// | | 探索打牌（`DialogueEncounterController`，已驗收） | 這一套 |
    /// |---|---|---|
    /// | 卡打在哪 | 目標上，出牌即判定 | **選項**上，只改機率、不判定 |
    /// | 判定時機 | 每出一張牌判定一次 | 出完牌後**才選一個選項**判定 |
    /// | 卡牌關係 | 屬性相剋決定**倍率** | colorId 命中就 **+value**（加法） |
    /// | 衰減 | 逐次衰減 | 沒有衰減，累加 |
    /// | 失敗 | 手牌用盡才結案 | 失敗就**移掉那個選項**，其餘繼續 |
    ///
    /// **刻意不共用 `DialogueEncounterController`** —— 改那一支會連帶弄壞
    /// 已經驗收的探索打牌。兩套並存，互不干擾。
    /// </summary>
    [CreateAssetMenu(fileName = "PCard_", menuName = "Eldritch/機率對話/卡牌")]
    public class ProbabilityCardData : ScriptableObject
    {
        [Tooltip("卡牌 unique ID")]
        public string cardId = "";

        [Tooltip("顏色 id。跟回答的 Accepted Color Ids 對應 —— **字串要一模一樣**。\n" +
                 "打錯字不會報錯，只會變成「這張牌對誰都沒效果」")]
        public string colorId = "";

        [Tooltip("命中同色回答時加上去的機率值。目前是整數（規格 R3）")]
        public int value = 10;

        [Tooltip("從卡池隨機時的權重")]
        [Min(0f)] public float weight = 1f;

        [Tooltip("卡面圖。規格書的 visualId —— 這裡直接放 Sprite，少一層查表")]
        public Sprite visual;

        [Tooltip("顯示用的顏色。色點與 Highlight 都用它 ——\n" +
                 "規格 §3.1：顏色關係一定要明顯，回答上要有跟卡牌相同的色點")]
        public Color displayColor = Color.white;

        [Tooltip("給人看的名字，不影響行為")]
        public string displayName = "";
    }

    /// <summary>
    /// 卡池。事件開始時從這裡隨機抽出手牌（規格 R1 / §7.1 cardPoolId）。
    ///
    /// 跟 `LootTable` / `EncounterPool` 同一句話：一份清單 → 依權重挑。
    /// </summary>
    [CreateAssetMenu(fileName = "PCardPool_", menuName = "Eldritch/機率對話/卡池")]
    public class ProbabilityCardPool : ScriptableObject
    {
        [Tooltip("這個池子可以抽到的卡")]
        public List<ProbabilityCardData> cards = new List<ProbabilityCardData>();

        /// <summary>
        /// 抽 count 張。**允許重複** —— 規格沒有說不能重複，
        /// 而且卡池小的時候不允許重複會抽不滿。
        /// </summary>
        public List<ProbabilityCardData> Deal(int count, System.Random rng)
        {
            var hand = new List<ProbabilityCardData>();
            if (rng == null || cards == null || cards.Count == 0) return hand;

            float total = 0f;
            for (int i = 0; i < cards.Count; i++)
                if (cards[i] != null && cards[i].weight > 0f) total += cards[i].weight;

            if (total <= 0f)
            {
                Debug.LogWarning(
                    $"[機率對話] 卡池「{name}」所有卡的權重都是 0，抽不出手牌。\n" +
                    "⚠️ Unity 新增 List 元素會零填充 —— 記得把 Weight 改成 1 以上。", this);
                return hand;
            }

            for (int n = 0; n < count; n++)
            {
                double roll = rng.NextDouble() * total;
                float acc = 0f;
                for (int i = 0; i < cards.Count; i++)
                {
                    if (cards[i] == null || cards[i].weight <= 0f) continue;
                    acc += cards[i].weight;
                    if (roll <= acc) { hand.Add(cards[i]); break; }
                }
            }

            return hand;
        }
    }
}
