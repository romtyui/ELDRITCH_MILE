using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TooltipBlockUI : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text bodyText;

    [Header("Layout")]
    public RectTransform rectTransform;
    public VerticalLayoutGroup verticalLayoutGroup;
    public LayoutElement layoutElement;

    [Min(1f)] public float minimumHeight = 40f;

    private void Awake()
    {
        CacheReferences();
    }

    public void SetData(TooltipEntry entry)
    {
        CacheReferences();

        if (titleText != null)
        {
            titleText.text = entry != null ? entry.title : "";
            titleText.enableWordWrapping = true;
        }

        if (bodyText != null)
        {
            bodyText.text = entry != null ? entry.body : "";
            bodyText.enableWordWrapping = true;
        }

        RebuildLayout();
    }

    private void CacheReferences()
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        if (verticalLayoutGroup == null)
            verticalLayoutGroup = GetComponent<VerticalLayoutGroup>();

        if (layoutElement == null)
            layoutElement = GetComponent<LayoutElement>();

        if (layoutElement == null)
            layoutElement = gameObject.AddComponent<LayoutElement>();
    }

    private void RebuildLayout()
    {
        CacheReferences();

        if (rectTransform == null)
            return;

        Canvas.ForceUpdateCanvases();

        RectTransform parentRect = rectTransform.parent as RectTransform;

        if (parentRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);

        Canvas.ForceUpdateCanvases();

        float blockWidth = rectTransform.rect.width;

        if (blockWidth <= 1f && parentRect != null)
            blockWidth = parentRect.rect.width;

        float horizontalPadding = verticalLayoutGroup != null ? verticalLayoutGroup.padding.horizontal : 0f;
        float verticalPadding = verticalLayoutGroup != null ? verticalLayoutGroup.padding.vertical : 0f;
        float availableTextWidth = Mathf.Max(1f, blockWidth - horizontalPadding);

        float titleHeight = GetPreferredTextHeight(titleText, availableTextWidth);
        float bodyHeight = GetPreferredTextHeight(bodyText, availableTextWidth);

        int visibleTextCount = 0;

        if (titleText != null && !string.IsNullOrWhiteSpace(titleText.text))
            visibleTextCount++;

        if (bodyText != null && !string.IsNullOrWhiteSpace(bodyText.text))
            visibleTextCount++;

        float spacing = 0f;

        if (verticalLayoutGroup != null && visibleTextCount > 1)
            spacing = verticalLayoutGroup.spacing * (visibleTextCount - 1);

        float preferredHeight = verticalPadding + titleHeight + bodyHeight + spacing;
        preferredHeight = Mathf.Max(minimumHeight, preferredHeight);

        layoutElement.ignoreLayout = false;
        layoutElement.preferredHeight = preferredHeight;
        layoutElement.flexibleHeight = 0f;

        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

        if (parentRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);

        Canvas.ForceUpdateCanvases();
    }

    private float GetPreferredTextHeight(TMP_Text textComponent, float availableWidth)
    {
        if (textComponent == null || string.IsNullOrWhiteSpace(textComponent.text))
            return 0f;

        Vector2 preferredSize = textComponent.GetPreferredValues(textComponent.text, availableWidth, Mathf.Infinity);
        return preferredSize.y;
    }
}