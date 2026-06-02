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

    [Header("Default God Animation")]
    public GodCardAnimationData defaultAnimationData;

    [Header("Runtime")]
    private GodCardAnimationData currentAnimationData;
    private bool animationFinished;

    [Header("Blackout")]
    public CanvasGroup blackoutCanvasGroup;

    [Range(0f, 1f)]
    public float blackoutAlpha = 0.75f;

    public float blackoutFadeDuration = 0.25f;

    [Header("Tentacle IK Animation")]
    public GameObject tentacleRoot;
    public Animator tentacleAnimator;
    public string tentacleTriggerName = "PlayGodCorruption";

    [Header("Animated Corrupted Card Template")]
    [Tooltip("動畫裡已經存在的污染牌模板，不再由程式 Instantiate。")]
    public CardViewUI animatedCorruptedCardTemplate;

    [Tooltip("控制動畫模板顯示/隱藏。建議動畫也控制這個 CanvasGroup Alpha。")]
    public CanvasGroup animatedCardCanvasGroup;

    [Header("Fallback Wait")]
    [Tooltip("如果動畫事件沒有呼叫結束，最多等待幾秒避免卡死。")]
    public float animationTimeout = 5f;

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
    CardResolveContext context,
    GodCardAnimationData animationData
)
    {
        if (playedCardView == null || transformEffect == null || context == null)
            yield break;

        currentAnimationData = ResolveAnimationData(animationData);
        animationFinished = false;

        RectTransform playedCardRect = playedCardView.GetComponent<RectTransform>();

        if (playedCardRect == null)
            yield break;

        // 1. 黑幕開啟
        yield return FadeBlackout(true);

        // 2. 神牌移到動畫層
        playedCardRect.SetParent(AnimationRoot, true);

        // 3. 神牌飛向中央
        if (centerPoint != null)
            yield return MoveRectWorld(playedCardRect, centerPoint.position, moveToCenterDuration);

        // 4. 神牌震動
        yield return ShakeRect(playedCardRect, shakeDuration, shakeStrength);

        // 5. 先執行污染，取得污染後的牌資料
        CardTransformResult result = transformEffect.ExecuteTransform(context);

        // 6. 把污染後的牌資料灌進動畫模板
        BindCorruptedCardToAnimationTemplate(result);

        // 7. 播放這張神牌指定的動畫，沒有指定就播預設動畫
        yield return PlayGodAnimationRoutine(currentAnimationData);

        // 8. 等動畫結束事件
        yield return WaitForAnimationFinished(currentAnimationData);

        // 9. 神牌消失
        yield return FinishPlayedGodCard(playedCardView);

        // 10. 重置模板
        ResetAnimatedCardTemplate();

        // 11. 關閉觸手根物件
        if (tentacleRoot != null)
            tentacleRoot.SetActive(false);

        // 12. 黑幕關閉
        yield return FadeBlackout(false);

        currentAnimationData = null;
    }

    private void BindCorruptedCardToAnimationTemplate(CardTransformResult result)
    {
        if (animatedCorruptedCardTemplate == null)
        {
            Debug.LogWarning("[GodCardCorruptionAnimation] animatedCorruptedCardTemplate 沒有指定");
            return;
        }

        if (result == null || !result.success || result.resultCardData == null)
        {
            Debug.LogWarning("[GodCardCorruptionAnimation] 沒有成功取得污染後的牌資料");
            return;
        }

        CardInstance displayInstance = new CardInstance(result.resultCardData);
        animatedCorruptedCardTemplate.Bind(displayInstance);

        if (animatedCardCanvasGroup == null)
            animatedCardCanvasGroup = animatedCorruptedCardTemplate.GetComponent<CanvasGroup>();

        if (animatedCardCanvasGroup != null)
        {
            // 一開始先保持透明，之後由動畫把 Alpha 拉到 1
            animatedCardCanvasGroup.alpha = 0f;
            animatedCardCanvasGroup.blocksRaycasts = false;
            animatedCardCanvasGroup.interactable = false;
        }

        animatedCorruptedCardTemplate.gameObject.SetActive(true);

        Debug.Log($"[GodCardCorruptionAnimation] 動畫模板綁定污染牌：{result.resultCardData.cardName}");
    }

    private GodCardAnimationData ResolveAnimationData(GodCardAnimationData cardAnimationData)
    {
        if (cardAnimationData != null)
        {
            Debug.Log($"[GodCardAnimation] 使用神牌專屬動畫：{cardAnimationData.animationName}");
            return cardAnimationData;
        }

        if (defaultAnimationData != null)
        {
            Debug.Log($"[GodCardAnimation] 使用預設神牌動畫：{defaultAnimationData.animationName}");
            return defaultAnimationData;
        }

        Debug.LogWarning("[GodCardAnimation] 沒有專屬動畫，也沒有預設動畫，將使用 Controller 上的預設 Trigger");
        return null;
    }

    private IEnumerator PlayGodAnimationRoutine(GodCardAnimationData animationData)
    {
        if (tentacleRoot != null)
            tentacleRoot.SetActive(true);

        // 等一幀，避免 SetActive 後 Animator 還沒初始化
        yield return null;

        if (tentacleAnimator == null)
        {
            Debug.LogWarning("[GodCardAnimation] tentacleAnimator 沒有指定");
            yield break;
        }

        string trigger = tentacleTriggerName;

        if (animationData != null)
        {
            if (animationData.animatorController != null)
                tentacleAnimator.runtimeAnimatorController = animationData.animatorController;

            if (!string.IsNullOrEmpty(animationData.triggerName))
                trigger = animationData.triggerName;
        }

        Debug.Log($"[GodCardAnimation] SetTrigger: {trigger}");

        tentacleAnimator.ResetTrigger(trigger);
        tentacleAnimator.SetTrigger(trigger);
    }
    private void ResetAnimatedCardTemplate()
    {
        if (animatedCardCanvasGroup != null)
        {
            animatedCardCanvasGroup.alpha = 0f;
            animatedCardCanvasGroup.blocksRaycasts = false;
            animatedCardCanvasGroup.interactable = false;
        }
    }

    private IEnumerator PlayTentacleAnimationRoutine()
    {
        if (tentacleRoot != null)
            tentacleRoot.SetActive(true);

        // 等一幀，避免剛 SetActive Animator 還沒初始化就吃不到 Trigger
        yield return null;

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

    private IEnumerator WaitForAnimationFinished(GodCardAnimationData animationData)
    {
        float timeout = animationTimeout;

        if (animationData != null && animationData.animationTimeout > 0f)
            timeout = animationData.animationTimeout;

        float timer = 0f;

        while (!animationFinished)
        {
            timer += Time.deltaTime;

            if (timer >= timeout)
            {
                Debug.LogWarning("[GodCardAnimation] 等待神牌動畫結束逾時，強制結束");
                break;
            }

            yield return null;
        }
    }

    // 給 Animation Event 呼叫
    public void AnimEvent_GodCorruptionFinished()
    {
        Debug.Log("[GodCardCorruptionAnimation] 收到動畫結束事件");
        animationFinished = true;
    }

    private IEnumerator FadeBlackout(bool show)
    {
        if (blackoutCanvasGroup == null)
            yield break;

        blackoutCanvasGroup.blocksRaycasts = show;
        blackoutCanvasGroup.interactable = show;

        float targetAlpha = blackoutAlpha;

        if (currentAnimationData != null)
            targetAlpha = currentAnimationData.blackoutAlpha;

        float start = blackoutCanvasGroup.alpha;
        float end = show ? targetAlpha : 0f;

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