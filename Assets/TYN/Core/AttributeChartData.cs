using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// C17/C19：屬性相剋表。
    ///
    /// 刻意做成 ScriptableObject —— 因為屬性名稱(Q7a)與相剋關係都還沒定案，
    /// 放在資產裡就能在 Inspector 隨時調整，不必改程式重編譯。
    /// </summary>
    [CreateAssetMenu(fileName = "AttributeChart", menuName = "Eldritch/Attribute Chart")]
    public class AttributeChartData : ScriptableObject
    {
        [Serializable]
        public class Rule
        {
            public ExploreAttribute cardAttribute;
            public ExploreAttribute targetAttribute;
            public Effectiveness effectiveness = Effectiveness.Match;
        }

        [Header("倍率（C19 定案：1 / 0.5 / 0，沒有 2×）")]
        [Range(0f, 1f)] public float matchMultiplier = 1.0f;
        [Range(0f, 1f)] public float partialMultiplier = 0.5f;
        [Range(0f, 1f)] public float noneMultiplier = 0.0f;

        [Header("相剋規則")]
        [Tooltip("沒有列在這裡的組合，一律使用下方的預設值")]
        public List<Rule> rules = new List<Rule>();

        [Tooltip("查不到規則時的預設效果。設為 Partial 可避免未設定的組合意外變成完全無效")]
        public Effectiveness defaultEffectiveness = Effectiveness.Partial;

        public Effectiveness GetEffectiveness(ExploreAttribute card, ExploreAttribute target)
        {
            // 目標沒有屬性 = 不吃相剋，一律視為相符
            if (target == ExploreAttribute.None) return Effectiveness.Match;

            for (int i = 0; i < rules.Count; i++)
            {
                Rule r = rules[i];
                if (r.cardAttribute == card && r.targetAttribute == target)
                {
                    return r.effectiveness;
                }
            }

            // 同屬性視為相符，是最不容易出錯的預設
            if (card == target) return Effectiveness.Match;

            return defaultEffectiveness;
        }

        public float GetMultiplier(Effectiveness eff)
        {
            switch (eff)
            {
                case Effectiveness.Match: return matchMultiplier;
                case Effectiveness.Partial: return partialMultiplier;
                case Effectiveness.None: return noneMultiplier;
                default: return matchMultiplier;
            }
        }

        public float GetMultiplier(ExploreAttribute card, ExploreAttribute target)
        {
            return GetMultiplier(GetEffectiveness(card, target));
        }
    }
}
