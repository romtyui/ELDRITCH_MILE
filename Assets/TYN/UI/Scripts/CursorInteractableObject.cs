using UnityEngine;

public class CursorInteractableObject : MonoBehaviour
{
    [Header("懸浮時的鼠標狀態")]
    public CursorType hoverCursor;

    [Header("長按/點擊時的鼠標狀態 (可選)")]
    public CursorType holdCursor;
    
    // 是否正在被長按
    private bool isHolding = false;

    // 滑鼠進入物件範圍時觸發
    private void OnMouseEnter()
    {
        if (!isHolding)
        {
            CursorManager.Instance.SetCursor(hoverCursor);
        }
    }

    // 滑鼠離開物件範圍時觸發
    private void OnMouseExit()
    {
        isHolding = false;
        // 恢復為預設狀態
        CursorManager.Instance.SetCursor(CursorType.Idle);
    }

    // 滑鼠在物件上按下時觸發 (長按開始)
    private void OnMouseDown()
    {
        isHolding = true;
        // 如果有設定長按鼠標，就切換過去
        if (holdCursor != CursorType.Idle)
        {
            CursorManager.Instance.SetCursor(holdCursor);
        }
        
        // 這裡可以加入你打開寶箱的邏輯
        Debug.Log("正在互動：" + gameObject.name);
    }

    // 滑鼠在物件上放開時觸發 (長按結束)
    private void OnMouseUp()
    {
        isHolding = false;
        // 放開後，因為滑鼠還在物件上，所以變回懸浮狀態
        CursorManager.Instance.SetCursor(hoverCursor);
    }
}