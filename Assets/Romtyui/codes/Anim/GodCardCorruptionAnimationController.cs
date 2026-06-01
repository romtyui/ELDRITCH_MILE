using System.Collections;
using UnityEngine;

public class GodCardCorruptionAnimationController : MonoBehaviour
{
    [Header("Roots")]
    public RectTransform animationRoot;

    [Header("Played God Card")]
    public RectTransform centerPoint;
    public float moveToCenterDuration = 0.35f;
    public float shakeDuration = 0.45f;
    public float shakeStrength = 12f;

    [Header("Blackout")]
    public CanvasGroup blackoutCanvasGroup;

    [Range(0f, 1f)]
    public float blackoutAlpha = 0.75f;

    public float blackoutFadeDuration = 0.25f;

    [Header("Tentacle IK Animation")]
    public GameObject tentacleRoot;
    public Animator tentacleAnimator;
    public string tentacleTriggerName = "PlayGodCorruption";

    [Tooltip("觸手伸進書口後，幾秒後執行污染並顯示污染牌。")]
    public float revealCardDelay = 1.0f;

    [Tooltip("整段觸手動畫最短播放時間。避免太快結束。")]
    public float minTentacleAnimTime = 2.2f;

    [Header("Book / Card Reveal")]
    public RectTransform bookMouthPoint;
    public RectTransform corruptedCardRevealPoint;
    public CardViewUI cardPreviewPrefab;

    public float cardRevealMoveDuration = 0.35f;
    public float cardShowStayTime = 0.85f;
    public float cardReturnDuration = 0.35f;
    public float previewCardScale = 0.75f;

    [Header("End")]
    public float godCardFadeDuration = 0.2f;

    public Transform AnimationRoot
    {
        get
        {
            if (animationRoot != null)
                return animationRoot;

            return transform;
        }
    }

    public IEnumerator PlayGodCorruptionSequence(
        CardViewUI playedCardView,
        TransformRandomCardByPoolEffectData transformEffect,
        CardResolveContext context
    )
    {
        if (playedCardView == null || transformEffect == null || context == null)
            yield break;

        RectTransform playedCardRect = playedCardView.GetComponent<RectTransform>();

        if (playedCardRect == null)
            yield break;

        // 1. 畫面變黑，並擋住滑鼠操作
        yield return FadeBlackout(true);

        // 2. 把神牌移到動畫層
        playedCardRect.SetParent(AnimationRoot, true);

        // 3. 神牌飛到畫面中央
        if (centerPoint != null)
            yield return MoveRectWorld(playedCardRect, centerPoint.position, moveToCenterDuration);

        // 4. 神牌震動
        yield return ShakeRect(playedCardRect, shakeDuration, shakeStrength);

        // 5. 觸手出現 / 播 IK 動畫
        PlayTentacleAnimation();

        // 6. 等主觸手伸進書口
        if (revealCardDelay > 0f)
            yield return new WaitForSeconds(revealCardDelay);

        // 7. 在這個時間點真正執行污染
        CardTransformResult result = transformEffect.ExecuteTransform(context);

        // 8. 顯示污染後的牌：M_A
        yield return RevealCorruptedCard(result);

        // 9. 確保觸手動畫有播到基本長度
        if (minTentacleAnimTime > 0f)
            yield return new WaitForSeconds(minTentacleAnimTime);

        // 10. 神牌消失
        yield return FinishPlayedGodCard(playedCardView);

        // 11. 關掉觸手
        if (tentacleRoot != null)
            tentacleRoot.SetActive(false);

        // 12. 黑幕消失，玩家可以繼續操作
        yield return FadeBlackout(false);
    }

    private void PlayTentacleAnimation()
    {
        if (tentacleRoot != null)
            tentacleRoot.SetActive(true);

        if (tentacleAnimator != null)
        {
            Debug.Log($"[GodCardCorruptionAnimation] SetTrigger: {tentacleTriggerName}");

            tentacleAnimator.ResetTrigger(tentacleTriggerName);
            tentacleAnimator.SetTrigger(tentacleTriggerName);
        }
        else
        {
            Debug.LogWarning("[GodCardCorruptionAnimation] tentacleAnimator 沒有指定");
        }
    }

    private IEnumerator RevealCorruptedCard(CardTransformResult result)
    {
        if (result == null || !result.success)
            yield break;

        if (result.resultCardData == null)
            yield break;

        if (cardPreviewPrefab == null || bookMouthPoint == null || corruptedCardRevealPoint == null)
            yield break;

        CardViewUI preview = Instantiate(cardPreviewPrefab, AnimationRoot);
        preview.gameObject.SetActive(true);

        // 顯示污染後的牌
        CardInstance displayInstance = new CardInstance(result.resultCardData);
        preview.Bind(displayInstance);

        RectTransform previewRect = preview.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = preview.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = preview.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 1f;

        previewRect.position = bookMouthPoint.position;
        previewRect.localScale = Vector3.one * previewCardScale;
        previewRect.localRotation = Quaternion.identity;

        // 從書口拿出來
        yield return MoveRectWorld(
            previewRect,
            corruptedCardRevealPoint.position,
            cardRevealMoveDuration
        );

        // 讓玩家看清楚是哪張污染牌
        if (cardShowStayTime > 0f)
            yield return new WaitForSeconds(cardShowStayTime);

        // 丟回書口
        yield return MoveRectWorld(
            previewRect,
            bookMouthPoint.position,
            cardReturnDuration
        );

        // 淡出 / 消失
        float timer = 0f;
        float fadeDuration = 0.15f;
        Vector3 startScale = previewRect.localScale;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / fadeDuration);

            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            previewRect.localScale = Vector3.Lerp(startScale, Vector3.one * 0.2f, t);

            yield return null;
        }

        Destroy(preview.gameObject);
    }

    private IEnumerator FadeBlackout(bool show)
    {
        if (blackoutCanvasGroup == null)
            yield break;

        blackoutCanvasGroup.blocksRaycasts = show;
        blackoutCanvasGroup.interactable = show;

        float start = blackoutCanvasGroup.alpha;
        float end = show ? blackoutAlpha : 0f;

        float timer = 0f;

        while (timer < blackoutFadeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / blackoutFadeDuration);
            float smoothT = t * t * (3f - 2f * t);

            blackoutCanvasGroup.alpha = Mathf.Lerp(start, end, smoothT);

            yield return null;
        }

        blackoutCanvasGroup.alpha = end;

        if (!show)
        {
            blackoutCanvasGroup.blocksRaycasts = false;
            blackoutCanvasGroup.interactable = false;
        }
    }

    private IEnumerator FinishPlayedGodCard(CardViewUI playedCardView)
    {
        if (playedCardView == null)
            yield break;

        RectTransform rect = playedCardView.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = playedCardView.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = playedCardView.gameObject.AddComponent<CanvasGroup>();

        float timer = 0f;
        Vector3 startScale = rect.localScale;

        while (timer < godCardFadeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / godCardFadeDuration);

            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            rect.localScale = Vector3.Lerp(startScale, Vector3.one * 0.2f, t);

            yield return null;
        }

        Destroy(playedCardView.gameObject);
    }

    private IEnumerator ShakeRect(RectTransform rect, float duration, float strength)
    {
        if (rect == null)
            yield break;

        Vector3 originalPosition = rect.position;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            Vector3 offset = new Vector3(
                Random.Range(-strength, strength),
                Random.Range(-strength, strength),
                0f
            );

            rect.position = originalPosition + offset;

            yield return null;
        }

        rect.position = originalPosition;
    }

    private IEnumerator MoveRectWorld(RectTransform rect, Vector3 targetWorldPosition, float duration)
    {
        if (rect == null)
            yield break;

        Vector3 startPosition = rect.position;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / duration);
            float smoothT = t * t * (3f - 2f * t);

            rect.position = Vector3.Lerp(startPosition, targetWorldPosition, smoothT);

            yield return null;
        }

        rect.position = targetWorldPosition;
    }
}