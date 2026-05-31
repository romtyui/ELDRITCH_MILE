using UnityEngine;
using UnityEngine.EventSystems;

public class BattleTargetUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public BattleUnit battleUnit;

    public static BattleTargetUI CurrentHoveredTarget { get; private set; }

    public void OnPointerEnter(PointerEventData eventData)
    {
        CurrentHoveredTarget = this;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (CurrentHoveredTarget == this)
            CurrentHoveredTarget = null;
    }
}