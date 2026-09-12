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

    [Header("Format")]
    [Tooltip("是否顯示最大值，例如 75 / 100")]
    public bool showMaxValue;

    public string separator = " / ";

    private Coroutine animationCoroutine;

    private int displayedValue;
    private int displayedMaxValue;

    private bool initialized;

    private void Awake()
    {
        if (valueText == null)
            valueText = GetComponent<TMP_Text>();
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

            animationCoroutine = null;
            yield break;
        }

        if (animationDuration <= 0f)
        {
            displayedValue = targetValue;
            RefreshText();

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

        animationCoroutine = null;
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