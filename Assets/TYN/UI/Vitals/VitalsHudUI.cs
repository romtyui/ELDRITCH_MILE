using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EldritchMile.Core;

namespace EldritchMile.UI
{
    /// <summary>
    /// 戰鬥以外的 HP／SAN 顯示。外觀是從戰鬥那一顆燈（`lamp_Panel/Root`）複製出來的。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【為什麼不直接沿用戰鬥的 BattleHUDUI】那一支讀的是 `BattleUnit` 與 `EnergySystem`，
    /// 兩者都只活在戰鬥 Stage 裡。戰鬥以外的數值在 `RunStateManager`，
    /// 我方的唯一入口是 <see cref="PlayerVitals"/>。
    ///
    /// 【哪些環節顯示】由同物件上的 `UIPanel.visibleInStages` 決定（目前只有 Explore）。
    /// 其他環節有位置衝突，待討論（2026-09-14）。
    ///
    /// 【為什麼每幀輪詢】PlayerVitals 沒有變動事件，而會改到它的地方很多
    /// （事件效果、吃東西、戰鬥回存）。只在數值真的變了才改 UI，成本可以忽略。
    /// </summary>
    public class VitalsHudUI : MonoBehaviour
    {
        [Header("元件")]
        [Tooltip("HP 條（Image Type = Filled）")]
        public Image hpFill;

        [Tooltip("SAN 條（Image Type = Filled）")]
        public Image sanFill;

        public TMP_Text hpText;
        public TMP_Text sanText;

        [Header("格式")]
        [Tooltip("{0} = 目前、{1} = 上限")]
        public string hpFormat = "{0}";

        [Tooltip("{0} = 目前、{1} = 上限")]
        public string sanFormat = "{0}";

        private int lastHp = int.MinValue, lastMaxHp, lastSan = int.MinValue, lastMaxSan;

        private void OnEnable()
        {
            // 重新顯示時一定要重畫 —— 隱藏期間數值可能變了
            lastHp = int.MinValue;
            lastSan = int.MinValue;
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            int hp = PlayerVitals.Hp, maxHp = PlayerVitals.MaxHp;
            int san = PlayerVitals.San, maxSan = PlayerVitals.MaxSan;

            if (hp != lastHp || maxHp != lastMaxHp)
            {
                lastHp = hp; lastMaxHp = maxHp;
                if (hpFill != null) hpFill.fillAmount = maxHp > 0 ? Mathf.Clamp01((float)hp / maxHp) : 0f;
                if (hpText != null) hpText.text = string.Format(hpFormat, hp, maxHp);
            }

            if (san != lastSan || maxSan != lastMaxSan)
            {
                lastSan = san; lastMaxSan = maxSan;
                if (sanFill != null) sanFill.fillAmount = maxSan > 0 ? Mathf.Clamp01((float)san / maxSan) : 0f;
                if (sanText != null) sanText.text = string.Format(sanFormat, san, maxSan);
            }
        }
    }
}
