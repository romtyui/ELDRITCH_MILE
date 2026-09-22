using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum TooltipOpenMode
{
    Hover,
    Click
}

public class TooltipTriggerUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Target")]
    public RectTransform targetRect;

    [Header("Position")]
    public TooltipAnchorSide preferredSide = TooltipAnchorSide.Left;

    [Tooltip("此 Tooltip 額外的位置偏移。X 正數向右，Y 正數向上")]
    public Vector2 positionOffset = Vector2.zero;

    [Header("Open Mode")]
    public TooltipOpenMode openMode = TooltipOpenMode.Hover;

    [Header("Click Settings")]
    public bool stopEventPropagation = false;

    [Header("Static Entries")]
    public List<TooltipEntry> entries = new();

    private bool isOpen;

    private static TooltipTriggerUI currentClickedTooltip;

    public void SetEntries(List<TooltipEntry> newEntries)
    {
        entries = newEntries;
    }

    public void SetEntries(List<TooltipEntry> newEntries, TooltipAnchorSide side)
    {
        entries = newEntries;
        preferredSide = side;
    }

    public void SetTooltip(string title, string body, string keyword = "")
    {
        List<TooltipEntry> newEntries = new List<TooltipEntry>();

        if (!string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(body))
        {
            newEntries.Add(new TooltipEntry(title, body));

            if (!string.IsNullOrWhiteSpace(keyword))
                newEntries.Add(new TooltipEntry("說明", keyword));
        }

        SetEntries(newEntries);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (openMode != TooltipOpenMode.Hover)
            return;

        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (openMode != TooltipOpenMode.Hover)
            return;

        HideTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (openMode != TooltipOpenMode.Click)
            return;

        if (isOpen)
        {
            HideTooltip();

            if (currentClickedTooltip == this)
                currentClickedTooltip = null;

            isOpen = false;

            if (stopEventPropagation)
                eventData.Use();

            return;
        }

        CloseCurrentClickedTooltip();
        ShowTooltip();

        isOpen = true;
        currentClickedTooltip = this;

        if (stopEventPropagation)
            eventData.Use();
    }

    public void ShowTooltip()
    {
        if (entries == null || entries.Count == 0)
            return;

        if (TooltipUI.Instance == null)
            return;

        RectTransform rect = targetRect != null ? targetRect : transform as RectTransform;

        if (rect == null)
            return;

        bool showClickBlocker = openMode == TooltipOpenMode.Click;
        TooltipUI.Instance.Show(entries, rect, preferredSide, positionOffset, showClickBlocker);
    }

    private void OnDestroy()
    {
        HideTooltip();
    }

    private void HideTooltip()
    {
        if (TooltipUI.Instance != null)
            TooltipUI.Instance.Hide();

        isOpen = false;
    }

    public static void CloseCurrentClickedTooltip()
    {
        if (currentClickedTooltip != null)
        {
            currentClickedTooltip.isOpen = false;
            currentClickedTooltip = null;
        }

        if (TooltipUI.Instance != null)
            TooltipUI.Instance.Hide();
    }

    private void OnDisable()
    {
        if (currentClickedTooltip == this)
            currentClickedTooltip = null;

        isOpen = false;

        if (TooltipUI.Instance != null)
            TooltipUI.Instance.Hide();
    }
}