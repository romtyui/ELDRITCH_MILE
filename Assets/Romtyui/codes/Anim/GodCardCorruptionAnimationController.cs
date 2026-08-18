using System.Collections;
using UnityEngine;

public class GodCardCorruptionAnimationController : MonoBehaviour
{
    // =========================================================
    // Roots
    // =========================================================

    [Header("Roots")]

    [Tooltip("神牌動畫 Prefab 生成時使用的父物件。可以放在專門的動畫 Canvas 底下。")]
    public RectTransform animationRoot;

    [Tooltip("打出的神牌 CardViewUI 使用的父物件。建議放在卡牌原本使用的 Canvas 底下。")]
    public RectTransform cardRoot;


    // =========================================================
    // Played God Card
    // =========================================================

    [Header("Played God Card")]

    [Tooltip("打出的神牌移動到中央時的目標位置。建議和 Card Root 使用相同 Canvas。")]
    public RectTransform centerPoint;

    public float moveToCenterDuration = 0.35f;

    public float shakeDuration = 0.45f;

    public float shakeStrength = 12f;


    // =========================================================
    // Default God Animation
    // =========================================================

    [Header("Default God Animation")]

    [Tooltip("如果卡片沒有指定專屬神牌動畫資料，可以使用這個預設動畫資料。")]
    public GodCardAnimationData defaultAnimationData;


    // =========================================================
    // Runtime
    // =========================================================

    [Header("Runtime")]

    [SerializeField]
    private GodCardAnimationData currentAnimationData;

    [SerializeField]
    private bool animationFinished;

    [SerializeField]
    private GameObject currentAnimationObject;

    [SerializeField]
    private Animator currentAnimationAnimator;


    // =========================================================
    // Blackout
    // =========================================================

    [Header("Blackout")]

    public CanvasGroup blackoutCanvasGroup;

    [Range(0f, 1f)]
    public float blackoutAlpha = 0.75f;

    public float blackoutFadeDuration = 0.25f;


    // =========================================================
    // Animation Prefab
    // =========================================================

    [Header("God Animation Prefab")]

    [Tooltip(
        "如果開啟，生成的神牌動畫 Prefab 如果有 RectTransform，" +
        "會自動拉滿 Animation Root。"
    )]
    public bool stretchAnimationPrefabToRoot = false;


    // =========================================================
    // Animated Corrupted Card Template
    // =========================================================

    [Header("Animated Corrupted Card Template")]

    [Tooltip("動畫中用來顯示污染後卡牌的 CardViewUI 模板。")]
    public CardViewUI animatedCorruptedCardTemplate;

    [Tooltip("控制污染後卡牌模板的顯示 / 隱藏。")]
    public CanvasGroup animatedCardCanvasGroup;


    // =========================================================
    // Fallback Wait
    // =========================================================

    [Header("Fallback Wait")]

    [Tooltip("如果動畫沒有送出結束 Animation Event，最多等待幾秒，避免流程卡死。")]
    public float animationTimeout = 5f;


    // =========================================================
    // End
    // =========================================================

    [Header("End")]

    public float godCardFadeDuration = 0.2f;


    // =========================================================
    // Animation Root
    // =========================================================

    public Transform AnimationRoot
    {
        get
        {
            if (animationRoot != null)
                return animationRoot;

            return transform;
        }
    }

    [Header("UI Hide During God Animation")]
    [Tooltip("神牌動畫播放期間要暫時隱藏的 UI 物件。")]
    public GameObject hideDuringGodAnimationUI;
    // =========================================================
    // Card Root
    // =========================================================

    public Transform CardRoot
    {
        get
        {
            if (cardRoot != null)
                return cardRoot;

            return transform;
        }
    }


    // =========================================================
    // 神牌完整動畫流程
    // =========================================================

    public IEnumerator PlayGodCorruptionSequence(
        CardViewUI playedCardView,
        TransformRandomCardByPoolEffectData transformEffect,
        CardResolveContext context,
        GodCardAnimationData animationData
    )
    {
        if (playedCardView == null)
        {
            Debug.LogWarning(
                "[GodCardAnimation] playedCardView 是 null"
            );

            yield break;
        }

        if (transformEffect == null)
        {
            Debug.LogWarning(
                "[GodCardAnimation] transformEffect 是 null"
            );

            yield break;
        }

        if (context == null)
        {
            Debug.LogWarning(
                "[GodCardAnimation] context 是 null"
            );

            yield break;
        }


        // =====================================================
        // Root 檢查
        // =====================================================

        if (animationRoot == null)
        {
            Debug.LogWarning(
                "[GodCardAnimation] Animation Root 沒有指定，" +
                "動畫 Prefab 會生成在 Controller 自己底下。"
            );
        }

        if (cardRoot == null)
        {
            Debug.LogWarning(
                "[GodCardAnimation] Card Root 沒有指定，" +
                "打出的神牌會移到 Controller 自己底下。"
            );
        }


        // =====================================================
        // 初始化
        // =====================================================

        currentAnimationData =
            ResolveAnimationData(animationData);

        animationFinished = false;


        RectTransform playedCardRect =
            playedCardView.GetComponent<RectTransform>();

        if (playedCardRect == null)
        {
            Debug.LogWarning(
                "[GodCardAnimation] playedCardView 找不到 RectTransform"
            );

            yield break;
        }

        // =====================================================
        // 1. 隱藏神牌動畫期間不需要的 UI
        // =====================================================

        HideUIForGodAnimation();


        // =====================================================
        // 2. 黑幕開啟
        // =====================================================

        yield return FadeBlackout(true);


        // =====================================================
        // 2. 打出的神牌移到 Card Root
        //
        // 注意：
        // 現在不是 Animation Root。
        //
        // 這樣 Animation Canvas 和 Card Canvas
        // 就可以完全分開。
        // =====================================================

        playedCardRect.SetParent(
            CardRoot,
            true
        );

        playedCardRect.SetAsLastSibling();


        // =====================================================
        // 3. 神牌飛向中央
        // =====================================================

        if (centerPoint != null)
        {
            yield return MoveRectWorld(
                playedCardRect,
                centerPoint.position,
                moveToCenterDuration
            );
        }
        else
        {
            Debug.LogWarning(
                "[GodCardAnimation] Center Point 沒有指定"
            );
        }


        // =====================================================
        // 4. 神牌震動
        // =====================================================

        yield return ShakeRect(
            playedCardRect,
            shakeDuration,
            shakeStrength
        );


        // =====================================================
        // 5. 執行污染
        // =====================================================

        CardTransformResult result =
            transformEffect.ExecuteTransform(context);


        // =====================================================
        // 6. 把污染後的牌資料灌進動畫模板
        // =====================================================

        BindCorruptedCardToAnimationTemplate(
            result
        );


        // =====================================================
        // 7. 生成並播放這張神牌自己的動畫 Prefab
        // =====================================================

        yield return PlayGodAnimationRoutine(
            currentAnimationData
        );


        // =====================================================
        // 8. 等動畫結束
        // =====================================================

        yield return WaitForAnimationFinished(
            currentAnimationData
        );


        // =====================================================
        // 9. 原本打出去的神牌消失
        // =====================================================

        yield return FinishPlayedGodCard(
            playedCardView
        );


        // =====================================================
        // 10. 重置污染牌 Template
        // =====================================================

        ResetAnimatedCardTemplate();


        // =====================================================
        // 11. 刪除這次生成的動畫 Prefab
        // =====================================================

        DestroyCurrentAnimationPrefab();


        // =====================================================
        // 12. 黑幕關閉
        // =====================================================

        yield return FadeBlackout(false);


        // =====================================================
        // 13. 恢復原本暫時隱藏的 UI
        // =====================================================

        ShowUIAfterGodAnimation();


        // =====================================================
        // 14. Runtime 清除
        // =====================================================

        currentAnimationData = null;

        animationFinished = false;

        currentAnimationObject = null;

        currentAnimationAnimator = null;
    }


    // =========================================================
    // 綁定污染後卡牌
    // =========================================================

    private void BindCorruptedCardToAnimationTemplate(
        CardTransformResult result
    )
    {
        if (animatedCorruptedCardTemplate == null)
        {
            Debug.LogWarning(
                "[GodCardCorruptionAnimation] " +
                "animatedCorruptedCardTemplate 沒有指定"
            );

            return;
        }


        if (result == null ||
            !result.success ||
            result.resultCardData == null)
        {
            Debug.LogWarning(
                "[GodCardCorruptionAnimation] " +
                "沒有成功取得污染後的牌資料"
            );

            return;
        }


        CardInstance displayInstance =
            new CardInstance(
                result.resultCardData
            );


        animatedCorruptedCardTemplate.Bind(
            displayInstance
        );


        if (animatedCardCanvasGroup == null)
        {
            animatedCardCanvasGroup =
                animatedCorruptedCardTemplate
                    .GetComponent<CanvasGroup>();
        }


        if (animatedCardCanvasGroup != null)
        {
            // 一開始保持透明
            // 可以由 Animation 控制 Alpha 顯示
            animatedCardCanvasGroup.alpha = 0f;

            animatedCardCanvasGroup.blocksRaycasts =
                false;

            animatedCardCanvasGroup.interactable =
                false;
        }


        animatedCorruptedCardTemplate
            .gameObject
            .SetActive(true);


        Debug.Log(
            $"[GodCardCorruptionAnimation] " +
            $"動畫模板綁定污染牌：" +
            $"{result.resultCardData.cardName}"
        );
    }


    // =========================================================
    // 決定使用哪個 GodCardAnimationData
    // =========================================================

    private GodCardAnimationData ResolveAnimationData(
        GodCardAnimationData cardAnimationData
    )
    {
        if (cardAnimationData != null)
        {
            Debug.Log(
                $"[GodCardAnimation] " +
                $"使用神牌專屬動畫：" +
                $"{cardAnimationData.animationName}"
            );

            return cardAnimationData;
        }


        if (defaultAnimationData != null)
        {
            Debug.Log(
                $"[GodCardAnimation] " +
                $"使用預設神牌動畫：" +
                $"{defaultAnimationData.animationName}"
            );

            return defaultAnimationData;
        }


        Debug.LogWarning(
            "[GodCardAnimation] " +
            "沒有設定專屬動畫，也沒有 Default Animation Data"
        );

        return null;
    }


    // =========================================================
    // 播放神牌動畫
    // =========================================================

    private IEnumerator PlayGodAnimationRoutine(
        GodCardAnimationData animationData
    )
    {
        animationFinished = false;


        if (animationData == null)
        {
            Debug.LogWarning(
                "[GodCardAnimation] animationData 是 null"
            );

            animationFinished = true;

            yield break;
        }


        if (animationData.animationPrefab == null)
        {
            Debug.LogWarning(
                $"[GodCardAnimation] " +
                $"{animationData.animationName} " +
                $"沒有設定 Animation Prefab"
            );

            animationFinished = true;

            yield break;
        }


        bool spawnSuccess =
            SpawnAnimationPrefab(
                animationData
            );


        if (!spawnSuccess)
        {
            animationFinished = true;

            yield break;
        }


        // =====================================================
        // 等一幀
        //
        // 讓 Instantiate 出來的 Prefab
        // 和 Animator 完成初始化
        // =====================================================

        yield return null;


        if (currentAnimationAnimator == null)
        {
            Debug.LogWarning(
                "[GodCardAnimation] " +
                "生成的動畫 Prefab 找不到 Animator"
            );

            animationFinished = true;

            yield break;
        }


        string trigger =
            animationData.triggerName;


        if (string.IsNullOrEmpty(trigger))
        {
            Debug.LogWarning(
                $"[GodCardAnimation] " +
                $"{animationData.animationName} " +
                $"沒有設定 Trigger Name"
            );

            animationFinished = true;

            yield break;
        }


        Debug.Log(
            $"[GodCardAnimation] " +
            $"播放動畫：{animationData.animationName}，" +
            $"Trigger = {trigger}"
        );


        currentAnimationAnimator.ResetTrigger(
            trigger
        );


        currentAnimationAnimator.SetTrigger(
            trigger
        );
    }


    // =========================================================
    // 生成神牌動畫 Prefab
    // =========================================================

    private bool SpawnAnimationPrefab(
        GodCardAnimationData animationData
    )
    {
        if (animationData == null)
        {
            Debug.LogWarning(
                "[GodCardAnimation] " +
                "SpawnAnimationPrefab animationData 是 null"
            );

            return false;
        }


        if (animationData.animationPrefab == null)
        {
            Debug.LogWarning(
                "[GodCardAnimation] " +
                "SpawnAnimationPrefab animationPrefab 是 null"
            );

            return false;
        }


        // =====================================================
        // 防止上一次動畫 Prefab 殘留
        // =====================================================

        DestroyCurrentAnimationPrefab();


        // =====================================================
        // 動畫永遠生成在 Animation Root
        // =====================================================

        Transform parent =
            AnimationRoot;


        currentAnimationObject =
            Instantiate(
                animationData.animationPrefab,
                parent
            );


        if (currentAnimationObject == null)
        {
            Debug.LogWarning(
                "[GodCardAnimation] " +
                "Instantiate Animation Prefab 失敗"
            );

            return false;
        }


        // =====================================================
        // 設定 Prefab Transform
        // =====================================================

        RectTransform rect =
            currentAnimationObject
                .GetComponent<RectTransform>();


        if (rect != null)
        {
            if (stretchAnimationPrefabToRoot)
            {
                rect.anchorMin =
                    Vector2.zero;

                rect.anchorMax =
                    Vector2.one;

                rect.offsetMin =
                    Vector2.zero;

                rect.offsetMax =
                    Vector2.zero;
            }


            rect.localPosition =
                Vector3.zero;

            rect.localRotation =
                Quaternion.identity;

            rect.localScale =
                Vector3.one;
        }
        else
        {
            Transform animationTransform =
                currentAnimationObject.transform;


            animationTransform.localPosition =
                Vector3.zero;

            animationTransform.localRotation =
                Quaternion.identity;

            animationTransform.localScale =
                Vector3.one;
        }


        // =====================================================
        // 找 Animator
        //
        // 先找 Root
        // Root 沒有再往 Children 找
        // =====================================================

        currentAnimationAnimator =
            currentAnimationObject
                .GetComponent<Animator>();


        if (currentAnimationAnimator == null)
        {
            currentAnimationAnimator =
                currentAnimationObject
                    .GetComponentInChildren<Animator>(
                        true
                    );
        }


        if (currentAnimationAnimator == null)
        {
            Debug.LogWarning(
                $"[GodCardAnimation] " +
                $"Prefab {animationData.animationPrefab.name} " +
                $"裡找不到 Animator"
            );


            DestroyCurrentAnimationPrefab();

            return false;
        }


        // =====================================================
        // 找 Animation Event Relay
        // =====================================================

        GodCardAnimationEventRelay relay =
            currentAnimationAnimator
                .GetComponent<GodCardAnimationEventRelay>();


        if (relay == null)
        {
            relay =
                currentAnimationObject
                    .GetComponentInChildren<GodCardAnimationEventRelay>(
                        true
                    );
        }


        if (relay != null)
        {
            relay.Initialize(this);
        }
        else
        {
            Debug.LogWarning(
                $"[GodCardAnimation] " +
                $"Prefab {animationData.animationPrefab.name} " +
                $"沒有 GodCardAnimationEventRelay。" +
                $"如果動畫沒有其他方式呼叫結束事件，" +
                $"流程會等到 Timeout。"
            );
        }


        Debug.Log(
            $"[GodCardAnimation] " +
            $"生成動畫 Prefab：" +
            $"{animationData.animationPrefab.name} " +
            $"→ Parent = {parent.name}"
        );


        return true;
    }


    // =========================================================
    // 刪除目前生成的動畫 Prefab
    // =========================================================

    private void DestroyCurrentAnimationPrefab()
    {
        if (currentAnimationObject != null)
        {
            Debug.Log(
                $"[GodCardAnimation] " +
                $"刪除動畫 Prefab：" +
                $"{currentAnimationObject.name}"
            );


            Destroy(
                currentAnimationObject
            );
        }


        currentAnimationObject = null;

        currentAnimationAnimator = null;
    }


    // =========================================================
    // 重置污染卡牌模板
    // =========================================================

    private void ResetAnimatedCardTemplate()
    {
        if (animatedCardCanvasGroup != null)
        {
            animatedCardCanvasGroup.alpha =
                0f;

            animatedCardCanvasGroup.blocksRaycasts =
                false;

            animatedCardCanvasGroup.interactable =
                false;
        }
    }


    // =========================================================
    // 等動畫結束
    // =========================================================

    private IEnumerator WaitForAnimationFinished(
        GodCardAnimationData animationData
    )
    {
        float timeout =
            animationTimeout;


        if (animationData != null &&
            animationData.animationTimeout > 0f)
        {
            timeout =
                animationData.animationTimeout;
        }


        float timer = 0f;


        while (!animationFinished)
        {
            timer += Time.deltaTime;


            if (timer >= timeout)
            {
                Debug.LogWarning(
                    "[GodCardAnimation] " +
                    "等待神牌動畫結束逾時，強制繼續流程"
                );

                break;
            }


            yield return null;
        }
    }


    // =========================================================
    // Animation Event
    // =========================================================

    public void AnimEvent_GodCorruptionFinished()
    {
        Debug.Log(
            "[GodCardCorruptionAnimation] " +
            "收到動畫結束事件"
        );


        animationFinished = true;
    }


    // =========================================================
    // Blackout
    // =========================================================

    private IEnumerator FadeBlackout(
        bool show
    )
    {
        if (blackoutCanvasGroup == null)
            yield break;


        blackoutCanvasGroup.blocksRaycasts =
            show;

        blackoutCanvasGroup.interactable =
            show;


        float targetAlpha =
            blackoutAlpha;


        if (currentAnimationData != null)
        {
            targetAlpha =
                currentAnimationData.blackoutAlpha;
        }


        float start =
            blackoutCanvasGroup.alpha;


        float end =
            show
                ? targetAlpha
                : 0f;


        // 沒有淡入時間時直接設定
        if (blackoutFadeDuration <= 0f)
        {
            blackoutCanvasGroup.alpha =
                end;


            if (!show)
            {
                blackoutCanvasGroup.blocksRaycasts =
                    false;

                blackoutCanvasGroup.interactable =
                    false;
            }


            yield break;
        }


        float timer = 0f;


        while (timer < blackoutFadeDuration)
        {
            timer += Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer /
                    blackoutFadeDuration
                );


            float smoothT =
                t * t * (3f - 2f * t);


            blackoutCanvasGroup.alpha =
                Mathf.Lerp(
                    start,
                    end,
                    smoothT
                );


            yield return null;
        }


        blackoutCanvasGroup.alpha =
            end;


        if (!show)
        {
            blackoutCanvasGroup.blocksRaycasts =
                false;

            blackoutCanvasGroup.interactable =
                false;
        }
    }


    // =========================================================
    // 原本打出的神牌消失
    // =========================================================

    private IEnumerator FinishPlayedGodCard(
        CardViewUI playedCardView
    )
    {
        if (playedCardView == null)
            yield break;


        RectTransform rect =
            playedCardView
                .GetComponent<RectTransform>();


        if (rect == null)
        {
            Destroy(
                playedCardView.gameObject
            );

            yield break;
        }


        CanvasGroup canvasGroup =
            playedCardView
                .GetComponent<CanvasGroup>();


        if (canvasGroup == null)
        {
            canvasGroup =
                playedCardView
                    .gameObject
                    .AddComponent<CanvasGroup>();
        }


        float startAlpha =
            canvasGroup.alpha;


        Vector3 startScale =
            rect.localScale;


        if (godCardFadeDuration <= 0f)
        {
            canvasGroup.alpha = 0f;

            rect.localScale =
                Vector3.one * 0.2f;


            Destroy(
                playedCardView.gameObject
            );


            yield break;
        }


        float timer = 0f;


        while (timer < godCardFadeDuration)
        {
            timer += Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer /
                    godCardFadeDuration
                );


            canvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    0f,
                    t
                );


            rect.localScale =
                Vector3.Lerp(
                    startScale,
                    Vector3.one * 0.2f,
                    t
                );


            yield return null;
        }


        canvasGroup.alpha = 0f;


        Destroy(
            playedCardView.gameObject
        );
    }


    // =========================================================
    // 神牌震動
    // =========================================================

    private IEnumerator ShakeRect(
        RectTransform rect,
        float duration,
        float strength
    )
    {
        if (rect == null)
            yield break;


        if (duration <= 0f)
            yield break;


        Vector3 originalPosition =
            rect.position;


        float timer = 0f;


        while (timer < duration)
        {
            timer += Time.deltaTime;


            Vector3 offset =
                new Vector3(
                    Random.Range(
                        -strength,
                        strength
                    ),
                    Random.Range(
                        -strength,
                        strength
                    ),
                    0f
                );


            rect.position =
                originalPosition +
                offset;


            yield return null;
        }


        rect.position =
            originalPosition;
    }


    // =========================================================
    // 神牌移動
    // =========================================================

    private IEnumerator MoveRectWorld(
        RectTransform rect,
        Vector3 targetWorldPosition,
        float duration
    )
    {
        if (rect == null)
            yield break;


        Vector3 startPosition =
            rect.position;


        if (duration <= 0f)
        {
            rect.position =
                targetWorldPosition;

            yield break;
        }


        float timer = 0f;


        while (timer < duration)
        {
            timer += Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer /
                    duration
                );


            float smoothT =
                t * t * (3f - 2f * t);


            rect.position =
                Vector3.Lerp(
                    startPosition,
                    targetWorldPosition,
                    smoothT
                );


            yield return null;
        }


        rect.position =
            targetWorldPosition;
    }


    // =========================================================
    // Controller 被 Destroy 時
    // 清掉還存在的動畫 Prefab
    // =========================================================

    private void OnDestroy()
    {
        if (currentAnimationObject != null)
        {
            Destroy(
                currentAnimationObject
            );


            currentAnimationObject = null;

            currentAnimationAnimator = null;
        }
    }
    private void HideUIForGodAnimation()
    {
        if (hideDuringGodAnimationUI == null)
            return;

        hideDuringGodAnimationUI.SetActive(false);

        Debug.Log(
            $"[GodCardAnimation] 隱藏 UI：{hideDuringGodAnimationUI.name}"
        );
    }

    private void ShowUIAfterGodAnimation()
    {
        if (hideDuringGodAnimationUI == null)
            return;

        hideDuringGodAnimationUI.SetActive(true);

        Debug.Log(
            $"[GodCardAnimation] 顯示 UI：{hideDuringGodAnimationUI.name}"
        );
    }
}