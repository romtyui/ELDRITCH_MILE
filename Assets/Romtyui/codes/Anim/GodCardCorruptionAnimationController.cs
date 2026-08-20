using System.Collections;
using UnityEngine;

public class GodCardCorruptionAnimationController : MonoBehaviour
{
    // =========================================================
    // Roots
    // =========================================================

    [Header("Roots")]

    [Tooltip(
        "神牌動畫 Prefab 生成時使用的父物件。" +
        "可以放在專門的神牌動畫 Canvas。"
    )]
    public RectTransform animationRoot;

    [Tooltip(
        "打出的神牌 CardViewUI 使用的父物件。" +
        "建議跟卡牌 UI 使用相同 Canvas。"
    )]
    public RectTransform cardRoot;


    // =========================================================
    // Played God Card
    // =========================================================

    [Header("Played God Card")]

    [Tooltip(
        "打出的神牌移動到的位置。" +
        "建議和 Card Root 使用相同 Canvas。"
    )]
    public RectTransform centerPoint;

    public float moveToCenterDuration = 0.35f;

    public float shakeDuration = 0.45f;

    public float shakeStrength = 12f;


    // =========================================================
    // Default God Animation
    // =========================================================

    [Header("Default God Animation")]

    public GodCardAnimationData defaultAnimationData;


    // =========================================================
    // Blackout
    // =========================================================

    [Header("Blackout")]

    public CanvasGroup blackoutCanvasGroup;

    [Range(0f, 1f)]
    public float blackoutAlpha = 0.75f;

    public float blackoutFadeDuration = 0.25f;


    // =========================================================
    // UI Hide During God Animation
    // =========================================================

    [Header("UI Hide During God Animation")]

    [Tooltip(
        "神牌動畫開始時隱藏，" +
        "整個神牌動畫結束後重新顯示的 UI。"
    )]
    public GameObject hideDuringGodAnimationUI;


    // =========================================================
    // Animation Prefab
    // =========================================================

    [Header("God Animation Prefab")]

    [Tooltip(
        "如果開啟，生成的動畫 Prefab 如果有 RectTransform，" +
        "會填滿 Animation Root。"
    )]
    public bool stretchAnimationPrefabToRoot = false;


    // =========================================================
    // Animated Corrupted Card Template
    // =========================================================

    [Header("Animated Corrupted Card Template")]

    [Tooltip(
        "動畫中顯示變換後卡牌的 CardViewUI 模板。"
    )]
    public CardViewUI animatedCorruptedCardTemplate;

    [Tooltip(
        "控制變換後卡牌模板顯示 / 隱藏。"
    )]
    public CanvasGroup animatedCardCanvasGroup;


    // =========================================================
    // Fallback Wait
    // =========================================================

    [Header("Fallback Wait")]

    [Tooltip("Animator 動畫總長度之外，額外增加多少秒作為 Timeout 緩衝。")]
    public float animationTimeoutBuffer = 2f;

    [Tooltip("如果無法取得 Animator 動畫總長度，使用這個固定 Timeout。")]
    public float fallbackAnimationTimeout = 10f;


    // =========================================================
    // End
    // =========================================================

    [Header("End")]

    public float godCardFadeDuration = 0.2f;


    // =========================================================
    // Runtime Animation
    // =========================================================

    [Header("Runtime Animation")]

    [SerializeField]
    private GodCardAnimationData currentAnimationData;

    [SerializeField]
    private GameObject currentAnimationObject;

    [SerializeField]
    private Animator currentAnimationAnimator;

    [SerializeField]
    private GodCardAnimationSignalEmitter currentSignalEmitter;

    [SerializeField]
    private bool animationFinished;


    // =========================================================
    // Runtime Transform
    // =========================================================

    [Header("Runtime Transform")]

    [SerializeField]
    private bool transformTriggered;

    private TransformRandomCardByPoolEffectData
        pendingTransformEffect;

    private CardResolveContext
        pendingTransformContext;

    private CardTransformResult
        currentTransformResult;


    // =========================================================
    // Root Properties
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
    // Main Sequence
    // =========================================================

    public IEnumerator PlayGodCorruptionSequence(
        CardViewUI playedCardView,
        TransformRandomCardByPoolEffectData transformEffect,
        CardResolveContext context,
        GodCardAnimationData animationData
    )
    {
        // =====================================================
        // Validation
        // =====================================================

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
        // Root Debug
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
        // Runtime 初始化
        // =====================================================

        currentAnimationData =
            ResolveAnimationData(
                animationData
            );


        animationFinished =
            false;


        transformTriggered =
            false;


        currentTransformResult =
            null;


        // =====================================================
        // 保存這次真正要執行的 Transform
        //
        // 注意：
        // 這裡完全沒有 ExecuteTransform。
        // =====================================================

        pendingTransformEffect =
            transformEffect;


        pendingTransformContext =
            context;


        RectTransform playedCardRect =
            playedCardView
                .GetComponent<RectTransform>();


        if (playedCardRect == null)
        {
            ClearPendingTransform();

            yield break;
        }


        // =====================================================
        // 1. 隱藏指定 UI
        // =====================================================

        //HideUIForGodAnimation();


        // =====================================================
        // 1. 開啟黑幕
        // =====================================================

        yield return FadeBlackout(
            true
        );


        // =====================================================
        // 3. 神牌移到 Card Root
        // =====================================================

        playedCardRect.SetParent(
            CardRoot,
            true
        );


        playedCardRect.SetAsLastSibling();


        // =====================================================
        // 4. 神牌飛到中央
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
        // 5. 神牌震動
        // =====================================================

        yield return ShakeRect(
            playedCardRect,
            shakeDuration,
            shakeStrength
        );


        // =====================================================
        // ★ 以前這裡會 ExecuteTransform()
        //
        // 現在不會。
        //
        // 要等動畫 Signal Track 到指定 Keyframe
        // 才真正變換。
        // =====================================================


        // =====================================================
        // 6. 生成並播放動畫 Prefab
        // =====================================================

        yield return PlayGodAnimationRoutine(
            currentAnimationData
        );


        // =====================================================
        // 7. 等動畫結束
        // =====================================================

        yield return WaitForAnimationFinished(
            currentAnimationData
        );


        // =====================================================
        // 8. Transform 保底
        //
        // 如果 Signal Track 沒有成功發送，
        // 還是一定要完成遊戲邏輯。
        // =====================================================

        if (!transformTriggered)
        {
            Debug.LogWarning(
                "[GodCardAnimation] " +
                "動畫結束前沒有收到 Transform Moment，" +
                "執行保底 Transform。"
            );


            TriggerTransformMoment();
        }


        // =====================================================
        // 9. 原本打出去的神牌消失
        // =====================================================

        yield return FinishPlayedGodCard(
            playedCardView
        );


        // =====================================================
        // 10. 重置變換後卡牌 Template
        // =====================================================

        ResetAnimatedCardTemplate();


        // =====================================================
        // 11. 刪除這次的動畫 Prefab
        // =====================================================

        DestroyCurrentAnimationPrefab();


        // =====================================================
        // 12. 關閉黑幕
        // =====================================================

        yield return FadeBlackout(
            false
        );


        // =====================================================
        // 13. 顯示原本隱藏 UI
        // =====================================================

        ShowUIAfterGodAnimation();


        // =====================================================
        // 14. 清除 Pending Transform
        // =====================================================

        ClearPendingTransform();


        // =====================================================
        // 15. Runtime 清除
        // =====================================================

        currentAnimationData =
            null;


        animationFinished =
            false;


        transformTriggered =
            false;


        currentTransformResult =
            null;
    }


    // =========================================================
    // Transform Moment
    //
    // 真正修改牌組是在這裡。
    // =========================================================

    public void TriggerTransformMoment()
    {
        // =====================================================
        // 防止同一張神牌重複 Transform
        // =====================================================

        if (transformTriggered)
        {
            Debug.Log(
                "[GodCardAnimation] " +
                "Transform Moment 已執行過，忽略重複訊號。"
            );

            return;
        }


        // =====================================================
        // Validation
        // =====================================================

        if (pendingTransformEffect == null)
        {
            Debug.LogWarning(
                "[GodCardAnimation] " +
                "Transform Moment 發生，" +
                "但 pendingTransformEffect 是 null"
            );

            return;
        }


        if (pendingTransformContext == null)
        {
            Debug.LogWarning(
                "[GodCardAnimation] " +
                "Transform Moment 發生，" +
                "但 pendingTransformContext 是 null"
            );

            return;
        }


        // 所有檢查通過後才算真的觸發
        transformTriggered =
            true;


        Debug.Log(
            "[GodCardAnimation] " +
            "★ 到達動畫 Transform Keyframe，現在正式變換卡牌 ★"
        );


        // =====================================================
        // 1. 現在才真正修改抽牌堆
        // =====================================================

        currentTransformResult =
            pendingTransformEffect
                .ExecuteTransform(
                    pendingTransformContext
                );


        // =====================================================
        // 2. 綁定變換後卡牌資料
        // =====================================================

        BindCorruptedCardToAnimationTemplate(
            currentTransformResult
        );
    }


    // =========================================================
    // Signal Callback
    // =========================================================

    private void OnTransformMomentSignal()
    {
        TriggerTransformMoment();
    }

    private void OnAnimationFinishedSignal()
    {
        if (animationFinished)
            return;

        animationFinished = true;

        Debug.Log("[GodCardAnimation] ★ 收到 Animation Finished Signal，整段神牌動畫正式完成 ★");
    }
    // =========================================================
    // Bind Corrupted Card
    // =========================================================

    private void BindCorruptedCardToAnimationTemplate(
        CardTransformResult result
    )
    {
        if (animatedCorruptedCardTemplate == null)
        {
            Debug.LogWarning(
                "[GodCardAnimation] " +
                "animatedCorruptedCardTemplate 沒有指定"
            );

            return;
        }


        if (result == null ||
            !result.success ||
            result.resultCardData == null)
        {
            Debug.LogWarning(
                "[GodCardAnimation] " +
                "沒有成功取得變換後卡牌資料"
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
            /*
             * 保持你原本的功能：
             *
             * Bind 完先 Alpha = 0。
             *
             * 你的 Animation Clip 本身
             * 可以繼續控制這張卡什麼時候顯示。
             */
            animatedCardCanvasGroup.alpha =
                0f;


            animatedCardCanvasGroup.blocksRaycasts =
                false;


            animatedCardCanvasGroup.interactable =
                false;
        }


        animatedCorruptedCardTemplate
            .gameObject
            .SetActive(
                true
            );


        Debug.Log(
            $"[GodCardAnimation] " +
            $"變換後卡牌 Template 綁定：" +
            $"{result.resultCardData.cardName}"
        );
    }


    // =========================================================
    // Resolve Animation Data
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
            "沒有設定任何 GodCardAnimationData"
        );


        return null;
    }


    // =========================================================
    // Play Animation
    // =========================================================

    private IEnumerator PlayGodAnimationRoutine(
    GodCardAnimationData animationData
)
    {
        animationFinished =
            false;


        if (animationData == null)
        {
            Debug.LogWarning(
                "[GodCardAnimation] animationData 是 null"
            );


            animationFinished =
                true;


            yield break;
        }


        if (animationData.animationPrefab == null)
        {
            Debug.LogWarning(
                $"[GodCardAnimation] " +
                $"{animationData.animationName} " +
                $"沒有設定 Animation Prefab"
            );


            animationFinished =
                true;


            yield break;
        }


        bool spawnSuccess =
            SpawnAnimationPrefab(
                animationData
            );


        if (!spawnSuccess)
        {
            animationFinished =
                true;


            yield break;
        }


        // =====================================================
        // 等 Animator 初始化
        // =====================================================

        yield return null;


        if (currentAnimationAnimator == null)
        {
            Debug.LogWarning(
                "[GodCardAnimation] " +
                "動畫 Prefab 找不到 Animator"
            );


            animationFinished =
                true;


            yield break;
        }


        string trigger =
            animationData.triggerName;


        if (string.IsNullOrEmpty(trigger))
        {
            Debug.LogWarning(
                $"[GodCardAnimation] " +
                $"{animationData.animationName} " +
                $"Trigger Name 是空的"
            );


            animationFinished =
                true;


            yield break;
        }


        // =====================================================
        // ★ 真正神牌 Prefab 動畫即將開始
        //
        // 到這一刻才隱藏指定 UI。
        //
        // 前面的：
        // Blackout
        // Card Move
        // Shake
        //
        // 都不會提前把 UI 隱藏。
        // =====================================================

        HideUIForGodAnimation();


        Debug.Log(
            $"[GodCardAnimation] " +
            $"開始播放：{animationData.animationName}，" +
            $"Trigger = {trigger}"
        );


        // =====================================================
        // 播放動畫
        // =====================================================

        currentAnimationAnimator
            .ResetTrigger(
                trigger
            );


        currentAnimationAnimator
            .SetTrigger(
                trigger
            );
    }


    // =========================================================
    // Spawn Animation Prefab
    // =========================================================

    private bool SpawnAnimationPrefab(GodCardAnimationData animationData)
    {
        if (animationData == null)
            return false;


        if (animationData.animationPrefab == null)
            return false;


        // =====================================================
        // 防止上一個 Prefab 殘留
        // =====================================================

        DestroyCurrentAnimationPrefab();


        Transform parent =
            AnimationRoot;


        // =====================================================
        // Instantiate
        // =====================================================

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
        // 即使 Prefab Asset 原本 inactive
        // Runtime 也強制開啟。
        // =====================================================

        currentAnimationObject
            .SetActive(
                true
            );


        // =====================================================
        // Transform
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
            Transform spawnedTransform =
                currentAnimationObject
                    .transform;


            spawnedTransform.localPosition =
                Vector3.zero;


            spawnedTransform.localRotation =
                Quaternion.identity;


            spawnedTransform.localScale =
                Vector3.one;
        }


        // =====================================================
        // Animator
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
                $"找不到 Animator"
            );


            DestroyCurrentAnimationPrefab();


            return false;
        }


        // =====================================================
        // Signal Emitter
        // =====================================================

        currentSignalEmitter =
            currentAnimationObject
                .GetComponent<GodCardAnimationSignalEmitter>();


        if (currentSignalEmitter == null)
        {
            currentSignalEmitter =
                currentAnimationObject
                    .GetComponentInChildren<
                        GodCardAnimationSignalEmitter
                    >(
                        true
                    );
        }


        if (currentSignalEmitter == null)
        {
            Debug.LogWarning(
                $"[GodCardAnimation] " +
                $"Prefab {animationData.animationPrefab.name} " +
                $"找不到 GodCardAnimationSignalEmitter。" +
                $"如果 Signal 沒有發出，最後會使用保底 Transform。"
            );
        }
        else
        {
            // 防止重複訂閱
            currentSignalEmitter.TransformMoment -= OnTransformMomentSignal;
            currentSignalEmitter.AnimationFinished -= OnAnimationFinishedSignal;
           

            // 訂閱這一次動畫 Prefab
            currentSignalEmitter.TransformMoment += OnTransformMomentSignal;
            currentSignalEmitter.AnimationFinished += OnAnimationFinishedSignal;
        }


        Debug.Log(
            $"[GodCardAnimation] " +
            $"動畫 Prefab 已生成：" +
            $"{currentAnimationObject.name}，" +
            $"Parent = {parent.name}，" +
            $"Active = {currentAnimationObject.activeInHierarchy}"
        );


        return true;
    }


    // =========================================================
    // Destroy Animation Prefab
    // =========================================================

    private void DestroyCurrentAnimationPrefab()
    {
        // =====================================================
        // 先取消訂閱
        // =====================================================

        if (currentSignalEmitter != null)
        {
            currentSignalEmitter.TransformMoment -=  OnTransformMomentSignal;
            currentSignalEmitter.AnimationFinished -= OnAnimationFinishedSignal;
        }


        currentSignalEmitter =
            null;


        // =====================================================
        // Destroy
        // =====================================================

        if (currentAnimationObject != null)
        {
            Debug.Log(
                $"[GodCardAnimation] " +
                $"Destroy 動畫 Prefab：" +
                $"{currentAnimationObject.name}"
            );


            Destroy(
                currentAnimationObject
            );
        }


        currentAnimationObject =
            null;


        currentAnimationAnimator =
            null;
    }


    // =========================================================
    // Wait For Animation Finished
    //
    // 這裡不依賴 Animation Event。
    //
    // 直接讀 Animator State。
    // =========================================================

    private IEnumerator WaitForAnimationFinished(GodCardAnimationData animationData)
    {
        float timeout = CalculateAnimationTimeout(animationData);
        float timer = 0f;

        while (!animationFinished)
        {
            timer += Time.deltaTime;

            if (timer >= timeout)
            {
                Debug.LogWarning($"[GodCardAnimation] 等待 Animation Finished Signal 逾時。Timeout = {timeout:F2} 秒，使用保底流程。");
                animationFinished = true;
                yield break;
            }

            yield return null;
        }

        Debug.Log("[GodCardAnimation] Animation Finished Signal 已收到，繼續神牌收尾。");
    }


    // =========================================================
    // Reset Corrupted Card Template
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
    // Hide UI
    // =========================================================

    private void HideUIForGodAnimation()
    {
        if (hideDuringGodAnimationUI == null)
            return;


        hideDuringGodAnimationUI
            .SetActive(
                false
            );


        Debug.Log(
            $"[GodCardAnimation] " +
            $"神牌動畫開始，隱藏 UI：" +
            $"{hideDuringGodAnimationUI.name}"
        );
    }


    // =========================================================
    // Show UI
    // =========================================================

    private void ShowUIAfterGodAnimation()
    {
        if (hideDuringGodAnimationUI == null)
            return;


        hideDuringGodAnimationUI
            .SetActive(
                true
            );


        Debug.Log(
            $"[GodCardAnimation] " +
            $"神牌動畫結束，顯示 UI：" +
            $"{hideDuringGodAnimationUI.name}"
        );
    }


    // =========================================================
    // Clear Pending Transform
    // =========================================================

    private void ClearPendingTransform()
    {
        pendingTransformEffect =
            null;


        pendingTransformContext =
            null;
    }


    // =========================================================
    // Fade Blackout
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


        float timer =
            0f;


        while (timer <
               blackoutFadeDuration)
        {
            timer +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer /
                    blackoutFadeDuration
                );


            float smoothT =
                t * t *
                (3f - 2f * t);


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
    // Finish Played God Card
    // =========================================================

    private IEnumerator FinishPlayedGodCard(CardViewUI playedCardView)
    {
        Debug.Log("[GodCardAnimation] ★ FinishPlayedGodCard 開始，原本神牌現在開始縮小 ★");


        if (playedCardView == null)
            yield break;

        RectTransform playedCardRect = playedCardView.GetComponent<RectTransform>();

        if (playedCardRect == null)
        {
            Destroy(playedCardView.gameObject);
            yield break;
        }

        CanvasGroup playedCardCanvasGroup = playedCardView.GetComponent<CanvasGroup>();

        if (playedCardCanvasGroup == null)
            playedCardCanvasGroup = playedCardView.gameObject.AddComponent<CanvasGroup>();

        Transform animationTransform = null;
        CanvasGroup animationCanvasGroup = null;

        if (currentAnimationObject != null)
        {
            animationTransform = currentAnimationObject.transform;
            animationCanvasGroup = currentAnimationObject.GetComponent<CanvasGroup>();

            if (animationCanvasGroup == null)
                animationCanvasGroup = currentAnimationObject.AddComponent<CanvasGroup>();
        }

        float playedCardStartAlpha = playedCardCanvasGroup.alpha;
        Vector3 playedCardStartScale = playedCardRect.localScale;

        float animationStartAlpha = 1f;
        Vector3 animationStartScale = Vector3.one;

        if (animationTransform != null)
            animationStartScale = animationTransform.localScale;

        if (animationCanvasGroup != null)
            animationStartAlpha = animationCanvasGroup.alpha;

        if (godCardFadeDuration <= 0f)
        {
            Destroy(playedCardView.gameObject);
            yield break;
        }

        float timer = 0f;

        while (timer < godCardFadeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / godCardFadeDuration);

            playedCardCanvasGroup.alpha = Mathf.Lerp(playedCardStartAlpha, 0f, t);
            playedCardRect.localScale = Vector3.Lerp(playedCardStartScale, Vector3.one * 0.2f, t);

            if (animationTransform != null)
                animationTransform.localScale = Vector3.Lerp(animationStartScale, animationStartScale * 0.2f, t);

            if (animationCanvasGroup != null)
                animationCanvasGroup.alpha = Mathf.Lerp(animationStartAlpha, 0f, t);

            yield return null;
        }

        playedCardCanvasGroup.alpha = 0f;
        playedCardRect.localScale = Vector3.one * 0.2f;

        if (animationTransform != null)
            animationTransform.localScale = animationStartScale * 0.2f;

        if (animationCanvasGroup != null)
            animationCanvasGroup.alpha = 0f;

        Destroy(playedCardView.gameObject);
    }


    // =========================================================
    // Shake
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


        float timer =
            0f;


        while (timer < duration)
        {
            timer +=
                Time.deltaTime;


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
    // Move Card
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


        float timer =
            0f;


        while (timer < duration)
        {
            timer +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer /
                    duration
                );


            float smoothT =
                t * t *
                (3f - 2f * t);


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
    // Disable
    // =========================================================

    private void OnDisable()
    {
        ShowUIAfterGodAnimation();
    }


    // =========================================================
    // Destroy
    // =========================================================

    private void OnDestroy()
    {
        DestroyCurrentAnimationPrefab();

        ShowUIAfterGodAnimation();

        ClearPendingTransform();
    }

    private float CalculateAnimationTimeout(GodCardAnimationData animationData)
    {
        if (currentAnimationAnimator == null || currentAnimationAnimator.runtimeAnimatorController == null)
            return fallbackAnimationTimeout;

        AnimationClip[] clips = currentAnimationAnimator.runtimeAnimatorController.animationClips;

        if (clips == null || clips.Length == 0)
            return fallbackAnimationTimeout;

        float totalDuration = 0f;

        foreach (AnimationClip clip in clips)
        {
            if (clip == null)
                continue;

            totalDuration += clip.length / 0.5f; ;
        }

        if (totalDuration <= 0f)
            return fallbackAnimationTimeout;

        float timeout = totalDuration + animationTimeoutBuffer;

        Debug.Log($"[GodCardAnimation] 動畫總長度 = {totalDuration:F2} 秒，Buffer = {animationTimeoutBuffer:F2} 秒，Timeout = {timeout:F2} 秒");

        return timeout;
    }
}