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

    [Header("Window Background")]
    [Tooltip("TooltipContainer 底下的 Background Image")]
    public Image windowBackgroundImage;

    [Tooltip("Tooltip 顯示在目標左側時使用的背景")]
    public Sprite leftBackgroundSprite;

    [Tooltip("Tooltip 顯示在目標右側時使用的背景")]
    public Sprite rightBackgroundSprite;

    [Tooltip("Tooltip 顯示在目標上方時使用的背景")]
    public Sprite topBackgroundSprite;

    [Tooltip("Tooltip 顯示在目標下方時使用的背景")]
    public Sprite bottomBackgroundSprite;

    [Header("Scroll Rect")]
    public ScrollRect scrollRect;
    public RectTransform viewport;

    [Header("Click Blocker")]
    public GameObject clickBlocker;

    [Header("Size")]
    [Min(1f)] public float tooltipWidth = 420f;
    [Min(1f)] public float minHeight = 100f;
    [Min(1f)] public float maxHeight = 360f;

    [Tooltip("TooltipContainer 中除了 Content 以外占用的上下總高度")]
    [Min(0f)] public float frameVerticalPadding = 24f;

    [Header("Position")]
    public Vector2 sideOffset = new Vector2(20f, 0f);
    public Vector2 verticalOffset = new Vector2(0f, 20f);
    public Vector2 screenPadding = new Vector2(20f, 20f);

    [Header("Clamp")]
    public bool clampToCanvas = true;
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
        SetupScrollRect();

        if (blockTemplate != null)
            blockTemplate.gameObject.SetActive(false);

        Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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
        if (container == null || !forceTopLeftPivot)
            return;

        container.anchorMin = new Vector2(0.5f, 0.5f);
        container.anchorMax = new Vector2(0.5f, 0.5f);
        container.pivot = new Vector2(0f, 1f);
        container.localScale = Vector3.one;
        container.localRotation = Quaternion.identity;
    }

    private void SetupScrollRect()
    {
        if (scrollRect == null)
            return;

        scrollRect.content = contentRoot;

        if (viewport != null)
            scrollRect.viewport = viewport;

        scrollRect.horizontal = false;
        scrollRect.vertical = true;
    }

    public void Show(List<TooltipEntry> entries, RectTransform target, TooltipAnchorSide preferredSide, Vector2 positionOffset, bool showClickBlocker)
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
        SetupScrollRect();
        ClearBlocks();

        container.gameObject.SetActive(true);
        SetClickBlockerActive(showClickBlocker);

        float initialMaxHeight = GetSafeMaxHeight();
        container.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, tooltipWidth);
        container.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, initialMaxHeight);

        Canvas.ForceUpdateCanvases();

        for (int i = 0; i < entries.Count; i++)
        {
            TooltipEntry entry = entries[i];

            if (entry == null)
                continue;

            TooltipBlockUI block = Instantiate(blockTemplate, contentRoot);
            block.gameObject.SetActive(true);
            block.SetData(entry);
            spawnedBlocks.Add(block);
        }

        if (spawnedBlocks.Count == 0)
        {
            Hide();
            return;
        }

        RefreshLayoutAndContainerSize();
        RepositionContainer(target, preferredSide, positionOffset);
        ResetScrollPosition();
    }

    public void Hide()
    {
        ClearBlocks();
        SetClickBlockerActive(false);

        if (container != null)
            container.gameObject.SetActive(false);
    }

    private void RefreshLayoutAndContainerSize()
    {
        if (container == null || contentRoot == null)
            return;

        container.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, tooltipWidth);
        container.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, GetSafeMaxHeight());

        Canvas.ForceUpdateCanvases();

        for (int i = 0; i < spawnedBlocks.Count; i++)
        {
            TooltipBlockUI block = spawnedBlocks[i];

            if (block == null || block.rectTransform == null)
                continue;

            LayoutRebuilder.ForceRebuildLayoutImmediate(block.rectTransform);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        Canvas.ForceUpdateCanvases();

        float preferredContentHeight = LayoutUtility.GetPreferredHeight(contentRoot);

        if (preferredContentHeight <= 0f)
            preferredContentHeight = contentRoot.rect.height;

        float safeMaxHeight = GetSafeMaxHeight();
        float safeMinHeight = Mathf.Clamp(minHeight, 1f, safeMaxHeight);
        float preferredContainerHeight = preferredContentHeight + frameVerticalPadding;
        float finalHeight = Mathf.Clamp(preferredContainerHeight, safeMinHeight, safeMaxHeight);

        container.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, tooltipWidth);
        container.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, finalHeight);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        LayoutRebuilder.ForceRebuildLayoutImmediate(container);
        Canvas.ForceUpdateCanvases();

        if (scrollRect == null)
            return;

        scrollRect.content = contentRoot;

        if (viewport != null)
            scrollRect.viewport = viewport;

        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.velocity = Vector2.zero;
    }

    private float GetSafeMaxHeight()
    {
        float configuredMaxHeight = Mathf.Max(1f, Mathf.Max(minHeight, maxHeight));

        if (canvasRect == null)
            return configuredMaxHeight;

        float canvasAvailableHeight = canvasRect.rect.height - screenPadding.y * 2f;
        return Mathf.Max(1f, Mathf.Min(configuredMaxHeight, canvasAvailableHeight));
    }

    private void ResetScrollPosition()
    {
        if (scrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = 1f;
    }

    private void SetClickBlockerActive(bool active)
    {
        if (clickBlocker == null)
            return;

        if (!active)
        {
            clickBlocker.SetActive(false);
            return;
        }

        clickBlocker.SetActive(true);

        if (container == null)
            return;

        if (clickBlocker.transform.parent != container.parent)
        {
            Debug.LogWarning("[TooltipUI] TooltipClickBlocker 和 TooltipContainer 必須是同一個父物件底下的兄弟物件");
            return;
        }

        clickBlocker.transform.SetAsFirstSibling();
        container.transform.SetAsLastSibling();
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

    private void RepositionContainer(RectTransform target, TooltipAnchorSide preferredSide, Vector2 positionOffset)
    {
        if (target == null || canvasRect == null || container == null)
            return;

        SetupContainerTransform();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(container);
        Canvas.ForceUpdateCanvases();

        Bounds targetBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRect, target);
        Vector2 tooltipSize = container.rect.size;

        float targetCenterX = (targetBounds.min.x + targetBounds.max.x) * 0.5f;
        float targetCenterY = (targetBounds.min.y + targetBounds.max.y) * 0.5f;
        float tooltipHalfWidth = tooltipSize.x * 0.5f;
        float tooltipHalfHeight = tooltipSize.y * 0.5f;

        Vector2 leftPos = new Vector2(targetBounds.min.x - tooltipSize.x - sideOffset.x, targetCenterY + tooltipHalfHeight + sideOffset.y);
        Vector2 rightPos = new Vector2(targetBounds.max.x + sideOffset.x, targetCenterY + tooltipHalfHeight + sideOffset.y);
        Vector2 topPos = new Vector2(targetCenterX - tooltipHalfWidth + verticalOffset.x, targetBounds.max.y + tooltipSize.y + verticalOffset.y);
        Vector2 bottomPos = new Vector2(targetCenterX - tooltipHalfWidth + verticalOffset.x, targetBounds.min.y - verticalOffset.y);

        Vector2 finalPos;
        TooltipAnchorSide resolvedSide;

        switch (preferredSide)
        {
            case TooltipAnchorSide.Left:
                finalPos = ChooseBestPosition(leftPos, TooltipAnchorSide.Left, rightPos, TooltipAnchorSide.Right, topPos, TooltipAnchorSide.Top, bottomPos, TooltipAnchorSide.Bottom, tooltipSize, out resolvedSide);
                break;

            case TooltipAnchorSide.Right:
                finalPos = ChooseBestPosition(rightPos, TooltipAnchorSide.Right, leftPos, TooltipAnchorSide.Left, topPos, TooltipAnchorSide.Top, bottomPos, TooltipAnchorSide.Bottom, tooltipSize, out resolvedSide);
                break;

            case TooltipAnchorSide.Top:
                finalPos = ChooseBestPosition(topPos, TooltipAnchorSide.Top, bottomPos, TooltipAnchorSide.Bottom, leftPos, TooltipAnchorSide.Left, rightPos, TooltipAnchorSide.Right, tooltipSize, out resolvedSide);
                break;

            case TooltipAnchorSide.Bottom:
                finalPos = ChooseBestPosition(bottomPos, TooltipAnchorSide.Bottom, topPos, TooltipAnchorSide.Top, leftPos, TooltipAnchorSide.Left, rightPos, TooltipAnchorSide.Right, tooltipSize, out resolvedSide);
                break;

            case TooltipAnchorSide.Auto:
            default:
                finalPos = ChooseBestPosition(leftPos, TooltipAnchorSide.Left, rightPos, TooltipAnchorSide.Right, topPos, TooltipAnchorSide.Top, bottomPos, TooltipAnchorSide.Bottom, tooltipSize, out resolvedSide);
                break;
        }

        finalPos += positionOffset;

        if (clampToCanvas)
            finalPos = ClampToCanvas(finalPos, tooltipSize);

        container.anchoredPosition = finalPos;
        SetWindowBackground(resolvedSide);
    }

    private Vector2 ChooseBestPosition(Vector2 firstPosition, TooltipAnchorSide firstSide, Vector2 secondPosition, TooltipAnchorSide secondSide, Vector2 thirdPosition, TooltipAnchorSide thirdSide, Vector2 fourthPosition, TooltipAnchorSide fourthSide, Vector2 tooltipSize, out TooltipAnchorSide resolvedSide)
    {
        if (FitsInsideCanvas(firstPosition, tooltipSize))
        {
            resolvedSide = firstSide;
            return firstPosition;
        }

        if (FitsInsideCanvas(secondPosition, tooltipSize))
        {
            resolvedSide = secondSide;
            return secondPosition;
        }

        if (FitsInsideCanvas(thirdPosition, tooltipSize))
        {
            resolvedSide = thirdSide;
            return thirdPosition;
        }

        if (FitsInsideCanvas(fourthPosition, tooltipSize))
        {
            resolvedSide = fourthSide;
            return fourthPosition;
        }

        resolvedSide = firstSide;
        return firstPosition;
    }

    private void SetWindowBackground(TooltipAnchorSide side)
    {
        if (windowBackgroundImage == null)
            return;

        Sprite selectedSprite = null;

        switch (side)
        {
            case TooltipAnchorSide.Left:
                selectedSprite = leftBackgroundSprite;
                break;

            case TooltipAnchorSide.Right:
                selectedSprite = rightBackgroundSprite;
                break;

            case TooltipAnchorSide.Top:
                selectedSprite = topBackgroundSprite;
                break;

            case TooltipAnchorSide.Bottom:
                selectedSprite = bottomBackgroundSprite;
                break;
        }

        if (selectedSprite == null)
            return;

        windowBackgroundImage.sprite = selectedSprite;
        windowBackgroundImage.enabled = true;
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

        if (maxX < minX)
            maxX = minX;

        if (maxY < minY)
            maxY = minY;

        float x = Mathf.Clamp(topLeftPosition.x, minX, maxX);
        float y = Mathf.Clamp(topLeftPosition.y, minY, maxY);

        return new Vector2(x, y);
    }
}