using System.Collections;
using TMPro;
using UnityEngine;

public class TurnPhaseBannerUI : MonoBehaviour
{
    [Header("UI")]
    public CanvasGroup canvasGroup;
    public TMP_Text messageText;

    [Header("Player Turn Text")]
    public string playerTurnTitle = "玩家回合";
    public Color playerTurnTitleColor = Color.white;
    public Color playerTurnNumberColor = Color.yellow;

    [Header("Enemy Turn Text")]
    public string enemyTurnTitle = "敵方回合";
    public Color enemyTurnColor = Color.red;

    [Header("Animation")]
    public float fadeInDuration = 0.2f;
    public float fadeOutDuration = 0.2f;
    public float enemyBannerHoldTime = 0.8f;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        HideImmediately();
    }

    public IEnumerator ShowPlayerTurn(int turnNumber)
    {
        string titleColorHex = ColorUtility.ToHtmlStringRGBA(playerTurnTitleColor);
        string numberColorHex = ColorUtility.ToHtmlStringRGBA(playerTurnNumberColor);

        string message =
            $"<color=#{titleColorHex}>{playerTurnTitle}</color>\n" +
            $"<color=#{numberColorHex}>第 {turnNumber} 回合</color>";

        yield return ShowMessage(message);
    }

    public IEnumerator ShowEnemyTurn()
    {
        string enemyColorHex = ColorUtility.ToHtmlStringRGBA(enemyTurnColor);

        string message =
            $"<color=#{enemyColorHex}>{enemyTurnTitle}</color>";

        yield return ShowMessage(message);
        yield return new WaitForSeconds(enemyBannerHoldTime);
        yield return Hide();
    }

    public IEnumerator ShowMessage(string message)
    {
        gameObject.SetActive(true);

        if (messageText != null)
        {
            messageText.richText = true;
            messageText.text = message;
        }

        yield return FadeTo(1f, fadeInDuration);
    }

    public IEnumerator Hide()
    {
        yield return FadeTo(0f, fadeOutDuration);
        gameObject.SetActive(false);
    }

    public void HideImmediately()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        gameObject.SetActive(false);
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (canvasGroup == null)
            yield break;

        float startAlpha = canvasGroup.alpha;
        float timer = 0f;

        if (duration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
            yield break;
        }

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }
}