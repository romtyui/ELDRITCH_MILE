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

    private Canvas rootCanvas;
    private RectTransform canvasRect;
    private Camera canvasCamera;
    private readonly List<TooltipBlockUI> spawnedBlocks = new();

    private void Awake()
    {
        Instance = this;

        rootCanvas = GetComponentInParent<Canvas>();
        canvasRect = rootCanvas != null ? rootCanvas.GetComponent<RectTransform>() : null;

        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            canvasCamera = rootCanvas.worldCamera;

        Hide();

        if (blockTemplate != null)
            blockTemplate.gameObject.SetActive(false);
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

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);

        if (container != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(container);

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

        Bounds targetBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRect, target);
        Vector2 tooltipSize = container.rect.size;

        float targetCenterY = (targetBounds.min.y + targetBounds.max.y) * 0.5f;
        float tooltipCenterOffsetY = tooltipSize.y * 0.5f;

        Vector2 leftPos = new Vector2(
            targetBounds.min.x - tooltipSize.x - sideOffset.x,
            targetCenterY + tooltipCenterOffsetY
        );

        Vector2 rightPos = new Vector2(
            targetBounds.max.x + sideOffset.x,
            targetCenterY + tooltipCenterOffsetY
        );

        Vector2 topPos = new Vector2(
            targetBounds.min.x,
            targetBounds.max.y + tooltipSize.y + verticalOffset.y
        );

        Vector2 bottomPos = new Vector2(
            targetBounds.min.x,
            targetBounds.min.y - verticalOffset.y
        );

        Vector2 finalPos = leftPos;

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

            default:
                finalPos = ChooseBestPosition(leftPos, rightPos, topPos, bottomPos, tooltipSize);
                break;
        }

        container.anchoredPosition = ClampToCanvas(finalPos, tooltipSize);
    }

    private Vector2 ChooseBestPosition(Vector2 first, Vector2 second, Vector2 third, Vector2 fourth, Vector2 tooltipSize)
    {
        if (FitsInsideCanvas(first, tooltipSize)) return first;
        if (FitsInsideCanvas(second, tooltipSize)) return second;
        if (FitsInsideCanvas(third, tooltipSize)) return third;
        if (FitsInsideCanvas(fourth, tooltipSize)) return fourth;

        return first;
    }

    private bool FitsInsideCanvas(Vector2 pos, Vector2 size)
    {
        if (canvasRect == null)
            return true;

        Rect rect = canvasRect.rect;

        float left = pos.x;
        float right = pos.x + size.x;
        float top = pos.y;
        float bottom = pos.y - size.y;

        return left >= rect.xMin + screenPadding.x &&
               right <= rect.xMax - screenPadding.x &&
               bottom >= rect.yMin + screenPadding.y &&
               top <= rect.yMax - screenPadding.y;
    }

    private Vector2 ClampToCanvas(Vector2 pos, Vector2 size)
    {
        if (canvasRect == null)
            return pos;

        Rect rect = canvasRect.rect;

        float x = Mathf.Clamp(
            pos.x,
            rect.xMin + screenPadding.x,
            rect.xMax - size.x - screenPadding.x
        );

        float y = Mathf.Clamp(
            pos.y,
            rect.yMin + size.y + screenPadding.y,
            rect.yMax - screenPadding.y
        );

        return new Vector2(x, y);
    }
}