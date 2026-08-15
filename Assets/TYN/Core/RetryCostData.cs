using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 物件的大類型。決定重試代價的固有倍率。
    ///
    /// 【暫定分類】目前物件太少，硬要設計一套分類機制會變成空想。
    /// 先用五級倍率頂著，等物件數量夠了再整理成真正的分類（屆時這個 enum 會被取代）。
    /// </summary>
    public enum ObjectTier
    {
        /// ×1 一般物件（普通木箱之類）
        Tier1 = 0,

        /// ×2
        Tier2 = 1,

        /// ×3
        Tier3 = 2,

        /// ×5
        Tier4 = 3,

        /// ×20 —— 代價高到「再試一次」本身就是一個重大決定
        TacticalDeath = 4,
    }

    /// <summary>
    /// 重試代價的計算規則。
    ///
    /// 【為什麼做成 ScriptableObject】跟 <see cref="AttributeChartData"/> 同一個理由：
    /// 級距與遞增規則都還沒定案，放在資產裡就能在 Inspector 隨時調，不必改程式重編譯。
    /// 分類機制日後要重整，也只會動到這一個檔。
    ///
    /// 【公式】總代價 = 大類型倍率 × 基礎代價的遞增結果
    /// </summary>
    [CreateAssetMenu(fileName = "RetryCost", menuName = "Eldritch/Retry Cost")]
    public class RetryCostData : ScriptableObject
    {
        /// <summary>基礎代價怎麼隨重試次數增加。</summary>
        public enum IncrementMode
        {
            /// 每次固定加上 Increment Amount（5 → 10 → 15 → 20）
            Fixed = 0,

            /// 每次乘上 Increment Amount（5 → 10 → 20 → 40，Amount = 2 時）
            Multiply = 1,

            /// 不遞增，每次都是基礎代價
            None = 2,
        }

        [Header("大類型固有倍率")]
        [Tooltip("一般物件。目前測試用的普通寶箱是這一級")]
        public float tier1Multiplier = 1f;

        public float tier2Multiplier = 2f;
        public float tier3Multiplier = 3f;
        public float tier4Multiplier = 5f;

        [Tooltip("代價高到「要不要再試一次」本身就是一個重大決定")]
        public float tacticalDeathMultiplier = 20f;

        [Header("基礎代價與遞增")]
        [Tooltip("第一次重試的基礎代價（還沒乘上大類型倍率）")]
        [Min(0f)] public float baseCost = 5f;

        [Tooltip("遞增方式。暫定 Fixed（固定 +5）")]
        public IncrementMode incrementMode = IncrementMode.Fixed;

        [Tooltip("Fixed 時是「每次加多少」，Multiply 時是「每次乘多少」。\n" +
                 "Multiply 模式下設 2 就是每次翻倍")]
        [Min(0f)] public float incrementAmount = 5f;

        public float GetTierMultiplier(ObjectTier tier)
        {
            switch (tier)
            {
                case ObjectTier.Tier1: return tier1Multiplier;
                case ObjectTier.Tier2: return tier2Multiplier;
                case ObjectTier.Tier3: return tier3Multiplier;
                case ObjectTier.Tier4: return tier4Multiplier;
                case ObjectTier.TacticalDeath: return tacticalDeathMultiplier;
                default: return tier1Multiplier;
            }
        }

        /// <summary>
        /// 這一次重試要付多少。
        /// </summary>
        /// <param name="tier">物件大類型</param>
        /// <param name="retryIndex">**第幾次重試，從 0 開始**。0 = 第一次重試</param>
        public int Calculate(ObjectTier tier, int retryIndex)
        {
            if (retryIndex < 0) retryIndex = 0;

            float raw;

            switch (incrementMode)
            {
                case IncrementMode.Fixed:
                    raw = baseCost + incrementAmount * retryIndex;
                    break;

                case IncrementMode.Multiply:
                    raw = baseCost * Mathf.Pow(incrementAmount, retryIndex);
                    break;

                default:
                    raw = baseCost;
                    break;
            }

            // 代價是要扣在 HP／SAN 這種整數資源上的，這裡就取整，
            // 免得 UI 顯示 7.5 但實際扣 7 或 8 —— 玩家會覺得數字在騙人
            return Mathf.Max(0, Mathf.RoundToInt(raw * GetTierMultiplier(tier)));
        }
    }
}
