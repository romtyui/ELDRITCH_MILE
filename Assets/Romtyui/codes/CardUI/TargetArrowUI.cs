using UnityEngine;

public class TargetArrowUI : MonoBehaviour
{
    [Header("Arrow Parts")]
    public RectTransform arrowBody;
    public RectTransform arrowHead;

    [Header("Follow Mouse Settings")]
    [Tooltip("箭頭終點距離滑鼠下方多少像素。數值越大，箭頭頭部越靠下。")]
    public float mouseEndYOffset = 55f;

    [Tooltip("箭頭起點是否從卡牌上方出發。")]
    public Vector2 startOffset = new Vector2(0f, 80f);

    [Tooltip("如果箭頭圖片本身不是朝右，請調整這個角度。箭頭朝右 = 0，朝上 = -90，朝下 = 90。")]
    public float arrowRotationOffset = 0f;

    private RectTransform startCard;
    private Canvas canvas;
    private RectTransform canvasRect;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas.transform as RectTransform;
        Hide();
    }

    public void Show(RectTransform cardRect)
    {
        startCard = cardRect;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        startCard = null;
        gameObject.SetActive(false);
    }

    public void UpdateArrow(Vector2 mouseScreenPos)
    {
        if (startCard == null || canvasRect == null)
            return;

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        Vector2 startScreenPos = RectTransformUtility.WorldToScreenPoint(cam, startCard.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            startScreenPos,
            cam,
            out Vector2 startLocal
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            mouseScreenPos,
            cam,
            out Vector2 mouseLocal
        );

        // 起點：卡牌位置，可以稍微往上，避免從卡牌中心射出
        Vector2 finalStartLocal = startLocal + startOffset;

        // 終點：永遠在滑鼠下方
        Vector2 finalEndLocal = mouseLocal + Vector2.down * mouseEndYOffset;

        DrawArrow(finalStartLocal, finalEndLocal);
    }

    private void DrawArrow(Vector2 startLocal, Vector2 endLocal)
    {
        Vector2 dir = endLocal - startLocal;

        if (dir.sqrMagnitude <= 0.01f)
            return;

        float length = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        if (arrowBody != null)
        {
            arrowBody.anchoredPosition = startLocal + dir * 0.5f;
            arrowBody.sizeDelta = new Vector2(length, arrowBody.sizeDelta.y);
            arrowBody.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        if (arrowHead != null)
        {
            arrowHead.anchoredPosition = endLocal;
            arrowHead.localRotation = Quaternion.Euler(0f, 0f, angle + arrowRotationOffset);
        }
    }
}