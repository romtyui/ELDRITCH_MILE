using UnityEngine;
using UnityEngine.EventSystems; // 必須引入此命名空間才能偵測滑鼠事件

public class BookmarkHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private RectTransform rectTransform;

    [Header("座標設定 (相對錨點)")]
    [Tooltip("縮在邊緣時的 Y 座標 (正數代表往上藏)")]
    public float hiddenY = 80f; 
    [Tooltip("降下來完整顯示時的 Y 座標")]
    public float shownY = 0f;

    [Header("動畫設定")]
    [Tooltip("降下與縮回的滑順速度")]
    public float moveSpeed = 10f;

    private float targetY;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // 遊戲開始時，強制先設定在隱藏位置
        targetY = hiddenY;
        Vector2 pos = rectTransform.anchoredPosition;
        pos.y = hiddenY;
        rectTransform.anchoredPosition = pos;
    }

    void Update()
    {
        // 使用 Lerp (線性插值) 讓 Y 座標平滑移動到目標位置
        Vector2 currentPos = rectTransform.anchoredPosition;
        currentPos.y = Mathf.Lerp(currentPos.y, targetY, Time.deltaTime * moveSpeed);
        rectTransform.anchoredPosition = currentPos;
    }

    // 當滑鼠「進入」圖片區域時觸發
    public void OnPointerEnter(PointerEventData eventData)
    {
        targetY = shownY;
    }

    // 當滑鼠「離開」圖片區域時觸發
    public void OnPointerExit(PointerEventData eventData)
    {
        targetY = hiddenY;
    }
}