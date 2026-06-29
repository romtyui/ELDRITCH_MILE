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
    public float moveUpDistance = 80f;
    public Vector2 randomOffset = new Vector2(40f, 20f);

    private float timer;
    private Vector2 startPosition;
    private Vector2 endPosition;

    public void Setup(int value, Vector2 screenPosition)
    {
        if (valueText != null)
            valueText.text = value.ToString();

        Vector2 offset = new Vector2(
            Random.Range(-randomOffset.x, randomOffset.x),
            Random.Range(-randomOffset.y, randomOffset.y)
        );

        startPosition = screenPosition + offset;
        endPosition = startPosition + Vector2.up * moveUpDistance;

        if (rectTransform != null)
            rectTransform.position = startPosition;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        timer = 0f;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        float t = Mathf.Clamp01(timer / lifetime);

        if (rectTransform != null)
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, t);

        if (canvasGroup != null)
            canvasGroup.alpha = 1f - t;

        if (t >= 1f)
            Destroy(gameObject);
    }
    public void SetupLocal(int value, Vector2 anchoredPosition)
    {
        if (valueText != null)
            valueText.text = value.ToString();

        startPosition = anchoredPosition;
        endPosition = startPosition + Vector2.up * moveUpDistance;

        if (rectTransform != null)
            rectTransform.anchoredPosition = startPosition;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        timer = 0f;
    }
}