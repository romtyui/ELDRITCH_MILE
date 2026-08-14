using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// C4/C17/C18/C19：全專案唯一的機率判定服務。
    ///
    /// 【為什麼要統一】舊碼有兩套互不相容的公式：
    ///   DialogueOptionInteractable → Clamp01(base × hiddenMultiplier)
    ///   EnemyInteractable          → Clamp01(base − fails × penalty)
    /// 兩者都與企劃不符，而且行為不一致。兩個檔案都已封存。
    /// </summary>
    public class ProbabilityCheck : MonoBehaviour
    {
        public static ProbabilityCheck Instance { get; private set; }

        [Header("相剋表")]
        public AttributeChartData chart;

        [Header("除錯")]
        [Tooltip("勾選後每次判定都會輸出詳細計算過程")]
        public bool verboseLog = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// 【純計算，無副作用】hover 預覽與實際擲骰都必須呼叫這支。
        ///
        /// ⚠️ 兩者共用同一函式是硬性要求。若預覽自己算一套、擲骰再算一套，
        ///    就會出現「顯示 50% 實際卻不是」的經典 bug，玩家會覺得遊戲在騙人。
        ///
        /// 計算式：Clamp01(卡片基礎機率 × 相剋倍率 × 目標當前衰減倍率)
        /// </summary>
        public float CalculateRate(CardDataExplore card, IProbabilityTarget target, out Effectiveness eff)
        {
            eff = Effectiveness.Match;

            if (card == null || target == null) return 0f;

            float baseRate = Mathf.Clamp01(card.successProbability);

            float attrMultiplier = 1f;
            if (chart != null)
            {
                eff = chart.GetEffectiveness(GetCardAttribute(card), target.Attribute);
                attrMultiplier = chart.GetMultiplier(eff);
            }

            float decay = Mathf.Clamp01(target.CurrentDecayMultiplier);

            return Mathf.Clamp01(baseRate * attrMultiplier * decay);
        }

        public float CalculateRate(CardDataExplore card, IProbabilityTarget target)
        {
            return CalculateRate(card, target, out _);
        }

        /// <summary>
        /// 【實際擲骰】C18②：一次只吃一張卡，不是清單。
        ///
        /// 「可以使用多張，機率不疊加」的真正意思是：每回合出一張、可以連續出，
        /// 但機率不會相加 —— 不是同時投入多張合成一個機率。
        ///
        /// 【呼叫端責任】本方法只負責擲骰，不會有任何副作用。
        /// 消耗手牌、target.ApplyDecay()、target.OnCheckResult() 由
        /// DialogueEncounterController 負責，順序不可顛倒。
        /// </summary>
        public bool Roll(CardDataExplore card, IProbabilityTarget target, out float finalRate)
        {
            Effectiveness eff;
            finalRate = CalculateRate(card, target, out eff);

            // 無效組合直接失敗，不浪費一次擲骰
            if (eff == Effectiveness.None || finalRate <= 0f)
            {
                if (verboseLog)
                {
                    Debug.Log($"[判定] {card?.cardName} → {target?.DisplayName}：屬性無效，直接失敗");
                }
                return false;
            }

            float roll = Random.value;
            bool success = roll <= finalRate;

            if (verboseLog)
            {
                Debug.Log(
                    $"[判定] {card.cardName} → {target.DisplayName}｜" +
                    $"基礎 {card.successProbability:P0} × 相剋 {eff} × 衰減 {target.CurrentDecayMultiplier:F2}" +
                    $" = {finalRate:P0}｜擲出 {roll:F2} → " +
                    (success ? "<color=green>成功</color>" : "<color=red>失敗</color>")
                );
            }

            return success;
        }

        public bool Roll(CardDataExplore card, IProbabilityTarget target)
        {
            return Roll(card, target, out _);
        }

        private ExploreAttribute GetCardAttribute(CardDataExplore card)
        {
            return card != null ? card.attribute : ExploreAttribute.None;
        }
    }
}
