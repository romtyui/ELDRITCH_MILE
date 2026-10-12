using TMPro;
using UnityEngine;

public class DamagePopupUI : MonoBehaviour
{
    [Header("Refs")]
    public TMP_Text valueText;
    public CanvasGroup canvasGroup;
    public RectTransform rectTransform;

    [Header("Animation")]
    public float lifetime = 0.8f;
    public Vector2 randomOffset = new Vector2(40f, 20f);

    [Min(0f)] public float arcHeight = 80f;
    [Min(0f)] public float fallDistance = 100f;


    [Tooltip("整段軌跡的水平位移；正值往右，負值往左，0 表示只做上下移動。")]
    public float horizontalDistance = 0f;
    [Range(0.05f, 0.95f)]
    [Tooltip("總時間中，用於上升到最高點的比例；剩餘時間用於下降到最低點。")]
    public float movementRiseRatio = 0.5f;

    [Header("Scale Animation")]
    [Min(0f)] public float maximumScaleMultiplier = 1f;
    [Min(0f)] public float minimumScaleMultiplier = 0f;

    [Range(0.05f, 0.95f)]
    [Tooltip("整段 lifetime 中，用於從最小放大到最大的時間比例；剩餘時間用於縮小。")]
    public float scaleGrowRatio = 0.5f;

    private Vector3 originalTextScale;

    private float timer;
    private Vector2 startPosition;
    private void Awake()
    {
        if (valueText != null) originalTextScale = valueText.rectTransform.localScale;
    }
    public void Setup(int value, Vector2 screenPosition)
    {
        if (valueText != null)
            valueText.text = value.ToString();

        Vector2 offset = new Vector2(Random.Range(-randomOffset.x, randomOffset.x), Random.Range(-randomOffset.y, randomOffset.y));
        startPosition = screenPosition + offset;

        if (rectTransform != null)
            rectTransform.position = startPosition;

        if (valueText != null)
            valueText.rectTransform.localScale = originalTextScale * minimumScaleMultiplier;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        timer = 0f;
    }
    public void SetupLocal(int value, Vector2 anchoredPosition)
    {
        if (valueText != null)
            valueText.text = value.ToString();

        startPosition = anchoredPosition;

        if (rectTransform != null)
            rectTransform.anchoredPosition = startPosition;

        if (valueText != null)
            valueText.rectTransform.localScale = originalTextScale * minimumScaleMultiplier;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        timer = 0f;
    }
    private void Update()
    {
        timer += Time.deltaTime;

        float safeLifetime = Mathf.Max(0.0001f, lifetime);
        float t = Mathf.Clamp01(timer / safeLifetime);
        float height;

        if (t < movementRiseRatio)
        {
            float riseProgress = Mathf.Clamp01(t / movementRiseRatio);
            height = arcHeight * (1f - (1f - riseProgress) * (1f - riseProgress));
        }
        else
        {
            float fallProgress = Mathf.Clamp01((t - movementRiseRatio) / (1f - movementRiseRatio));
            height = Mathf.Lerp(arcHeight, -fallDistance, fallProgress * fallProgress);
        }

        Vector2 currentPosition = startPosition + new Vector2(horizontalDistance * t, height);

        if (rectTransform != null)
            rectTransform.anchoredPosition = currentPosition;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f - t;

        float growDuration = safeLifetime * scaleGrowRatio;
        float scaleMultiplier;

        if (timer < growDuration)
        {
            float growProgress = Mathf.Clamp01(timer / growDuration);
            scaleMultiplier = Mathf.Lerp(minimumScaleMultiplier, maximumScaleMultiplier, growProgress);
        }
        else
        {
            float shrinkDuration = safeLifetime - growDuration;
            float shrinkProgress = Mathf.Clamp01((timer - growDuration) / shrinkDuration);
            scaleMultiplier = Mathf.Lerp(maximumScaleMultiplier, minimumScaleMultiplier, shrinkProgress);
        }

        if (valueText != null)
            valueText.rectTransform.localScale = originalTextScale * scaleMultiplier;

        if (t >= 1f)
            Destroy(gameObject);
    }

}