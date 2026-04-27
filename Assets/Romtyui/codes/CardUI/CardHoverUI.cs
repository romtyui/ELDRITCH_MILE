using UnityEngine;
using UnityEngine.EventSystems;


public class CardHoverUI : MonoBehaviour,IPointerEnterHandler, IPointerExitHandler //, IPointerClickHandler
{
    private HandFanLayout handLayout;
    private CardDragUI cardDrag;

    private void Awake()
    {
        handLayout = GetComponentInParent<HandFanLayout>();
        cardDrag = GetComponent<CardDragUI>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (handLayout == null) return;
        if (cardDrag != null && cardDrag.IsDragging) return;

        handLayout.SetHover(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (handLayout == null) return;
        if (cardDrag != null && cardDrag.IsDragging) return;

        handLayout.ClearHover(this);
    }
}
