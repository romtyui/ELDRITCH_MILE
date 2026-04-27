using UnityEngine;

public class TargetArrowUI : MonoBehaviour
{
    public RectTransform arrowBody;
    public RectTransform arrowHead;

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
        if (startCard == null || canvasRect == null) return;

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        Vector2 startPos = RectTransformUtility.WorldToScreenPoint(cam, startCard.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, startPos, cam, out Vector2 startLocal);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mouseScreenPos, cam, out Vector2 endLocal);

        Vector2 dir = endLocal - startLocal;
        float length = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        arrowBody.anchoredPosition = startLocal + dir * 0.5f;
        arrowBody.sizeDelta = new Vector2(length, arrowBody.sizeDelta.y);
        arrowBody.localRotation = Quaternion.Euler(0f, 0f, angle);

        arrowHead.anchoredPosition = endLocal;
        arrowHead.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}
