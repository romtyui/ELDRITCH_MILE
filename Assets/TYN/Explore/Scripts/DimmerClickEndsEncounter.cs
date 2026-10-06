using UnityEngine;
using UnityEngine.EventSystems;
using EldritchMile.Core;

namespace EldritchMile.Explore
{
    /// <summary>
    /// 打牌時點「空白處」就結束打牌。掛在對話框後面那層全螢幕壓黑（`DialogueUI/BLACK`）上。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【為什麼掛在壓黑層】（2026-09-15：和寶箱打牌時點非圖、非對話框、非牌區的地方就離開，省掉離開鍵）
    /// 打牌時畫面由下往上是：壓黑 → 對象大圖／對話框 → 手牌區。
    /// 點在後三者上會被它們自己收走；**穿過它們落到壓黑層上的，就是「空白處」**。
    /// 不必自己算範圍，Unity 的 raycast 順序已經算好了。
    ///
    /// 【選著牌時第一下只取消選取】兩段式出牌是「點牌 → 點目標」。
    /// 玩家選了牌、想點目標卻點偏，直接結束的話整個打牌環節就沒了 —— 代價太重。
    /// 所以選著牌時，點空白處先放下那張牌；沒選牌時才結束。
    /// </summary>
    public class DimmerClickEndsEncounter : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            DialogueEncounterController enc = DialogueEncounterController.Instance;
            if (enc == null || !enc.IsActive) return;

            // 只在探索（寶箱）生效，而且探索那邊要開著這個選項
            ExploreStageController stage = FindFirstObjectByType<ExploreStageController>();
            if (stage == null || !stage.clickOutsideEndsEncounter) return;

            ExploreHandUI hand = ExploreHandUI.Instance;
            if (hand != null && hand.IsAnyCardDragging) return;

            if (hand != null && hand.SelectedCard != null)
            {
                hand.ToggleSelect(hand.SelectedCard);
                return;
            }

            enc.EndEncounter();
        }
    }
}
