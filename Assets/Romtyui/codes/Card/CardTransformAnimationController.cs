using System.Collections;
using UnityEngine;

public class CardTransformAnimationController : MonoBehaviour
{
    [Header("Refs")]
    public RectTransform animationRoot;
    public CardViewUI cardPrefab;

    [Header("Played Transform Card")]
    public RectTransform centerPoint;
    public float moveToCenterDuration = 0.35f;
    public float specialAnimationWaitTime = 1f;

    [Header("Bag Animation")]
    public RectTransform bagSpawnPoint;
    public RectTransform bagPoint;
    public float bagMoveDuration = 0.35f;
    public float bagCardScale = 0.6f;
    public float bagCardYScale = 1f;
    public float bagSpawnStayTime = 0.6f;

    public Transform AnimationRoot
    {
        get
        {
            if (animationRoot != null)
                return animationRoot;

            return transform;
        }
    }

    public IEnumerator MovePlayedCardToCenterAndWait(CardViewUI playedCardView)
    {
        if (playedCardView == null)
        {
            yield return new WaitForSeconds(specialAnimationWaitTime);
            yield break;
        }

        RectTransform rect = playedCardView.GetComponent<RectTransform>();

        if (rect == null)
        {
            yield return new WaitForSeconds(specialAnimationWaitTime);
            yield break;
        }

        if (animationRoot != null)
            rect.SetParent(animationRoot, true);

        if (centerPoint != null)
        {
            yield return MoveRectWorld(rect, centerPoint.position, moveToCenterDuration);
        }

        // 之後這裡可以換成：
        // 1. 顯示專屬物件
        // 2. 播放 IK 動畫
        // 3. 等動畫結束
        yield return new WaitForSeconds(specialAnimationWaitTime);
    }

    public IEnumerator PlayBagTransformAnimation(CardTransformResult result)
    {
        if (result == null || !result.success)
            yield break;

        if (cardPrefab == null)
        {
            Debug.LogWarning("[CardTransformAnimationController] cardPrefab 沒有指定");
            yield break;
        }

        if (bagSpawnPoint == null || bagPoint == null)
        {
            Debug.LogWarning("[CardTransformAnimationController] bagSpawnPoint 或 bagPoint 沒有指定");
            yield break;
        }

        Transform parent = AnimationRoot;

        CardViewUI bagCardView = Instantiate(cardPrefab, parent);
        bagCardView.gameObject.SetActive(true);

        // 這裡顯示「被選中、被替換的原始牌」
        // 例如 M 被變成 M_A，這裡顯示 M 進入包包
        CardInstance displayInstance = new CardInstance(result.resultCardData);
        bagCardView.Bind(displayInstance);

        RectTransform bagCardRect = bagCardView.GetComponent<RectTransform>();
        bagCardRect.position = bagSpawnPoint.position;
        bagCardRect.localScale = new Vector3(
             bagCardScale,
             bagCardYScale,
             bagCardScale
         );
        bagCardRect.localRotation = Quaternion.identity;

        CanvasGroup canvasGroup = bagCardView.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = bagCardView.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 1f;

        // M_A 出現在包包上方後，先停留一段時間
        if (bagSpawnStayTime > 0f)
            yield return new WaitForSeconds(bagSpawnStayTime);

        yield return MoveRectWorld(bagCardRect, bagPoint.position, bagMoveDuration);

        float fadeTimer = 0f;
        float fadeDuration = 0.15f;

        while (fadeTimer < fadeDuration)
        {
            fadeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(fadeTimer / fadeDuration);

            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            bagCardRect.localScale = Vector3.Lerp(Vector3.one * bagCardScale, Vector3.one * 0.2f, t);

            yield return null;
        }

        Destroy(bagCardView.gameObject);

        Debug.Log($"[變化動畫] {result.originalCardData.cardName} 進入包包，變成 {result.resultCardData.cardName}");
    }

    public IEnumerator FinishPlayedTransformCard(CardViewUI playedCardView)
    {
        if (playedCardView == null)
            yield break;

        CanvasGroup canvasGroup = playedCardView.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = playedCardView.gameObject.AddComponent<CanvasGroup>();

        RectTransform rect = playedCardView.GetComponent<RectTransform>();

        float timer = 0f;
        float duration = 0.2f;

        Vector3 startScale = rect.localScale;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            rect.localScale = Vector3.Lerp(startScale, Vector3.one * 0.2f, t);

            yield return null;
        }

        Destroy(playedCardView.gameObject);
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