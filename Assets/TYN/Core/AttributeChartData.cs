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

        [Header("顏色")]
        [Tooltip("無（黑白）。純文字上用中性灰，純黑在深色底上看不見")]
        public Color noneColor = new Color(0.72f, 0.72f, 0.72f);

        [Tooltip("直覺（紅）")]
        public Color intuitionColor = new Color(0.86f, 0.34f, 0.32f);

        [Tooltip("邏輯（藍）")]
        public Color logicColor = new Color(0.36f, 0.60f, 0.86f);

        [Tooltip("批判與創造（綠）")]
        public Color insightColor = new Color(0.44f, 0.76f, 0.48f);

        /// <summary>
        /// 屬性的顏色。**放在這裡是因為這份資產已經是「屬性的事實」的家** ——
        /// 相剋表在這裡，顏色也該在這裡，否則卡框、選項關鍵字、tooltip 會各配一套而慢慢對不起來。
        ///
        /// 預設值是照牌框的色系抓的近似值，**要跟美術對一次**。
        /// </summary>
        public Color ColorOf(ExploreAttribute attr)
        {
            switch (attr)
            {
                case ExploreAttribute.Intuition: return intuitionColor;
                case ExploreAttribute.Logic: return logicColor;
                case ExploreAttribute.Insight: return insightColor;
                default: return noneColor;
            }
        }

        /// <summary>把一段文字包成 TMP 的顏色語法。給選項的關鍵字用。</summary>
        public string Colorize(string text, ExploreAttribute attr)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // ⚠️ 一定要用 RGB**不含 alpha** 的六碼。TMP 吃八碼，但八碼時
            //    alpha 會覆蓋 TMP 自己的淡入淡出，做動效時文字會不跟著透明度走
            return $"<color=#{ColorUtility.ToHtmlStringRGB(ColorOf(attr))}>{text}</color>";
        }

        public float GetMultiplier(ExploreAttribute card, ExploreAttribute target)
        {
            return GetMultiplier(GetEffectiveness(card, target));
        }
    }
}
