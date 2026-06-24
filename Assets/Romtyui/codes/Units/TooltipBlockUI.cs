using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TooltipBlockUI : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text bodyText;

    [Header("Layout")]
    public RectTransform rectTransform;

    private void Awake()
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;
    }

    public void SetData(TooltipEntry entry)
    {
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

    private void RebuildLayout()
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        if (rectTransform == null)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        Canvas.ForceUpdateCanvases();
    }
}