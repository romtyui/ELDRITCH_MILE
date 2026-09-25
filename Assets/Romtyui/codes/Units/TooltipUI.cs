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

[System.Serializable]
public class TooltipBackgroundVariant
{
    [Header("Background Image")]
    public Sprite sprite;

    [Header("Background Image Size")]
    [Tooltip("只調整背景圖片寬度")]
    [Min(1f)] public float backgroundWidth = 420f;

    [Tooltip("只調整背景圖片高度")]
    [Min(1f)] public float backgroundHeight = 160f;

    [Header("Background Image Position")]
    [Tooltip("只調整 windowBackgroundImage 的位置")]
    public Vector2 backgroundPositionOffset = Vector2.zero;

    [Header("Window Position")]
    [Tooltip("調整整個 TooltipContainer 的位置")]
    public Vector2 windowPositionOffset = Vector2.zero;
}

[System.Serializable]
public class TooltipBackgroundSpritesByCount
{
    [Tooltip("1 個狀態")]
    public TooltipBackgroundVariant oneStatus = new TooltipBackgroundVariant();

    [Tooltip("2 個狀態")]
    public TooltipBackgroundVariant twoStatuses = new TooltipBackgroundVariant();

    [Tooltip("3 個狀態")]
    public TooltipBackgroundVariant threeStatuses = new TooltipBackgroundVariant();

    [Tooltip("4 個以上狀態")]
    public TooltipBackgroundVariant fourOrMoreStatuses = new TooltipBackgroundVariant();

    public TooltipBackgroundVariant GetVariant(int statusCount)
    {
        if (statusCount <= 1)
            return oneStatus;

        if (statusCount == 2)
            return twoStatuses;

        if (statusCount == 3)
            return threeStatuses;

        return fourOrMoreStatuses;
    }
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

    [Tooltip("Tooltip 顯示在目標左側時，依狀態數量使用的背景")]
    public TooltipBackgroundSpritesByCount leftBackgrounds = new TooltipBackgroundSpritesByCount();

    [Tooltip("Tooltip 顯示在目標右側時，依狀態數量使用的背景")]
    public TooltipBackgroundSpritesByCount rightBackgrounds = new TooltipBackgroundSpritesByCount();

    [Tooltip("Tooltip 顯示在目標上方時，依狀態數量使用的背景")]
    public TooltipBackgroundSpritesByCount topBackgrounds = new TooltipBackgroundSpritesByCount();

    [Tooltip("Tooltip 顯示在目標下方時，依狀態數量使用的背景")]
    public TooltipBackgroundSpritesByCount bottomBackgrounds = new TooltipBackgroundSpritesByCount();
    //[Header("Window Background")]
    //[Tooltip("TooltipContainer 底下的 Background Image")]
    //public Image windowBackgroundImage;

    //[Tooltip("Tooltip 顯示在目標左側時使用的背景")]
    //public Sprite leftBackgroundSprite;

    //[Tooltip("Tooltip 顯示在目標右側時使用的背景")]
    //public Sprite rightBackgroundSprite;

    //[Tooltip("Tooltip 顯示在目標上方時使用的背景")]
    //public Sprite topBackgroundSprite;

    //[Tooltip("Tooltip 顯示在目標下方時使用的背景")]
    //public Sprite bottomBackgroundSprite;

    [Header("Scroll Rect")]
    public ScrollRect scrollRect;
    public RectTransform viewport;

    [Header("Click Blocker")]
    public GameObject clickBlocker;

    [Header("Fixed Window Size")]
    [Min(1f)] public float tooltipWidth = 420f;
    [Min(1f)] public float tooltipHeight = 330f;

    [Tooltip("TooltipContainer 中除了 Content 以外占用的上下總高度")]
    [Min(0f)] public float frameVerticalPadding = 24f;

    [Header("Position")]
    public Vector2 sideOffset = new Vector2(20f, 0f);
    public Vector2 verticalOffset = new Vector2(0f, 20f);
    public Vector2 screenPadding = new Vector2(20f, 20f);

    [Header("Fixed Window Height")]
    [Tooltip("Left、Right 方向的 Tooltip 是否使用固定Y座標")]
    public bool useFixedWindowY = true;

    [Tooltip("TooltipContainer 固定的頂端Y座標")]
    public float fixedWindowY = 0f;

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

    public void Show(List<TooltipEntry> entries, RectTransform target, TooltipAnchorSide preferredSide, TooltipPositionMode positionMode, Vector2 positionOffset, bool showClickBlocker)
    {
        if (container == null || contentRoot == null || blockTemplate == null) return;

        if (entries == null || entries.Count == 0)
        {
            Hide();
            return;
        }

        if (positionMode == TooltipPositionMode.RelativeToTrigger && target == null)
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
        ApplyFixedWindowSize();

        Canvas.ForceUpdateCanvases();

        for (int i = 0; i < entries.Count; i++)
        {
            TooltipEntry entry = entries[i];
            if (entry == null) continue;

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
        RepositionContainer(target, preferredSide, positionMode, positionOffset, spawnedBlocks.Count);
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
        if (container == null || contentRoot == null) return;

        ApplyFixedWindowSize();
        Canvas.ForceUpdateCanvases();

        for (int i = 0; i < spawnedBlocks.Count; i++)
        {
            TooltipBlockUI block = spawnedBlocks[i];
            if (block == null || block.rectTransform == null) continue;

            LayoutRebuilder.ForceRebuildLayoutImmediate(block.rectTransform);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        LayoutRebuilder.ForceRebuildLayoutImmediate(container);
        Canvas.ForceUpdateCanvases();

        if (scrollRect == null) return;

        scrollRect.content = contentRoot;
        if (viewport != null) scrollRect.viewport = viewport;

        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.velocity = Vector2.zero;
    }
    private float GetSafeMaxHeight()
    {
        return Mathf.Max(1f, tooltipHeight);
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

    private void RepositionContainer(RectTransform target, TooltipAnchorSide preferredSide, TooltipPositionMode positionMode, Vector2 positionOffset, int statusCount)
    {
        if (container == null || canvasRect == null) return;

        SetupContainerTransform();
        ApplyFixedWindowSize();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(container);
        Canvas.ForceUpdateCanvases();

        TooltipAnchorSide resolvedSide = preferredSide == TooltipAnchorSide.Auto ? TooltipAnchorSide.Left : preferredSide;
        TooltipBackgroundVariant variant = GetWindowVariant(resolvedSide, statusCount);
        Vector2 finalPosition;

        if (positionMode == TooltipPositionMode.RelativeToTrigger)
        {
            Bounds targetBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRect, target);
            finalPosition = CalculateRelativePosition(resolvedSide, targetBounds, container.rect.size);
            finalPosition += positionOffset;
        }
        else
        {
            finalPosition = positionOffset;
        }

        ApplyWindowVariant(variant);

        if (variant != null) finalPosition += variant.windowPositionOffset;

        bool lockVerticalPosition = positionMode == TooltipPositionMode.FixedCanvasPosition && useFixedWindowY && (resolvedSide == TooltipAnchorSide.Left || resolvedSide == TooltipAnchorSide.Right);
        if (lockVerticalPosition) finalPosition.y = fixedWindowY;

        if (clampToCanvas)
        {
            finalPosition = ClampToCanvas(finalPosition, container.rect.size);
            if (lockVerticalPosition) finalPosition.y = fixedWindowY;
        }

        container.anchoredPosition = finalPosition;
    }
    private Vector2 CalculateRelativePosition(TooltipAnchorSide side, Bounds targetBounds, Vector2 tooltipSize)
    {
        float targetCenterX = (targetBounds.min.x + targetBounds.max.x) * 0.5f;
        float tooltipHalfWidth = tooltipSize.x * 0.5f;

        switch (side)
        {
            case TooltipAnchorSide.Left:
                return new Vector2(targetBounds.min.x - tooltipSize.x - sideOffset.x, targetBounds.max.y + sideOffset.y);

            case TooltipAnchorSide.Right:
                return new Vector2(targetBounds.max.x + sideOffset.x, targetBounds.max.y + sideOffset.y);

            case TooltipAnchorSide.Top:
                return new Vector2(targetCenterX - tooltipHalfWidth + verticalOffset.x, targetBounds.max.y + tooltipSize.y + verticalOffset.y);

            case TooltipAnchorSide.Bottom:
                return new Vector2(targetCenterX - tooltipHalfWidth + verticalOffset.x, targetBounds.min.y - verticalOffset.y);

            default:
                return new Vector2(targetBounds.max.x + sideOffset.x, targetBounds.max.y + sideOffset.y);
        }
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

    //private void SetWindowBackground(TooltipAnchorSide side, int statusCount)
    //{
    //    if (windowBackgroundImage == null)
    //        return;

    //    TooltipBackgroundSpritesByCount backgroundSet = null;

    //    switch (side)
    //    {
    //        case TooltipAnchorSide.Left:
    //            backgroundSet = leftBackgrounds;
    //            break;

    //        case TooltipAnchorSide.Right:
    //            backgroundSet = rightBackgrounds;
    //            break;

    //        case TooltipAnchorSide.Top:
    //            backgroundSet = topBackgrounds;
    //            break;

    //        case TooltipAnchorSide.Bottom:
    //            backgroundSet = bottomBackgrounds;
    //            break;
    //    }

    //    if (backgroundSet == null)
    //        return;

    //    Sprite selectedSprite = backgroundSet.GetSprite(statusCount);

    //    if (selectedSprite == null)
    //    {
    //        Debug.LogWarning($"[TooltipUI] 尚未設定 {side} 方向、{statusCount} 個狀態使用的背景圖片");
    //        return;
    //    }

    //    windowBackgroundImage.sprite = selectedSprite;
    //    windowBackgroundImage.enabled = true;
    //}

    private TooltipBackgroundVariant GetWindowVariant(TooltipAnchorSide side, int statusCount)
    {
        TooltipBackgroundSpritesByCount backgroundSet = null;

        switch (side)
        {
            case TooltipAnchorSide.Left:
                backgroundSet = leftBackgrounds;
                break;

            case TooltipAnchorSide.Right:
                backgroundSet = rightBackgrounds;
                break;

            case TooltipAnchorSide.Top:
                backgroundSet = topBackgrounds;
                break;

            case TooltipAnchorSide.Bottom:
                backgroundSet = bottomBackgrounds;
                break;
        }

        if (backgroundSet == null)
            return null;

        return backgroundSet.GetVariant(statusCount);
    }

    private void ApplyFixedWindowSize()
{
    if (container == null)
        return;

    container.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, tooltipWidth);
    container.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, tooltipHeight);
}

    private void ApplyWindowVariant(TooltipBackgroundVariant variant)
    {
        if (variant == null || windowBackgroundImage == null)
            return;

        if (variant.sprite != null)
        {
            windowBackgroundImage.sprite = variant.sprite;
            windowBackgroundImage.enabled = true;
        }

        RectTransform backgroundRect = windowBackgroundImage.rectTransform;

        // Background 頂端固定，高度只往下增加
        backgroundRect.anchorMin = new Vector2(0.5f, 1f);
        backgroundRect.anchorMax = new Vector2(0.5f, 1f);
        backgroundRect.pivot = new Vector2(0.5f, 1f);

        backgroundRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            Mathf.Max(1f, variant.backgroundWidth)
        );

        backgroundRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            Mathf.Max(1f, variant.backgroundHeight)
        );

        backgroundRect.anchoredPosition = variant.backgroundPositionOffset;
    }

    private Vector2 CalculatePosition(TooltipAnchorSide side, Bounds targetBounds, Vector2 tooltipSize)
    {
        float targetCenterX = (targetBounds.min.x + targetBounds.max.x) * 0.5f;
        float tooltipHalfWidth = tooltipSize.x * 0.5f;

        float horizontalWindowY = useFixedWindowY
            ? fixedWindowY
            : targetBounds.max.y + sideOffset.y;

        switch (side)
        {
            case TooltipAnchorSide.Left:
                return new Vector2(
                    targetBounds.min.x - tooltipSize.x - sideOffset.x,
                    horizontalWindowY
                );

            case TooltipAnchorSide.Right:
                return new Vector2(
                    targetBounds.max.x + sideOffset.x,
                    horizontalWindowY
                );

            case TooltipAnchorSide.Top:
                return new Vector2(
                    targetCenterX - tooltipHalfWidth + verticalOffset.x,
                    targetBounds.max.y + tooltipSize.y + verticalOffset.y
                );

            case TooltipAnchorSide.Bottom:
                return new Vector2(
                    targetCenterX - tooltipHalfWidth + verticalOffset.x,
                    targetBounds.min.y - verticalOffset.y
                );

            default:
                return container.anchoredPosition;
        }
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