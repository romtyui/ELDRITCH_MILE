using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum TooltipAnchorSide
{
    Auto,
    Left,
    Right,
    Top,
    Bottom
}

public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance;

    [Header("Root")]
    public RectTransform container;
    public RectTransform contentRoot;
    public TooltipBlockUI blockTemplate;

    [Header("Position")]
    public Vector2 sideOffset = new Vector2(20f, 0f);
    public Vector2 verticalOffset = new Vector2(0f, 20f);
    public Vector2 screenPadding = new Vector2(20f, 20f);

    [Header("Clamp")]
    [Tooltip("是否強制讓 Tooltip 留在 Canvas 畫面內")]
    public bool clampToCanvas = true;

    [Tooltip("顯示時是否強制修正 container 的 Anchor / Pivot。建議打開")]
    public bool forceTopLeftPivot = true;

    private Canvas rootCanvas;
    private RectTransform canvasRect;
    private Camera canvasCamera;

    private readonly List<TooltipBlockUI> spawnedBlocks = new();

    private void Awake()
    {
        Instance = this;

        CacheCanvasRefs();
        SetupContainerTransform();

        Hide();

        if (blockTemplate != null)
            blockTemplate.gameObject.SetActive(false);
    }

    private void CacheCanvasRefs()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        canvasRect = rootCanvas != null ? rootCanvas.GetComponent<RectTransform>() : null;

        canvasCamera = null;

        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            canvasCamera = rootCanvas.worldCamera;
    }

    private void SetupContainerTransform()
    {
        if (container == null)
            return;

        if (!forceTopLeftPivot)
            return;

        // 這裡很重要：
        // Reposition() 算出來的位置是「以 Canvas 中心為原點」的 local position。
        // 所以 container 的 anchor 要固定在 Canvas 中心。
        container.anchorMin = new Vector2(0.5f, 0.5f);
        container.anchorMax = new Vector2(0.5f, 0.5f);

        // 讓 anchoredPosition 代表 Tooltip 左上角位置。
        container.pivot = new Vector2(0f, 1f);
    }

    public void Show(List<TooltipEntry> entries, RectTransform target, TooltipAnchorSide preferredSide = TooltipAnchorSide.Auto)
    {
        if (container == null || contentRoot == null || blockTemplate == null)
            return;

        if (entries == null || entries.Count == 0 || target == null)
        {
            Hide();
            return;
        }

        CacheCanvasRefs();
        SetupContainerTransform();

        ClearBlocks();

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] == null)
                continue;

            TooltipBlockUI block = Instantiate(blockTemplate, contentRoot);
            block.gameObject.SetActive(true);
            block.SetData(entries[i]);
            spawnedBlocks.Add(block);
        }

        if (spawnedBlocks.Count == 0)
        {
            Hide();
            return;
        }

        container.gameObject.SetActive(true);

        Canvas.ForceUpdateCanvases();

        if (contentRoot != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);

        if (container != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(container);

        Canvas.ForceUpdateCanvases();

        Reposition(target, preferredSide);
    }

    public void Hide()
    {
        ClearBlocks();

        if (container != null)
            container.gameObject.SetActive(false);
    }

    private void ClearBlocks()
    {
        for (int i = spawnedBlocks.Count - 1; i >= 0; i--)
        {
            TooltipBlockUI block = spawnedBlocks[i];

            if (block == null)
                continue;

            Transform blockTransform = block.transform;

            if (blockTransform != null)
                blockTransform.SetParent(null);

            Destroy(block.gameObject);
        }

        spawnedBlocks.Clear();

        if (contentRoot == null)
            return;

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = contentRoot.GetChild(i);

            if (child == null)
                continue;

            if (blockTemplate != null && child == blockTemplate.transform)
                continue;

            child.SetParent(null);
            Destroy(child.gameObject);
        }
    }

    private void Reposition(RectTransform target, TooltipAnchorSide preferredSide)
    {
        if (target == null || canvasRect == null || container == null)
            return;

        SetupContainerTransform();

        Canvas.ForceUpdateCanvases();

        if (contentRoot != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);

        if (container != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(container);

        Canvas.ForceUpdateCanvases();

        Bounds targetBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRect, target);
        Vector2 tooltipSize = container.rect.size;

        float targetCenterY = (targetBounds.min.y + targetBounds.max.y) * 0.5f;
        float tooltipHalfHeight = tooltipSize.y * 0.5f;

        // 因為 container pivot 是左上角，所以這些位置都是 Tooltip 左上角座標。

        Vector2 leftPos = new Vector2(
            targetBounds.min.x - tooltipSize.x - sideOffset.x,
            targetCenterY + tooltipHalfHeight + sideOffset.y
        );

        Vector2 rightPos = new Vector2(
            targetBounds.max.x + sideOffset.x,
            targetCenterY + tooltipHalfHeight + sideOffset.y
        );

        Vector2 topPos = new Vector2(
            targetBounds.min.x,
            targetBounds.max.y + tooltipSize.y + verticalOffset.y
        );

        Vector2 bottomPos = new Vector2(
            targetBounds.min.x,
            targetBounds.min.y - verticalOffset.y
        );

        Vector2 finalPos;

        switch (preferredSide)
        {
            case TooltipAnchorSide.Left:
                finalPos = ChooseBestPosition(leftPos, rightPos, topPos, bottomPos, tooltipSize);
                break;

            case TooltipAnchorSide.Right:
                finalPos = ChooseBestPosition(rightPos, leftPos, topPos, bottomPos, tooltipSize);
                break;

            case TooltipAnchorSide.Top:
                finalPos = ChooseBestPosition(topPos, bottomPos, leftPos, rightPos, tooltipSize);
                break;

            case TooltipAnchorSide.Bottom:
                finalPos = ChooseBestPosition(bottomPos, topPos, leftPos, rightPos, tooltipSize);
                break;

            case TooltipAnchorSide.Auto:
            default:
                finalPos = ChooseBestPosition(leftPos, rightPos, topPos, bottomPos, tooltipSize);
                break;
        }

        if (clampToCanvas)
            finalPos = ClampToCanvas(finalPos, tooltipSize);

        container.anchoredPosition = finalPos;
    }

    private Vector2 ChooseBestPosition(
        Vector2 first,
        Vector2 second,
        Vector2 third,
        Vector2 fourth,
        Vector2 tooltipSize
    )
    {
        if (FitsInsideCanvas(first, tooltipSize))
            return first;

        if (FitsInsideCanvas(second, tooltipSize))
            return second;

        if (FitsInsideCanvas(third, tooltipSize))
            return third;

        if (FitsInsideCanvas(fourth, tooltipSize))
            return fourth;

        return first;
    }

    private bool FitsInsideCanvas(Vector2 topLeftPosition, Vector2 size)
    {
        if (canvasRect == null)
            return true;

        Rect rect = canvasRect.rect;

        float left = topLeftPosition.x;
        float right = topLeftPosition.x + size.x;
        float top = topLeftPosition.y;
        float bottom = topLeftPosition.y - size.y;

        return left >= rect.xMin + screenPadding.x &&
               right <= rect.xMax - screenPadding.x &&
               bottom >= rect.yMin + screenPadding.y &&
               top <= rect.yMax - screenPadding.y;
    }

    private Vector2 ClampToCanvas(Vector2 topLeftPosition, Vector2 size)
    {
        if (canvasRect == null)
            return topLeftPosition;

        Rect rect = canvasRect.rect;

        float minX = rect.xMin + screenPadding.x;
        float maxX = rect.xMax - size.x - screenPadding.x;

        float minY = rect.yMin + size.y + screenPadding.y;
        float maxY = rect.yMax - screenPadding.y;

        // 如果 Tooltip 太大，避免 Mathf.Clamp min > max 導致位置異常。
        if (maxX < minX)
            maxX = minX;

        if (maxY < minY)
            maxY = minY;

        float x = Mathf.Clamp(topLeftPosition.x, minX, maxX);
        float y = Mathf.Clamp(topLeftPosition.y, minY, maxY);

        return new Vector2(x, y);
    }
}