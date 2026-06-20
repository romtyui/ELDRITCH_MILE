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

    [Header("Open Mode")]
    public TooltipOpenMode openMode = TooltipOpenMode.Hover;

    [Header("Click Settings")]
    public bool stopEventPropagation = false;

    [Header("Static Entries")]
    public List<TooltipEntry> entries = new();

    private bool isOpen;

    private static TooltipTriggerUI currentClickedTooltip;

    public void SetEntries(List<TooltipEntry> newEntries, TooltipAnchorSide side = TooltipAnchorSide.Left)
    {
        entries = newEntries;
        preferredSide = side;
    }

    // «O¯dÂÂª© SetTooltip¡AÁ×§K EnemyUnit / CardViewUI / StatusIconUI ÂÂ©I¥s³ø¿ù
    public void SetTooltip(string title, string body, string keyword = "")
    {
        List<TooltipEntry> newEntries = new List<TooltipEntry>();

        if (!string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(body))
        {
            newEntries.Add(new TooltipEntry(title, body));

            if (!string.IsNullOrWhiteSpace(keyword))
                newEntries.Add(new TooltipEntry("»¡©ú", keyword));
        }

        SetEntries(newEntries, preferredSide);
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

    private void ShowTooltip()
    {
        if (TooltipUI.Instance == null)
            return;

        if (entries == null || entries.Count == 0)
            return;

        RectTransform rect = targetRect != null ? targetRect : transform as RectTransform;

        TooltipUI.Instance.Show(entries, rect, preferredSide);
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