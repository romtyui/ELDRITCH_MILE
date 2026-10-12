using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraEdgePan : MonoBehaviour
{
    [Header("Pan Targets")]
    public List<Transform> panTargets = new();

    [Header("Edge Settings")]
    [Range(0.01f, 0.5f)] public float edgeZoneRatio = 0.15f;
    [Min(0f)] public float maxHorizontalOffset = 12f;
    [Min(0f)] public float maxVerticalOffset = 8f;
    [Min(0.01f)] public float smoothTime = 0.2f;
    public bool reverseDirection = true;

    private readonly Dictionary<Transform, Vector3> originalPositions = new();
    private Vector2 currentOffset;
    private Vector2 offsetVelocity;

    private void OnEnable()
    {
        originalPositions.Clear();
        currentOffset = Vector2.zero;
        offsetVelocity = Vector2.zero;

        for (int i = 0; i < panTargets.Count; i++)
        {
            Transform target = panTargets[i];
            if (target == null || originalPositions.ContainsKey(target)) continue;
            originalPositions.Add(target, target.localPosition);
        }
    }

    private void LateUpdate()
    {
        Vector2 targetOffset = Vector2.zero;

        if (Application.isFocused && Mouse.current != null && Screen.width > 0 && Screen.height > 0)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();

            if (mousePosition.x >= 0f && mousePosition.x <= Screen.width && mousePosition.y >= 0f && mousePosition.y <= Screen.height)
            {
                float horizontal = GetEdgeAmount(mousePosition.x / Screen.width);
                float vertical = GetEdgeAmount(mousePosition.y / Screen.height);
                float direction = reverseDirection ? -1f : 1f;
                targetOffset = new Vector2(horizontal * maxHorizontalOffset * direction, vertical * maxVerticalOffset * direction);
            }
        }

        currentOffset = Vector2.SmoothDamp(currentOffset, targetOffset, ref offsetVelocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);

        foreach (var pair in originalPositions)
        {
            if (pair.Key == null) continue;
            pair.Key.localPosition = pair.Value + new Vector3(currentOffset.x, currentOffset.y, 0f);
        }
    }

    private float GetEdgeAmount(float normalizedPosition)
    {
        float zone = Mathf.Clamp(edgeZoneRatio, 0.01f, 0.5f);
        if (normalizedPosition < zone) return (normalizedPosition - zone) / zone;
        if (normalizedPosition > 1f - zone) return (normalizedPosition - (1f - zone)) / zone;
        return 0f;
    }

    private void OnDisable()
    {
        foreach (var pair in originalPositions)
        {
            if (pair.Key == null) continue;
            pair.Key.localPosition = pair.Value;
        }

        currentOffset = Vector2.zero;
        offsetVelocity = Vector2.zero;
    }
}