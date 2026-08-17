using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UIRaycastDebug : MonoBehaviour
{
    [Header("Debug")]
    public bool enableDebug = true;

    [Tooltip("是否在滑鼠左鍵點擊時輸出目前滑鼠底下的 UI Raycast 結果")]
    public bool printOnLeftClick = true;

    [Tooltip("最多列出幾個 Raycast 物件")]
    public int maxPrintCount = 20;

    private void Update()
    {
        if (!enableDebug)
            return;

        if (Mouse.current == null)
            return;

        if (printOnLeftClick && !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        PrintRaycastResults();
    }

    [ContextMenu("Print UI Raycast Results")]
    public void PrintRaycastResults()
    {
        if (EventSystem.current == null)
        {
            Debug.LogWarning("[UIRaycastDebug] EventSystem.current 是 null");
            return;
        }

        Vector2 mousePosition = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : Vector2.zero;

        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        Debug.Log($"[UIRaycastDebug] Mouse = {mousePosition}, Raycast Count = {results.Count}");

        int count = Mathf.Min(results.Count, maxPrintCount);

        for (int i = 0; i < count; i++)
        {
            RaycastResult result = results[i];

            string moduleName = result.module != null
                ? result.module.name
                : "null";

            int sortOrderPriority = result.module != null
                ? result.module.sortOrderPriority
                : 0;

            int renderOrderPriority = result.module != null
                ? result.module.renderOrderPriority
                : 0;

            Debug.Log(
                $"[UIRaycastDebug] {i}: {result.gameObject.name}, " +
                $"module = {moduleName}, " +
                $"sortingLayer = {result.sortingLayer}, " +
                $"sortingOrder = {result.sortingOrder}, " +
                $"depth = {result.depth}, " +
                $"distance = {result.distance}, " +
                $"sortOrderPriority = {sortOrderPriority}, " +
                $"renderOrderPriority = {renderOrderPriority}",
                result.gameObject
            );
        }
    }
}