using UnityEngine;
using UnityEngine.EventSystems;

public class BattleTargetUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public BattleUnit battleUnit;

    private static BattleTargetUI currentHoveredTarget;

    public static BattleTargetUI CurrentHoveredTarget
    {
        get
        {
            if (currentHoveredTarget == null) return null;
            if (!currentHoveredTarget.isActiveAndEnabled) return null;
            if (!CanDirectlyTarget(currentHoveredTarget.battleUnit)) return null;

            return currentHoveredTarget;
        }
    }

    public static bool CanDirectlyTarget(BattleUnit unit)
    {
        if (unit == null) return false;
        if (!unit.gameObject.activeInHierarchy || unit.currentHp <= 0) return false;
        if (unit is EnemyUnit && unit.HasStatus(StatusType.Lurking)) return false;

        return true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        currentHoveredTarget = this;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (currentHoveredTarget == this) currentHoveredTarget = null;
    }

    private void OnDisable()
    {
        if (currentHoveredTarget == this) currentHoveredTarget = null;
    }
}