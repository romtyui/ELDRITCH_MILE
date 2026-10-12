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

    [Min(0f)]
    [Tooltip("神牌到達定點後，震動持續的秒數。")]
    public float shakeDuration = 0.45f;

    [Min(0f)]
    [Tooltip("震動位移上限，以 Game 畫面的像素計算；12 表示 X、Y 各最多偏移約 12 像素。")]
    public float shakeStrength = 12f;

    [Tooltip("卡片切換到 Card Root 後的大小倍率；(1,1) 保留切換前的縮放。")]
    public Vector2 displayScale = Vector2.one;
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
    // Animated Corrupted Card Template
    // =========================================================

    [Header("Animated Corrupted Card Template")]

    [Tooltip("動畫中顯示變換後卡牌的 CardViewUI 模板。" )]
    public CardViewUI animatedCorruptedCardTemplate;

    [Tooltip( "控制變換後卡牌模板顯示 / 隱藏。")]
    public CanvasGroup animatedCardCanvasGroup;
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


    public IEnumerator PlayGodCorruptionSequence(CardViewUI playedCardView, TransformRandomCardByPoolEffectData transformEffect, CardResolveContext context, GodCardAnimationData animationData)
    {
        // =====================================================
        // Validation
        // =====================================================

        if (playedCardView == null)
        {
            Debug.LogWarning("[GodCardAnimation] playedCardView 是 null");
            yield break;
        }

        if (transformEffect == null)
        {
            Debug.LogWarning("[GodCardAnimation] transformEffect 是 null");
            yield break;
        }

        if (context == null)
        {
            Debug.LogWarning("[GodCardAnimation] context 是 null");
            yield break;
        }

        // =====================================================
        // Root Debug
        // =====================================================

        if (animationRoot == null)
        {
            Debug.LogWarning("[GodCardAnimation] Animation Root 沒有指定，動畫 Prefab 會生成在 Controller 自己底下。");
        }

        if (cardRoot == null)
        {
            Debug.LogWarning("[GodCardAnimation] Card Root 沒有指定，打出的神牌會移到 Controller 自己底下。");
        }

        // =====================================================
        // Runtime 初始化
        // =====================================================

        currentAnimationData = ResolveAnimationData(animationData);
        animationFinished = false;
        transformTriggered = false;
        currentTransformResult = null;

        // =====================================================
        // 保存這次真正要執行的 Transform
        //
        // 注意：
        // 這裡完全沒有 ExecuteTransform。
        // =====================================================

        pendingTransformEffect = transformEffect;
        pendingTransformContext = context;

        RectTransform playedCardRect = playedCardView.GetComponent<RectTransform>();

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

        yield return FadeBlackout(true);

        // =====================================================
        // 3. 保持原本父物件，在 BattleUICanvas 飛向中央
        // =====================================================

        if (centerPoint != null)
        {
            Canvas sourceCanvas = playedCardRect.GetComponentInParent<Canvas>();
            Canvas destinationCanvas = centerPoint.GetComponentInParent<Canvas>();

            if (sourceCanvas != null && destinationCanvas != null)
            {
                sourceCanvas = sourceCanvas.rootCanvas;
                destinationCanvas = destinationCanvas.rootCanvas;

                Camera sourceCamera = sourceCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : sourceCanvas.worldCamera;
                Camera destinationCamera = destinationCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : destinationCanvas.worldCamera;
                RectTransform sourceCanvasRect = sourceCanvas.GetComponent<RectTransform>();
                Vector2 centerScreenPoint = RectTransformUtility.WorldToScreenPoint(destinationCamera, centerPoint.position);

                if (sourceCanvasRect != null && RectTransformUtility.ScreenPointToWorldPointInRectangle(sourceCanvasRect, centerScreenPoint, sourceCamera, out Vector3 sourceCenterWorldPosition))
                {
                    yield return MoveRectWorld(playedCardRect, sourceCenterWorldPosition, moveToCenterDuration);
                }
                else
                {
                    Debug.LogWarning("[GodCardAnimation] 無法將 Center Point 換算到原卡牌 Canvas，改用原本的世界座標移動。");
                    yield return MoveRectWorld(playedCardRect, centerPoint.position, moveToCenterDuration);
                }
            }
            else
            {
                Debug.LogWarning("[GodCardAnimation] 找不到卡牌或 Center Point 的 Canvas，改用原本的世界座標移動。");
                yield return MoveRectWorld(playedCardRect, centerPoint.position, moveToCenterDuration);
            }
        }
        else
        {
            Debug.LogWarning("[GodCardAnimation] Center Point 沒有指定");
        }

        // =====================================================
        // 4. 在原本 Canvas 完成神牌震動
        // =====================================================

        yield return ShakeRect(playedCardRect, shakeDuration, shakeStrength);

        // =====================================================
        // 5. 同一幀切到 Card Root，維持螢幕位置與大小
        // =====================================================

        RectTransform destinationRoot = CardRoot as RectTransform;

        if (destinationRoot != null)
        {
            ReparentCardPreservingScreenAppearance(playedCardRect, destinationRoot);
        }
        else
        {
            Debug.LogWarning("[GodCardAnimation] Card Root 不是 RectTransform，無法換算螢幕座標，使用原本的 SetParent。");
            playedCardRect.SetParent(CardRoot, true);
            playedCardRect.SetAsLastSibling();
        }

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

        yield return PlayGodAnimationRoutine(currentAnimationData);

        // =====================================================
        // 7. 等動畫結束
        // =====================================================

        yield return WaitForAnimationFinished(currentAnimationData);

        // =====================================================
        // 8. Transform 保底
        //
        // 如果 Signal Track 沒有成功發送，
        // 還是一定要完成遊戲邏輯。
        // =====================================================

        if (!transformTriggered)
        {
            Debug.LogWarning("[GodCardAnimation] 動畫結束前沒有收到 Transform Moment，執行保底 Transform。");
            TriggerTransformMoment();
        }

        // =====================================================
        // 9. 原本打出去的神牌消失
        // =====================================================

        yield return FinishPlayedGodCard(playedCardView);

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

        yield return FadeBlackout(false);

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

        currentAnimationData = null;
        animationFinished = false;
        transformTriggered = false;
        currentTransformResult = null;
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

        // 執行播放前選定的那次變換，不重新隨機選牌。
        bool applied = pendingTransformContext.battleManager.playerDeck.ApplyPreparedCardTransform(currentTransformResult);

        if (!applied)
        {
            Debug.LogWarning("[GodCardAnimation] 待執行的卡牌變換失敗");
            return;
        }


        // =====================================================
        // 2. 綁定變換後卡牌資料
        // =====================================================

        // 事件之後顯示變換後的牌。
        // 此處只更新牌面，不重設 Alpha、位置或縮放。
        if (animatedCorruptedCardTemplate != null)
        {
            animatedCorruptedCardTemplate.Bind(
                new CardInstance(currentTransformResult.resultCardData)
            );

            Debug.Log(
                $"[GodCardAnimation] 動畫牌面切換為：{currentTransformResult.resultCardData.cardName}",
                animatedCorruptedCardTemplate
            );
        }
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

    private void BindCorruptedCardToAnimationTemplate(CardTransformResult result)
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
            result.originalCardData == null)
        {
            Debug.LogWarning(
                "[GodCardAnimation] " +
                "沒有成功取得變換前卡牌資料"
            );

            return;
        }

        CardInstance displayInstance = new CardInstance(result.originalCardData);

        animatedCorruptedCardTemplate.Bind(displayInstance);

        if (animatedCardCanvasGroup == null)
        {
            animatedCardCanvasGroup =
                animatedCorruptedCardTemplate.GetComponent<CanvasGroup>();
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
            animatedCardCanvasGroup.alpha = 0f;

            animatedCardCanvasGroup.blocksRaycasts = false;

            animatedCardCanvasGroup.interactable = false;
        }

        animatedCorruptedCardTemplate.gameObject.SetActive(true);

        Debug.Log(
            $"[GodCardAnimation] 動畫起始牌面綁定：{result.originalCardData.cardName}",
            animatedCorruptedCardTemplate
        );
    }

    private IEnumerator DebugAnimatedCardVisual(CardViewUI template, CardInstance instance)
    {
        if (template == null || instance == null || instance.data == null)
            yield break;

        CardData data = instance.data;
        CardVisualData visual = data.visualData != null
            ? data.visualData
            : template.defaultVisualData;

        if (visual == null)
        {
            Debug.LogError(
                $"[動畫卡面檢查] {data.cardName} 沒有 Visual Data，模板也沒有 Default Visual Data。",
                template
            );

            yield break;
        }

        UnityEngine.UI.Image[] images =
        {
        template.artworkImage,
        template.cardFaceImage,
        template.cardFrameImage,
        template.maskImage
    };

        Sprite[] expectedSprites =
        {
        visual.artworkSprite,
        visual.cardFaceSprite,
        visual.cardFrameSprite,
        visual.maskSprite
    };

        string[] labels = { "Artwork", "Face", "Frame", "Mask" };

        // 比較 Bind 當下與後續兩個影格，檢查圖片是否被動畫覆寫。
        for (int frame = 0; frame < 3; frame++)
        {
            if (template == null)
                yield break;

            for (int i = 0; i < images.Length; i++)
            {
                UnityEngine.UI.Image image = images[i];

                if (image == null)
                {
                    Debug.LogError(
                        $"[動畫卡面檢查] {labels[i]} 的 Image 參照是空的。",
                        template
                    );

                    continue;
                }

                string path = image.name;
                Transform parent = image.transform.parent;

                while (parent != null)
                {
                    path = parent.name + "/" + path;
                    parent = parent.parent;
                }

                Debug.Log(
                    $"[動畫卡面檢查] 階段={frame}，牌={data.cardName}，" +
                    $"欄位={labels[i]}，" +
                    $"預期圖片={expectedSprites[i]?.name ?? "None"}，" +
                    $"Sprite={image.sprite?.name ?? "None"}，" +
                    $"OverrideSprite={image.overrideSprite?.name ?? "None"}，" +
                    $"路徑={path}",
                    image
                );
            }

            yield return new WaitForEndOfFrame();
        }
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


        // 提前選定變換對象，但此時不修改牌堆。
        currentTransformResult = pendingTransformEffect != null
            ? pendingTransformEffect.PrepareTransform(pendingTransformContext)
            : new CardTransformResult(false);

        bool spawnSuccess =
            SpawnAnimationPrefab(
                animationData
            );


        if (!spawnSuccess)
        {
            animationFinished = true;
            yield break;
        }

        // Prefab 已生成，現在先讓動畫卡片顯示原牌。
        BindCorruptedCardToAnimationTemplate(currentTransformResult);
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
        if (animationData == null) return false;
        if (animationData.animationPrefab == null) return false;

        DestroyCurrentAnimationPrefab();

        Transform parent = AnimationRoot;
        currentAnimationObject = Instantiate(animationData.animationPrefab, parent, false);

        if (currentAnimationObject == null)
        {
            Debug.LogWarning("[GodCardAnimation] Instantiate Animation Prefab 失敗");
            return false;
        }

        currentAnimationObject.SetActive(true);

        RectTransform rect = currentAnimationObject.GetComponent<RectTransform>();

        if (rect != null)
        {
            if (stretchAnimationPrefabToRoot)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            rect.localPosition = Vector3.zero;
            //rect.localRotation = Quaternion.identity;

            // 保留 Prefab 原本的 localScale，不要設為 Vector3.one。
        }
        else
        {
            Transform spawnedTransform = currentAnimationObject.transform;
            spawnedTransform.localPosition = Vector3.zero;

            // 保留 Prefab 原本的 localRotation。
        }

        GodCardAnimationReferences references = currentAnimationObject.GetComponent<GodCardAnimationReferences>();

        if (references == null)
        {
            references = currentAnimationObject.GetComponentInChildren<GodCardAnimationReferences>(true);
        }

        if (references != null)
        {
            animatedCorruptedCardTemplate = references.animatedCorruptedCardTemplate;
            animatedCardCanvasGroup = references.animatedCardCanvasGroup;

            if (animatedCardCanvasGroup == null && animatedCorruptedCardTemplate != null)
            {
                animatedCardCanvasGroup = animatedCorruptedCardTemplate.GetComponent<CanvasGroup>();
            }
        }
        else
        {
            Debug.LogWarning($"[GodCardAnimation] Prefab {animationData.animationPrefab.name} 找不到 GodCardAnimationReferences。");
        }

        currentAnimationAnimator = currentAnimationObject.GetComponent<Animator>();

        if (currentAnimationAnimator == null)
        {
            currentAnimationAnimator = currentAnimationObject.GetComponentInChildren<Animator>(true);
        }

        if (currentAnimationAnimator == null)
        {
            Debug.LogWarning($"[GodCardAnimation] Prefab {animationData.animationPrefab.name} 找不到 Animator");
            DestroyCurrentAnimationPrefab();
            return false;
        }

        currentSignalEmitter = currentAnimationObject.GetComponent<GodCardAnimationSignalEmitter>();

        if (currentSignalEmitter == null)
        {
            currentSignalEmitter = currentAnimationObject.GetComponentInChildren<GodCardAnimationSignalEmitter>(true);
        }

        if (currentSignalEmitter == null)
        {
            Debug.LogWarning($"[GodCardAnimation] Prefab {animationData.animationPrefab.name} 找不到 GodCardAnimationSignalEmitter。如果 Signal 沒有發出，最後會使用保底 Transform。");
        }
        else
        {
            currentSignalEmitter.TransformMoment -= OnTransformMomentSignal;
            currentSignalEmitter.AnimationFinished -= OnAnimationFinishedSignal;
            currentSignalEmitter.TransformMoment += OnTransformMomentSignal;
            currentSignalEmitter.AnimationFinished += OnAnimationFinishedSignal;
        }

        Debug.Log($"[GodCardAnimation] 動畫 Prefab 已生成：{currentAnimationObject.name}，Parent = {parent.name}，Prefab Scale = {animationData.animationPrefab.transform.localScale}，Clone Scale = {currentAnimationObject.transform.localScale}，Parent World Scale = {parent.lossyScale}");

        return true;
    }


    // =========================================================
    // Destroy Animation Prefab
    // =========================================================

    private void DestroyCurrentAnimationPrefab()
    {
        if (currentSignalEmitter != null)
        {
            currentSignalEmitter.TransformMoment -= OnTransformMomentSignal;
            currentSignalEmitter.AnimationFinished -= OnAnimationFinishedSignal;
        }

        currentSignalEmitter = null;

        if (currentAnimationObject != null)
        {
            Debug.Log($"[GodCardAnimation] Destroy 動畫 Prefab：{currentAnimationObject.name}");
            Destroy(currentAnimationObject);
        }

        currentAnimationObject = null;
        currentAnimationAnimator = null;
        animatedCorruptedCardTemplate = null;
        animatedCardCanvasGroup = null;
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

        if (playedCardView == null) yield break;

        RectTransform playedCardRect = playedCardView.GetComponent<RectTransform>();

        if (playedCardRect == null)
        {
            Destroy(playedCardView.gameObject);
            yield break;
        }

        CanvasGroup playedCardCanvasGroup = playedCardView.GetComponent<CanvasGroup>();

        if (playedCardCanvasGroup == null) playedCardCanvasGroup = playedCardView.gameObject.AddComponent<CanvasGroup>();

        Transform animationTransform = null;
        CanvasGroup animationCanvasGroup = null;

        if (currentAnimationObject != null)
        {
            animationTransform = currentAnimationObject.transform;
            animationCanvasGroup = currentAnimationObject.GetComponent<CanvasGroup>();

            if (animationCanvasGroup == null) animationCanvasGroup = currentAnimationObject.AddComponent<CanvasGroup>();
        }

        float playedCardStartAlpha = playedCardCanvasGroup.alpha;
        Vector3 playedCardStartScale = playedCardRect.localScale;

        float animationStartAlpha = 1f;
        Vector3 animationStartScale = Vector3.one;

        if (animationTransform != null) animationStartScale = animationTransform.localScale;
        if (animationCanvasGroup != null) animationStartAlpha = animationCanvasGroup.alpha;

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
            playedCardRect.localScale = Vector3.Lerp(playedCardStartScale, playedCardStartScale * 0.2f, t);

            if (animationTransform != null) animationTransform.localScale = Vector3.Lerp(animationStartScale, animationStartScale * 0.2f, t);
            if (animationCanvasGroup != null) animationCanvasGroup.alpha = Mathf.Lerp(animationStartAlpha, 0f, t);

            yield return null;
        }

        playedCardCanvasGroup.alpha = 0f;
        playedCardRect.localScale = playedCardStartScale * 0.2f;

        if (animationTransform != null) animationTransform.localScale = animationStartScale * 0.2f;
        if (animationCanvasGroup != null) animationCanvasGroup.alpha = 0f;

        Destroy(playedCardView.gameObject);
    }


    // =========================================================
    // Shake
    // =========================================================

    private IEnumerator ShakeRect(RectTransform rect, float duration, float strength)
    {
        if (rect == null || duration <= 0f || strength <= 0f) yield break;

        Canvas sourceCanvas = rect.GetComponentInParent<Canvas>();
        RectTransform parentRect = rect.parent as RectTransform;

        if (sourceCanvas == null || parentRect == null)
        {
            Debug.LogWarning("[GodCardAnimation] ShakeRect 找不到 Canvas 或 RectTransform 父物件，略過震動。");
            yield break;
        }

        sourceCanvas = sourceCanvas.rootCanvas;

        Camera sourceCamera = sourceCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : sourceCanvas.worldCamera;

        if (sourceCanvas.renderMode == RenderMode.ScreenSpaceCamera && sourceCamera == null)
        {
            Debug.LogWarning("[GodCardAnimation] ShakeRect 的 Canvas 未指定 Render Camera，略過震動。");
            yield break;
        }

        Vector3 originalWorldPosition = rect.position;
        Vector2 originalScreenPosition = RectTransformUtility.WorldToScreenPoint(sourceCamera, originalWorldPosition);
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            Vector2 screenOffset = new Vector2(Random.Range(-strength, strength), Random.Range(-strength, strength));
            Vector2 targetScreenPosition = originalScreenPosition + screenOffset;

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parentRect, targetScreenPosition, sourceCamera, out Vector3 targetWorldPosition))
            {
                rect.position = targetWorldPosition;
            }

            yield return null;
        }

        rect.position = originalWorldPosition;
    }

    private void ReparentCardPreservingScreenAppearance(RectTransform cardRect, RectTransform destinationRoot)
    {
        if (cardRect == null || destinationRoot == null) return;

        Vector3 displayedLocalScale = new Vector3(displayScale.x, displayScale.y, 1f);
        Canvas sourceCanvas = cardRect.GetComponentInParent<Canvas>();
        Canvas destinationCanvas = destinationRoot.GetComponentInParent<Canvas>();

        if (sourceCanvas == null || destinationCanvas == null)
        {
            Debug.LogWarning("[GodCardAnimation] 找不到來源或目標 Canvas，只切換父物件並套用 Display Scale。");
            cardRect.SetParent(destinationRoot, false);
            cardRect.localScale = displayedLocalScale;
            cardRect.SetAsLastSibling();
            return;
        }

        sourceCanvas = sourceCanvas.rootCanvas;
        destinationCanvas = destinationCanvas.rootCanvas;

        Camera sourceCamera = sourceCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : sourceCanvas.worldCamera;
        Camera destinationCamera = destinationCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : destinationCanvas.worldCamera;
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(sourceCamera, cardRect.position);

        cardRect.SetParent(destinationRoot, false);
        cardRect.SetAsLastSibling();

        if (destinationCanvas.renderMode == RenderMode.ScreenSpaceCamera && destinationCamera == null)
        {
            Debug.LogWarning("[GodCardAnimation] 目標 Canvas 未指定 Render Camera，無法維持卡牌螢幕位置。");
            cardRect.localScale = displayedLocalScale;
            return;
        }

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(destinationRoot, screenPosition, destinationCamera, out Vector3 destinationWorldPosition))
        {
            cardRect.position = destinationWorldPosition;
        }
        else
        {
            Debug.LogWarning("[GodCardAnimation] 無法將卡牌螢幕位置換算到 Card Root。");
        }

        SetLayerRecursively(cardRect, destinationCanvas.gameObject.layer);

        Canvas[] nestedCanvases = cardRect.GetComponentsInChildren<Canvas>(true);

        foreach (Canvas nestedCanvas in nestedCanvases)
        {
            if (nestedCanvas.renderMode == RenderMode.ScreenSpaceCamera) nestedCanvas.worldCamera = destinationCamera;
        }

        cardRect.localScale = displayedLocalScale;
    }


    private void SetLayerRecursively(Transform root, int layer)
    {
        if (root == null) return;

        root.gameObject.layer = layer;

        for (int i = 0; i < root.childCount; i++)
        {
            SetLayerRecursively(root.GetChild(i), layer);
        }
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