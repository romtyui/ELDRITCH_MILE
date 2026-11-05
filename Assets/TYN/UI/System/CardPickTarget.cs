using UnityEngine;
using UnityEngine.EventSystems;

namespace EldritchMile.UI
{
    /// <summary>
    /// 卡冊挑選模式時掛在每一格牌上：點一下就是選它。（DeckBookUI.OpenPicker）
    /// 不是挑選模式時元件是關著的，點了沒反應，也不會擋掉卡冊原本的操作。
    /// </summary>
    public class CardPickTarget : MonoBehaviour, IPointerClickHandler
    {
        [System.NonSerialized] public DeckBookUI owner;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!enabled || owner == null) return;
            if (eventData.button != PointerEventData.InputButton.Left) return;
            owner.Picked(GetComponent<CardViewUI>());
        }
    }
}
