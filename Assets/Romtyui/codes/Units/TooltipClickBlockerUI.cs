using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipClickBlockerUI : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        TooltipTriggerUI.CloseCurrentClickedTooltip();
        gameObject.SetActive(false);
    }
}