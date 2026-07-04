using UnityEngine;

public class TargetArrowUI : MonoBehaviour
{
    public static TargetArrowUI Instance { get; private set; }

    [Header("Root")]
    [Tooltip("負責顯示/隱藏整組箭頭。通常是 ArrowRoot 或 ArrowVisualRoot。")]
    public RectTransform visualRoot;

    [Tooltip("負責座標換算的全螢幕 RectTransform。建議是 ArrowCanvasRoot。")]
    public RectTransform positionRoot;

    [Header("Arrow Parts")]
    public RectTransform arrowBody;
    public RectTransform arrowHead;

    [Header("Follow Mouse Settings")]
    public float mouseEndYOffset = 55f;
    public Vector2 startOffset = new Vector2(0f, 80f);
    public float arrowRotationOffset = 0f;

    [Header("Debug")]
    public bool debugLogPosition = false;

    private RectTransform startCard;
    private Canvas canvas;
    private bool isShowing;

    private void Awake()
    {
        Instance = this;

        AutoFindRefs();

        Hide();
    }

    private void OnEnable()
    {
        Instance = this;

        AutoFindRefs();
    }

    private void AutoFindRefs()
    {
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (visualRoot == null)
            visualRoot = transform as RectTransform;

        if (positionRoot == null)
            positionRoot = visualRoot;

        if (arrowBody == null)
        {
            Transform body = transform.Find("ArrowCanvasRoot/ArrowBody");

            if (body == null)
                body = transform.Find("ArrowBody");

            if (body != null)
                arrowBody = body as RectTransform;
        }

        if (arrowHead == null)
        {
            Transform head = transform.Find("ArrowCanvasRoot/ArrowHead");

            if (head == null)
                head = transform.Find("ArrowHead");

            if (head != null)
                arrowHead = head as RectTransform;
        }
    }

    public void Show(RectTransform cardRect)
    {
        AutoFindRefs();

        startCard = cardRect;
        isShowing = true;

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        SetArrowVisible(true);
    }

    public void Hide()
    {
        startCard = null;
        isShowing = false;

        SetArrowVisible(false);
    }

    private void SetArrowVisible(bool visible)
    {
        if (visualRoot != null)
            visualRoot.gameObject.SetActive(visible);

        if (arrowBody != null)
            arrowBody.gameObject.SetActive(visible);

        if (arrowHead != null)
            arrowHead.gameObject.SetActive(visible);
    }

    public void UpdateArrow(Vector2 mouseScreenPos)
    {
        if (!isShowing)
            return;

        if (startCard == null)
            return;

        AutoFindRefs();

        if (positionRoot == null)
            return;

        Camera cam = null;

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera;

        Vector2 startScreenPos = RectTransformUtility.WorldToScreenPoint(
            cam,
            startCard.position
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            positionRoot,
            startScreenPos,
            cam,
            out Vector2 startLocal
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            positionRoot,
            mouseScreenPos,
            cam,
            out Vector2 mouseLocal
        );

        Vector2 finalStartLocal = startLocal + startOffset;
        Vector2 finalEndLocal = mouseLocal + Vector2.down * mouseEndYOffset;

        if (debugLogPosition)
        {
            Debug.Log(
                $"[TargetArrowUI] mouse = {mouseScreenPos}, start = {finalStartLocal}, end = {finalEndLocal}"
            );
        }

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
            arrowBody.gameObject.SetActive(true);
            arrowBody.anchoredPosition = startLocal + dir * 0.5f;
            arrowBody.sizeDelta = new Vector2(length, arrowBody.sizeDelta.y);
            arrowBody.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        if (arrowHead != null)
        {
            arrowHead.gameObject.SetActive(true);
            arrowHead.anchoredPosition = endLocal;
            arrowHead.localRotation = Quaternion.Euler(0f, 0f, angle + arrowRotationOffset);
        }
    }
}