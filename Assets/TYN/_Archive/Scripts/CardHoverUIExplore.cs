using UnityEngine;
using UnityEngine.EventSystems;

// ⚠️ 封存的舊碼。包進 namespace 是為了**不要污染全域命名空間** ——
//    這裡宣告的 RunNodeData / MapData / ICardInteractable 等等
//    與正式碼同名，放在全域會蓋掉正式那組，症狀是新檔案裡出現
//    「MapData 沒有 GetNode」這種完全不會指向這裡的錯誤。
namespace EldritchMile.Archive
{

public class CardHoverUIExplore : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler 
{
    // 如果你之後複製了 HandFanLayout 變成 HandFanLayoutExplore，可以解除註解
    // private HandFanLayoutExplore handLayout;
    private CardDragUIExplore cardDrag;

    private void Awake()
    {
        // handLayout = GetComponentInParent<HandFanLayoutExplore>();
        cardDrag = GetComponent<CardDragUIExplore>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // if (handLayout == null) return;
        if (cardDrag != null && cardDrag.IsDragging) return;

        // 告知排版管理器，這張卡被 Hover，讓它放大或浮出
        // handLayout.SetHover(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // if (handLayout == null) return;
        if (cardDrag != null && cardDrag.IsDragging) return;

        // 告知排版管理器取消 Hover 效果
        // handLayout.ClearHover(this);
    }
}
}
