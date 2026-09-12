using System.Collections;
using TMPro;
using UnityEngine;

public class AnimatedNumberTextUI : MonoBehaviour
{
    [Header("Text")]
    public TMP_Text valueText;

    [Header("Animation")]
    [Tooltip("數字變化動畫需要幾秒")]
    public float animationDuration = 0.4f;

    [Tooltip("是否使用真實時間。建議開啟，這樣 Time.timeScale = 0 時 UI 動畫仍然能跑")]
    public bool useUnscaledTime = true;

    [Header("Animation Color")]
    [Tooltip("數字下降時使用的顏色")]
    public Color decreaseColor = Color.red;

    [Tooltip("數字上升時使用的顏色")]
    public Color increaseColor = Color.green;

    [Tooltip("數字動畫期間是否變色")]
    public bool changeColorDuringAnimation = true;

    [Header("Format")]
    [Tooltip("是否顯示最大值，例如 75 / 100")]
    public bool showMaxValue;

    public string separator = " / ";

    private Coroutine animationCoroutine;

    private int displayedValue;
    private int displayedMaxValue;

    private bool initialized;

    private Color originalColor;
    private bool colorInitialized;

    private void Awake()
    {
        if (valueText == null)
            valueText = GetComponent<TMP_Text>();

        CacheOriginalColor();
    }

    private void CacheOriginalColor()
    {
        if (valueText == null)
            return;

        originalColor = valueText.color;
        colorInitialized = true;
    }

    public void SetValueImmediate(int value)
    {
        initialized = true;

        displayedValue = value;

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        RestoreOriginalColor();
        RefreshText();
    }

    public void SetValueImmediate(int value, int maxValue)
    {
        initialized = true;

        displayedValue = value;
        displayedMaxValue = maxValue;

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        RestoreOriginalColor();
        RefreshText();
    }

    public void SetValue(int targetValue)
    {
        if (!initialized)
        {
            SetValueImmediate(targetValue);
            return;
        }

        StartNumberAnimation(targetValue, displayedMaxValue);
    }

    public void SetValue(int targetValue, int maxValue)
    {
        if (!initialized)
        {
            SetValueImmediate(targetValue, maxValue);
            return;
        }

        StartNumberAnimation(targetValue, maxValue);
    }

    private void StartNumberAnimation(int targetValue, int maxValue)
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        if (changeColorDuringAnimation && valueText != null)
        {
            if (!colorInitialized)
                CacheOriginalColor();

            if (targetValue < displayedValue)
            {
                valueText.color = decreaseColor;
            }
            else if (targetValue > displayedValue)
            {
                valueText.color = increaseColor;
            }
            else
            {
                RestoreOriginalColor();
            }
        }

        animationCoroutine = StartCoroutine(
            AnimateValueRoutine(targetValue, maxValue)
        );
    }

    private IEnumerator AnimateValueRoutine(int targetValue, int maxValue)
    {
        int startValue = displayedValue;

        displayedMaxValue = maxValue;

        if (startValue == targetValue)
        {
            displayedValue = targetValue;
            RefreshText();
            RestoreOriginalColor();

            animationCoroutine = null;
            yield break;
        }

        if (animationDuration <= 0f)
        {
            displayedValue = targetValue;
            RefreshText();
            RestoreOriginalColor();

            animationCoroutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            float deltaTime = useUnscaledTime
                ? Time.unscaledDeltaTime
                : Time.deltaTime;

            elapsed += deltaTime;

            float t = Mathf.Clamp01(elapsed / animationDuration);

            displayedValue = Mathf.RoundToInt(
                Mathf.Lerp(startValue, targetValue, t)
            );

            RefreshText();

            yield return null;
        }

        displayedValue = targetValue;
        RefreshText();

        RestoreOriginalColor();

        animationCoroutine = null;
    }

    private void RestoreOriginalColor()
    {
        if (valueText == null)
            return;

        if (!colorInitialized)
            return;

        valueText.color = originalColor;
    }

    private void RefreshText()
    {
        if (valueText == null)
            return;

        if (showMaxValue)
        {
            valueText.text =
                displayedValue +
                separator +
                displayedMaxValue;
        }
        else
        {
            valueText.text = displayedValue.ToString();
        }
    }
}