using UnityEngine;

public class TutorialInverseMaskRaycastFilter :
    MonoBehaviour,
    ICanvasRaycastFilter
{
    [Header("Mask Reference")]
    [SerializeField] private RectTransform overlayRect;

    [Header("Hole Data")]
    [SerializeField]
    private Vector2 holeCenter =
        new Vector2(0.5f, 0.5f);

    [SerializeField]
    private Vector2 holeSize =
        new Vector2(0.3f, 0.2f);

    [SerializeField]
    [Range(0f, 0.5f)]
    private float cornerRadius = 0.05f;

    [SerializeField] private bool holeEnabled = true;

    public void SetHole(
        Vector2 normalizedCenter,
        Vector2 normalizedSize,
        float normalizedCornerRadius
    )
    {
        holeCenter = normalizedCenter;

        holeSize = new Vector2(
            Mathf.Clamp01(normalizedSize.x),
            Mathf.Clamp01(normalizedSize.y)
        );

        cornerRadius = Mathf.Max(
            0f,
            normalizedCornerRadius
        );

        holeEnabled = true;
    }

    public void SetHoleEnabled(bool enabled)
    {
        holeEnabled = enabled;
    }

    public bool IsRaycastLocationValid(
        Vector2 screenPoint,
        Camera eventCamera
    )
    {
        if (!holeEnabled)
        {
            // 沒有洞時，整片黑幕擋住操作
            return true;
        }

        if (overlayRect == null)
        {
            overlayRect = transform as RectTransform;
        }

        if (overlayRect == null)
        {
            return true;
        }

        Vector2 localPoint;

        bool converted =
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                overlayRect,
                screenPoint,
                eventCamera,
                out localPoint
            );

        if (!converted)
        {
            return true;
        }

        Rect rect = overlayRect.rect;

        Vector2 normalizedPoint = new Vector2(
            Mathf.InverseLerp(
                rect.xMin,
                rect.xMax,
                localPoint.x
            ),
            Mathf.InverseLerp(
                rect.yMin,
                rect.yMax,
                localPoint.y
            )
        );

        bool isInsideHole =
            IsPointInsideRoundedRectangle(
                normalizedPoint,
                holeCenter,
                holeSize,
                cornerRadius
            );

        /*
         * true：
         * DarkOverlay 接收 Raycast，阻擋下面 UI。
         *
         * false：
         * DarkOverlay 不接收 Raycast，
         * 點擊穿透到下面 UI。
         */
        return !isInsideHole;
    }

    private bool IsPointInsideRoundedRectangle(
        Vector2 point,
        Vector2 center,
        Vector2 size,
        float radius
    )
    {
        Vector2 halfSize =
            size * 0.5f;

        if (halfSize.x <= 0f ||
            halfSize.y <= 0f)
        {
            return false;
        }

        Vector2 local =
            point - center;

        float maxRadius =
            Mathf.Min(
                halfSize.x,
                halfSize.y
            );

        float finalRadius =
            Mathf.Clamp(
                radius,
                0f,
                maxRadius
            );

        Vector2 innerHalfSize =
            halfSize -
            new Vector2(
                finalRadius,
                finalRadius
            );

        Vector2 distance = new Vector2(
            Mathf.Abs(local.x),
            Mathf.Abs(local.y)
        );

        Vector2 roundedDistance =
            distance - innerHalfSize;

        Vector2 outsideDistance = new Vector2(
            Mathf.Max(
                roundedDistance.x,
                0f
            ),
            Mathf.Max(
                roundedDistance.y,
                0f
            )
        );

        float outsideLength =
            outsideDistance.magnitude;

        float insideDistance =
            Mathf.Min(
                Mathf.Max(
                    roundedDistance.x,
                    roundedDistance.y
                ),
                0f
            );

        float signedDistance =
            outsideLength +
            insideDistance -
            finalRadius;

        return signedDistance <= 0f;
    }
}