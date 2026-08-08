using System.Collections;
using UnityEngine;


// =============================================================
// 一般卡成功出牌動畫種類
// =============================================================

public enum GeneralCardPlayAnimationType
{
    Default,

    Fast,

    Heavy,

    Bounce,

    Shake,

    /*
     * 直接在展示位置彈出。
     * 不從手牌位置飛過去。
     */
    PopIn,

    /*
     * 從展示位置上方掉下來。
     */
    DropDown,

    /*
     * 直接在展示位置旋轉出現。
     */
    SpinIn,

    /*
     * 模擬翻牌。
     */
    Flip,

    /*
     * 從上方快速砸下。
     */
    Slam,

    /*
     * 直接閃現在展示位置。
     */
    Flash
}


public class GeneralCardPlayAnimationController : MonoBehaviour
{
    // =========================================================
    // Animation Type
    // =========================================================

    [Header("Animation Type")]

    [Tooltip(
        "一般卡成功出牌時使用的演出模式。\n\n" +

        "Default = 飛到中央 + Punch\n" +

        "Fast = 飛到中央 + 快速拉伸\n" +

        "Heavy = 飛到中央 + 蓄力爆發\n" +

        "Bounce = 飛到中央 + 上下彈跳\n" +

        "Shake = 飛到中央 + 震動\n" +

        "PopIn = 直接在展示位置彈出\n" +

        "DropDown = 從上方掉下\n" +

        "SpinIn = 旋轉出現\n" +

        "Flip = 翻牌出現\n" +

        "Slam = 從上方重砸\n" +

        "Flash = 直接閃現"
    )]
    public GeneralCardPlayAnimationType animationType =
        GeneralCardPlayAnimationType.Default;


    // =========================================================
    // Animation Root
    // =========================================================

    [Header("Animation Root")]

    [Tooltip(
        "一般卡成功打出後，暫時移到這個 UI Root 播放動畫"
    )]
    public RectTransform animationRoot;


    // =========================================================
    // Played Card Position
    // =========================================================

    [Header("Played Card Position")]

    [Tooltip(
        "卡牌成功打出後主要展示的位置"
    )]
    public Vector2 playedCardPosition =
        new Vector2(
            0f,
            120f
        );


    // =========================================================
    // Common Scale
    // =========================================================

    [Header("Scale")]

    [Tooltip(
        "卡牌正常展示時的 XY 縮放"
    )]
    public Vector2 displayScale =
        new Vector2(
            1.15f,
            1.15f
        );


    [Tooltip(
        "Default 模式 Punch 時的 XY 額外倍率"
    )]
    public Vector2 punchScale =
        new Vector2(
            1.08f,
            1.08f
        );


    // =========================================================
    // Common Timing
    // =========================================================

    [Header("Timing")]

    [Tooltip(
        "需要從手牌飛到展示位置的模式，移動所需時間"
    )]
    public float moveDuration =
        0.18f;


    [Tooltip(
        "Default 模式 Punch 總時間"
    )]
    public float punchDuration =
        0.08f;


    [Tooltip(
        "Intro 完成後，正式執行卡牌效果前停留多久"
    )]
    public float holdDuration =
        0.12f;


    [Tooltip(
        "效果結算完成後淡出時間"
    )]
    public float fadeDuration =
        0.16f;


    // =========================================================
    // Fast Settings
    // =========================================================

    [Header("Fast Settings")]

    [Tooltip(
        "快速攻擊拉伸時，相對 Display Scale 的 XY 倍率"
    )]
    public Vector2 fastStretchScale =
        new Vector2(
            1.05f,
            1.25f
        );


    [Tooltip(
        "Fast 拉伸 + 回正所使用的時間"
    )]
    public float fastSettleDuration =
        0.07f;


    // =========================================================
    // Heavy Settings
    // =========================================================

    [Header("Heavy Settings")]

    [Tooltip(
        "Heavy 蓄力時，相對 Display Scale 的 XY 倍率"
    )]
    public Vector2 heavyChargeScale =
        new Vector2(
            0.92f,
            0.88f
        );


    [Tooltip(
        "Heavy 爆發瞬間，相對 Display Scale 的 XY 倍率"
    )]
    public Vector2 heavyImpactScale =
        new Vector2(
            1.25f,
            1.35f
        );


    [Tooltip(
        "Heavy 縮小蓄力時間"
    )]
    public float heavyChargeDuration =
        0.12f;


    [Tooltip(
        "Heavy 爆發放大的時間"
    )]
    public float heavyImpactDuration =
        0.07f;


    [Tooltip(
        "Heavy 爆發後回正時間"
    )]
    public float heavyRecoverDuration =
        0.09f;


    // =========================================================
    // Bounce Settings
    // =========================================================

    [Header("Bounce Settings")]

    [Tooltip(
        "Bounce 往上彈的高度"
    )]
    public float bounceHeight =
        55f;


    [Tooltip(
        "Bounce 往上彈的時間"
    )]
    public float bounceUpDuration =
        0.10f;


    [Tooltip(
        "Bounce 往下壓的時間"
    )]
    public float bounceDownDuration =
        0.08f;


    [Tooltip(
        "Bounce 回到正常位置的時間"
    )]
    public float bounceRecoverDuration =
        0.08f;


    [Tooltip(
        "Bounce 往上時，相對 Display Scale 的 XY 倍率"
    )]
    public Vector2 bounceUpScale =
        new Vector2(
            0.95f,
            1.12f
        );


    [Tooltip(
        "Bounce 落下壓扁時，相對 Display Scale 的 XY 倍率"
    )]
    public Vector2 bounceSquashScale =
        new Vector2(
            1.15f,
            0.90f
        );


    // =========================================================
    // Shake Settings
    // =========================================================

    [Header("Shake Settings")]

    [Tooltip(
        "Shake 總震動時間"
    )]
    public float shakeDuration =
        0.22f;


    [Tooltip(
        "Shake 左右最大位移"
    )]
    public float shakeAmount =
        10f;


    [Tooltip(
        "Shake 最大旋轉角度"
    )]
    public float shakeRotation =
        3f;


    [Tooltip(
        "Shake 時，相對 Display Scale 的 XY 倍率"
    )]
    public Vector2 shakeScale =
        new Vector2(
            1.05f,
            1.05f
        );


    // =========================================================
    // PopIn Settings
    // =========================================================

    [Header("Pop In Settings")]

    [Tooltip(
        "PopIn 剛出現時，相對 Display Scale 的 XY 倍率"
    )]
    public Vector2 popStartScale =
        new Vector2(
            0.35f,
            0.35f
        );


    [Tooltip(
        "PopIn 第一次彈大時，相對 Display Scale 的 XY 倍率"
    )]
    public Vector2 popOvershootScale =
        new Vector2(
            1.18f,
            1.18f
        );


    [Tooltip(
        "PopIn 從小變大的時間"
    )]
    public float popInDuration =
        0.10f;


    [Tooltip(
        "PopIn 從 Overshoot 回正的時間"
    )]
    public float popRecoverDuration =
        0.08f;


    // =========================================================
    // DropDown Settings
    // =========================================================

    [Header("Drop Down Settings")]

    [Tooltip(
        "DropDown 從展示位置上方多高開始掉落"
    )]
    public float dropStartHeight =
        350f;


    [Tooltip(
        "DropDown 掉落所需時間"
    )]
    public float dropDuration =
        0.18f;


    [Tooltip(
        "掉落到展示位置時壓扁的 XY 倍率"
    )]
    public Vector2 dropSquashScale =
        new Vector2(
            1.18f,
            0.88f
        );


    [Tooltip(
        "DropDown 落地後恢復的時間"
    )]
    public float dropRecoverDuration =
        0.09f;


    // =========================================================
    // SpinIn Settings
    // =========================================================

    [Header("Spin In Settings")]

    [Tooltip(
        "SpinIn 剛開始時的旋轉角度"
    )]
    public float spinStartRotation =
        -180f;


    [Tooltip(
        "SpinIn 剛開始時，相對 Display Scale 的 XY 倍率"
    )]
    public Vector2 spinStartScale =
        new Vector2(
            0.45f,
            0.45f
        );


    [Tooltip(
        "SpinIn 旋轉完成時間"
    )]
    public float spinDuration =
        0.22f;


    // =========================================================
    // Flip Settings
    // =========================================================

    [Header("Flip Settings")]

    [Tooltip(
        "Flip 開始時 X 軸壓縮程度。\n" +
        "例如 0.05 代表幾乎像一條直線。"
    )]
    [Range(0.01f, 1f)]
    public float flipStartXScale =
        0.05f;


    [Tooltip(
        "Flip 第一次展開後的 X 軸 Overshoot 倍率"
    )]
    public float flipOvershootX =
        1.12f;


    [Tooltip(
        "Flip 從細線展開的時間"
    )]
    public float flipOpenDuration =
        0.12f;


    [Tooltip(
        "Flip 從 Overshoot 回正的時間"
    )]
    public float flipRecoverDuration =
        0.07f;


    // =========================================================
    // Slam Settings
    // =========================================================

    [Header("Slam Settings")]

    [Tooltip(
        "Slam 從展示位置上方多高開始"
    )]
    public float slamStartHeight =
        450f;


    [Tooltip(
        "Slam 砸下來的時間"
    )]
    public float slamDownDuration =
        0.10f;


    [Tooltip(
        "Slam 落地時往下超過展示位置多少"
    )]
    public float slamOvershootDistance =
        18f;


    [Tooltip(
        "Slam 落地瞬間，相對 Display Scale 的 XY 倍率"
    )]
    public Vector2 slamImpactScale =
        new Vector2(
            1.30f,
            0.78f
        );


    [Tooltip(
        "Slam 落地後彈回的高度"
    )]
    public float slamBounceHeight =
        28f;


    [Tooltip(
        "Slam 第一次回彈時間"
    )]
    public float slamBounceDuration =
        0.08f;


    [Tooltip(
        "Slam 最後回到正常位置的時間"
    )]
    public float slamRecoverDuration =
        0.08f;


    // =========================================================
    // Flash Settings
    // =========================================================

    [Header("Flash Settings")]

    [Tooltip(
        "Flash 出現時，相對 Display Scale 的 XY 倍率"
    )]
    public Vector2 flashStartScale =
        new Vector2(
            1.35f,
            1.35f
        );


    [Tooltip(
        "Flash 第一次出現的時間"
    )]
    public float flashInDuration =
        0.05f;


    [Tooltip(
        "Flash 中間快速變淡的最低 Alpha"
    )]
    [Range(0f, 1f)]
    public float flashMiddleAlpha =
        0.25f;


    [Tooltip(
        "Flash 第二次閃亮恢復的時間"
    )]
    public float flashRecoverDuration =
        0.08f;


    // =========================================================
    // Outro
    // =========================================================

    [Header("Outro")]

    [Tooltip(
        "消失時的 XY 縮放"
    )]
    public Vector2 disappearScale =
        new Vector2(
            0.75f,
            0.75f
        );


    // =========================================================
    // Animation Root
    // =========================================================

    public RectTransform AnimationRoot
    {
        get
        {
            return animationRoot;
        }
    }


    // =========================================================
    // Intro
    // =========================================================

    public IEnumerator PlayIntro(
        CardViewUI cardView
    )
    {
        if (cardView == null)
            yield break;


        RectTransform cardRect =
            cardView.transform as RectTransform;


        if (cardRect == null)
            yield break;


        CanvasGroup canvasGroup =
            cardView.GetComponent<CanvasGroup>();


        if (canvasGroup == null)
        {
            canvasGroup =
                cardView.gameObject
                    .AddComponent<CanvasGroup>();
        }


        canvasGroup.alpha = 1f;


        /*
         * Detach 前後保留下來的卡牌狀態。
         */
        Vector2 startPosition =
            cardRect.anchoredPosition;


        Vector3 startScale =
            cardRect.localScale;


        Quaternion startRotation =
            cardRect.localRotation;


        Vector3 displayTargetScale =
            new Vector3(
                displayScale.x,
                displayScale.y,
                1f
            );


        // =====================================================
        // 每種動畫自己決定：
        //
        // 要不要飛到中央
        // 要不要瞬間出現
        // 要不要從上面掉下
        // 要不要旋轉
        // =====================================================

        switch (animationType)
        {
            // =================================================
            // Default
            // =================================================

            case GeneralCardPlayAnimationType.Default:

                yield return MoveToDisplayPosition(
                    cardRect,
                    startPosition,
                    startScale,
                    displayTargetScale
                );


                yield return PlayDefaultAnimation(
                    cardRect,
                    displayTargetScale
                );

                break;


            // =================================================
            // Fast
            // =================================================

            case GeneralCardPlayAnimationType.Fast:

                yield return MoveToDisplayPosition(
                    cardRect,
                    startPosition,
                    startScale,
                    displayTargetScale
                );


                yield return PlayFastAnimation(
                    cardRect,
                    displayTargetScale
                );

                break;


            // =================================================
            // Heavy
            // =================================================

            case GeneralCardPlayAnimationType.Heavy:

                yield return MoveToDisplayPosition(
                    cardRect,
                    startPosition,
                    startScale,
                    displayTargetScale
                );


                yield return PlayHeavyAnimation(
                    cardRect,
                    displayTargetScale
                );

                break;


            // =================================================
            // Bounce
            // =================================================

            case GeneralCardPlayAnimationType.Bounce:

                yield return MoveToDisplayPosition(
                    cardRect,
                    startPosition,
                    startScale,
                    displayTargetScale
                );


                yield return PlayBounceAnimation(
                    cardRect,
                    displayTargetScale
                );

                break;


            // =================================================
            // Shake
            // =================================================

            case GeneralCardPlayAnimationType.Shake:

                yield return MoveToDisplayPosition(
                    cardRect,
                    startPosition,
                    startScale,
                    displayTargetScale
                );


                yield return PlayShakeAnimation(
                    cardRect,
                    displayTargetScale,
                    startRotation
                );

                break;


            // =================================================
            // PopIn
            //
            // 不飛。
            // 直接出現在 playedCardPosition。
            // =================================================

            case GeneralCardPlayAnimationType.PopIn:

                yield return PlayPopInAnimation(
                    cardRect,
                    canvasGroup,
                    displayTargetScale
                );

                break;


            // =================================================
            // DropDown
            //
            // 不從手牌飛。
            // 從 playedCardPosition 上方掉下來。
            // =================================================

            case GeneralCardPlayAnimationType.DropDown:

                yield return PlayDropDownAnimation(
                    cardRect,
                    canvasGroup,
                    displayTargetScale
                );

                break;


            // =================================================
            // SpinIn
            //
            // 不從手牌飛。
            // 在中央旋轉出現。
            // =================================================

            case GeneralCardPlayAnimationType.SpinIn:

                yield return PlaySpinInAnimation(
                    cardRect,
                    canvasGroup,
                    displayTargetScale,
                    startRotation
                );

                break;


            // =================================================
            // Flip
            //
            // 不飛。
            // 直接在展示位置做翻牌。
            // =================================================

            case GeneralCardPlayAnimationType.Flip:

                yield return PlayFlipAnimation(
                    cardRect,
                    canvasGroup,
                    displayTargetScale
                );

                break;


            // =================================================
            // Slam
            //
            // 從上方快速砸下。
            // =================================================

            case GeneralCardPlayAnimationType.Slam:

                yield return PlaySlamAnimation(
                    cardRect,
                    canvasGroup,
                    displayTargetScale
                );

                break;


            // =================================================
            // Flash
            //
            // 不移動。
            // 在展示位置瞬間閃現。
            // =================================================

            case GeneralCardPlayAnimationType.Flash:

                yield return PlayFlashAnimation(
                    cardRect,
                    canvasGroup,
                    displayTargetScale
                );

                break;
        }


        /*
         * =====================================================
         * 無論哪一種 Intro，
         * 最後都保證回到相同展示狀態。
         * =====================================================
         */

        cardRect.anchoredPosition =
            playedCardPosition;


        cardRect.localScale =
            displayTargetScale;


        cardRect.localRotation =
            startRotation;


        canvasGroup.alpha =
            1f;


        /*
         * =====================================================
         * 最後停留。
         * =====================================================
         */

        if (holdDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                holdDuration
            );
        }
    }


    // =========================================================
    // Common Move
    // =========================================================

    private IEnumerator MoveToDisplayPosition(
        RectTransform cardRect,
        Vector2 startPosition,
        Vector3 startScale,
        Vector3 displayTargetScale
    )
    {
        float elapsed =
            0f;


        while (elapsed < moveDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                moveDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(
                        elapsed /
                        moveDuration
                    );


            float smoothT =
                Smooth01(t);


            cardRect.anchoredPosition =
                Vector2.Lerp(
                    startPosition,
                    playedCardPosition,
                    smoothT
                );


            cardRect.localScale =
                Vector3.Lerp(
                    startScale,
                    displayTargetScale,
                    smoothT
                );


            yield return null;
        }


        cardRect.anchoredPosition =
            playedCardPosition;


        cardRect.localScale =
            displayTargetScale;
    }


    // =========================================================
    // Default
    // =========================================================

    private IEnumerator PlayDefaultAnimation(
        RectTransform cardRect,
        Vector3 displayTargetScale
    )
    {
        Vector3 punchTargetScale =
            MultiplyScale(
                displayTargetScale,
                punchScale
            );


        float halfPunchDuration =
            punchDuration *
            0.5f;


        yield return AnimateScale(
            cardRect,
            displayTargetScale,
            punchTargetScale,
            halfPunchDuration
        );


        yield return AnimateScale(
            cardRect,
            punchTargetScale,
            displayTargetScale,
            halfPunchDuration
        );


        cardRect.localScale =
            displayTargetScale;
    }


    // =========================================================
    // Fast
    // =========================================================

    private IEnumerator PlayFastAnimation(
        RectTransform cardRect,
        Vector3 displayTargetScale
    )
    {
        Vector3 stretchScale =
            MultiplyScale(
                displayTargetScale,
                fastStretchScale
            );


        float stretchDuration =
            Mathf.Max(
                0.01f,
                fastSettleDuration *
                0.45f
            );


        float settleDuration =
            Mathf.Max(
                0.01f,
                fastSettleDuration *
                0.55f
            );


        yield return AnimateScale(
            cardRect,
            displayTargetScale,
            stretchScale,
            stretchDuration
        );


        yield return AnimateScale(
            cardRect,
            stretchScale,
            displayTargetScale,
            settleDuration
        );


        cardRect.localScale =
            displayTargetScale;
    }


    // =========================================================
    // Heavy
    // =========================================================

    private IEnumerator PlayHeavyAnimation(
        RectTransform cardRect,
        Vector3 displayTargetScale
    )
    {
        Vector3 chargeScale =
            MultiplyScale(
                displayTargetScale,
                heavyChargeScale
            );


        Vector3 impactScale =
            MultiplyScale(
                displayTargetScale,
                heavyImpactScale
            );


        /*
         * 蓄力縮小。
         */
        yield return AnimateScaleSmooth(
            cardRect,
            displayTargetScale,
            chargeScale,
            heavyChargeDuration
        );


        /*
         * 突然爆發。
         */
        yield return AnimateScale(
            cardRect,
            chargeScale,
            impactScale,
            heavyImpactDuration
        );


        /*
         * 恢復。
         */
        yield return AnimateScaleSmooth(
            cardRect,
            impactScale,
            displayTargetScale,
            heavyRecoverDuration
        );


        cardRect.localScale =
            displayTargetScale;
    }


    // =========================================================
    // Bounce
    // =========================================================

    private IEnumerator PlayBounceAnimation(
        RectTransform cardRect,
        Vector3 displayTargetScale
    )
    {
        Vector2 basePosition =
            playedCardPosition;


        Vector2 topPosition =
            basePosition +
            new Vector2(
                0f,
                bounceHeight
            );


        Vector2 squashPosition =
            basePosition +
            new Vector2(
                0f,
                -bounceHeight *
                0.12f
            );


        Vector3 upScale =
            MultiplyScale(
                displayTargetScale,
                bounceUpScale
            );


        Vector3 squashScale =
            MultiplyScale(
                displayTargetScale,
                bounceSquashScale
            );


        /*
         * 往上。
         */
        yield return AnimatePositionAndScale(
            cardRect,
            basePosition,
            topPosition,
            displayTargetScale,
            upScale,
            bounceUpDuration
        );


        /*
         * 往下壓。
         */
        yield return AnimatePositionAndScale(
            cardRect,
            topPosition,
            squashPosition,
            upScale,
            squashScale,
            bounceDownDuration
        );


        /*
         * 回正。
         */
        yield return AnimatePositionAndScale(
            cardRect,
            squashPosition,
            basePosition,
            squashScale,
            displayTargetScale,
            bounceRecoverDuration
        );


        cardRect.anchoredPosition =
            basePosition;


        cardRect.localScale =
            displayTargetScale;
    }


    // =========================================================
    // Shake
    // =========================================================

    private IEnumerator PlayShakeAnimation(
        RectTransform cardRect,
        Vector3 displayTargetScale,
        Quaternion baseRotation
    )
    {
        Vector2 basePosition =
            playedCardPosition;


        Vector3 enlargedScale =
            MultiplyScale(
                displayTargetScale,
                shakeScale
            );


        /*
         * 開始震動以前先稍微放大。
         */
        yield return AnimateScale(
            cardRect,
            displayTargetScale,
            enlargedScale,
            0.04f
        );


        float elapsed =
            0f;


        while (elapsed < shakeDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                shakeDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(
                        elapsed /
                        shakeDuration
                    );


            /*
             * 越接近結束，
             * 震動幅度越小。
             */
            float strength =
                1f - t;


            float offsetX =
                Random.Range(
                    -shakeAmount,
                    shakeAmount
                )
                *
                strength;


            cardRect.anchoredPosition =
                basePosition +
                new Vector2(
                    offsetX,
                    0f
                );


            float rotationOffset =
                Random.Range(
                    -shakeRotation,
                    shakeRotation
                )
                *
                strength;


            cardRect.localRotation =
                baseRotation *
                Quaternion.Euler(
                    0f,
                    0f,
                    rotationOffset
                );


            yield return null;
        }


        cardRect.anchoredPosition =
            basePosition;


        cardRect.localRotation =
            baseRotation;


        yield return AnimateScale(
            cardRect,
            enlargedScale,
            displayTargetScale,
            0.05f
        );


        cardRect.localScale =
            displayTargetScale;
    }


    // =========================================================
    // PopIn
    // =========================================================

    private IEnumerator PlayPopInAnimation(
        RectTransform cardRect,
        CanvasGroup canvasGroup,
        Vector3 displayTargetScale
    )
    {
        /*
         * 直接放到展示位置。
         * 完全不播放飛行。
         */
        cardRect.anchoredPosition =
            playedCardPosition;


        Vector3 startScale =
            MultiplyScale(
                displayTargetScale,
                popStartScale
            );


        Vector3 overshootScale =
            MultiplyScale(
                displayTargetScale,
                popOvershootScale
            );


        cardRect.localScale =
            startScale;


        canvasGroup.alpha =
            0f;


        /*
         * 小 → 大。
         * Alpha 同時出現。
         */
        yield return AnimateScaleAndAlpha(
            cardRect,
            canvasGroup,
            startScale,
            overshootScale,
            0f,
            1f,
            popInDuration
        );


        /*
         * Overshoot → 正常。
         */
        yield return AnimateScaleSmooth(
            cardRect,
            overshootScale,
            displayTargetScale,
            popRecoverDuration
        );


        cardRect.localScale =
            displayTargetScale;


        canvasGroup.alpha =
            1f;
    }


    // =========================================================
    // DropDown
    // =========================================================

    private IEnumerator PlayDropDownAnimation(
        RectTransform cardRect,
        CanvasGroup canvasGroup,
        Vector3 displayTargetScale
    )
    {
        Vector2 startPosition =
            playedCardPosition +
            new Vector2(
                0f,
                dropStartHeight
            );


        Vector3 squashScale =
            MultiplyScale(
                displayTargetScale,
                dropSquashScale
            );


        /*
         * 直接移到畫面上方。
         */
        cardRect.anchoredPosition =
            startPosition;


        cardRect.localScale =
            displayTargetScale;


        canvasGroup.alpha =
            0f;


        /*
         * 從上方掉落。
         *
         * 落下時逐漸出現，
         * 並變成 Squash Scale。
         */
        yield return AnimatePositionScaleAndAlpha(
            cardRect,
            canvasGroup,

            startPosition,
            playedCardPosition,

            displayTargetScale,
            squashScale,

            0f,
            1f,

            dropDuration
        );


        /*
         * 落地之後回正。
         */
        yield return AnimateScaleSmooth(
            cardRect,
            squashScale,
            displayTargetScale,
            dropRecoverDuration
        );


        cardRect.anchoredPosition =
            playedCardPosition;


        cardRect.localScale =
            displayTargetScale;


        canvasGroup.alpha =
            1f;
    }


    // =========================================================
    // SpinIn
    // =========================================================

    private IEnumerator PlaySpinInAnimation(
        RectTransform cardRect,
        CanvasGroup canvasGroup,
        Vector3 displayTargetScale,
        Quaternion baseRotation
    )
    {
        /*
         * 不從手牌移動。
         * 直接在中央生成旋轉感。
         */

        cardRect.anchoredPosition =
            playedCardPosition;


        Vector3 startScale =
            MultiplyScale(
                displayTargetScale,
                spinStartScale
            );


        Quaternion spinRotation =
            baseRotation *
            Quaternion.Euler(
                0f,
                0f,
                spinStartRotation
            );


        cardRect.localScale =
            startScale;


        cardRect.localRotation =
            spinRotation;


        canvasGroup.alpha =
            0f;


        float elapsed =
            0f;


        while (elapsed < spinDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                spinDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(
                        elapsed /
                        spinDuration
                    );


            float smoothT =
                Smooth01(t);


            cardRect.localScale =
                Vector3.Lerp(
                    startScale,
                    displayTargetScale,
                    smoothT
                );


            cardRect.localRotation =
                Quaternion.Lerp(
                    spinRotation,
                    baseRotation,
                    smoothT
                );


            canvasGroup.alpha =
                Mathf.Lerp(
                    0f,
                    1f,
                    smoothT
                );


            yield return null;
        }


        cardRect.localScale =
            displayTargetScale;


        cardRect.localRotation =
            baseRotation;


        canvasGroup.alpha =
            1f;
    }


    // =========================================================
    // Flip
    // =========================================================

    private IEnumerator PlayFlipAnimation(
        RectTransform cardRect,
        CanvasGroup canvasGroup,
        Vector3 displayTargetScale
    )
    {
        /*
         * Unity UI 沒有真正的 3D 翻卡需求時，
         * 用 X Scale 壓扁可以做出非常穩定的翻牌感。
         */

        cardRect.anchoredPosition =
            playedCardPosition;


        Vector3 thinScale =
            new Vector3(
                displayTargetScale.x *
                    flipStartXScale,

                displayTargetScale.y,

                displayTargetScale.z
            );


        Vector3 overshootScale =
            new Vector3(
                displayTargetScale.x *
                    flipOvershootX,

                displayTargetScale.y,

                displayTargetScale.z
            );


        cardRect.localScale =
            thinScale;


        canvasGroup.alpha =
            0f;


        /*
         * 細線 → 展開。
         */
        yield return AnimateScaleAndAlpha(
            cardRect,
            canvasGroup,

            thinScale,
            overshootScale,

            0f,
            1f,

            flipOpenDuration
        );


        /*
         * Overshoot → 正常。
         */
        yield return AnimateScaleSmooth(
            cardRect,
            overshootScale,
            displayTargetScale,
            flipRecoverDuration
        );


        cardRect.localScale =
            displayTargetScale;


        canvasGroup.alpha =
            1f;
    }


    // =========================================================
    // Slam
    // =========================================================

    private IEnumerator PlaySlamAnimation(
        RectTransform cardRect,
        CanvasGroup canvasGroup,
        Vector3 displayTargetScale
    )
    {
        Vector2 startPosition =
            playedCardPosition +
            new Vector2(
                0f,
                slamStartHeight
            );


        Vector2 impactPosition =
            playedCardPosition +
            new Vector2(
                0f,
                -slamOvershootDistance
            );


        Vector2 bouncePosition =
            playedCardPosition +
            new Vector2(
                0f,
                slamBounceHeight
            );


        Vector3 impactScale =
            MultiplyScale(
                displayTargetScale,
                slamImpactScale
            );


        /*
         * 從上方出現。
         */
        cardRect.anchoredPosition =
            startPosition;


        cardRect.localScale =
            displayTargetScale;


        canvasGroup.alpha =
            1f;


        /*
         * 快速往下砸。
         */
        yield return AnimatePositionAndScale(
            cardRect,

            startPosition,
            impactPosition,

            displayTargetScale,
            impactScale,

            slamDownDuration
        );


        /*
         * 落地後往上彈。
         */
        yield return AnimatePositionAndScale(
            cardRect,

            impactPosition,
            bouncePosition,

            impactScale,
            displayTargetScale,

            slamBounceDuration
        );


        /*
         * 最後落回展示位置。
         */
        yield return AnimatePositionAndScale(
            cardRect,

            bouncePosition,
            playedCardPosition,

            displayTargetScale,
            displayTargetScale,

            slamRecoverDuration
        );


        cardRect.anchoredPosition =
            playedCardPosition;


        cardRect.localScale =
            displayTargetScale;
    }


    // =========================================================
    // Flash
    // =========================================================

    private IEnumerator PlayFlashAnimation(
        RectTransform cardRect,
        CanvasGroup canvasGroup,
        Vector3 displayTargetScale
    )
    {
        /*
         * 直接在中央出現。
         * 完全不飛。
         */

        cardRect.anchoredPosition =
            playedCardPosition;


        Vector3 flashScale =
            MultiplyScale(
                displayTargetScale,
                flashStartScale
            );


        cardRect.localScale =
            flashScale;


        canvasGroup.alpha =
            0f;


        /*
         * 第一次瞬間亮起。
         */
        yield return AnimateScaleAndAlpha(
            cardRect,
            canvasGroup,

            flashScale,
            displayTargetScale,

            0f,
            1f,

            flashInDuration
        );


        /*
         * 快速變淡。
         */
        yield return AnimateAlpha(
            canvasGroup,
            1f,
            flashMiddleAlpha,
            flashRecoverDuration *
            0.45f
        );


        /*
         * 再恢復完整 Alpha。
         */
        yield return AnimateAlpha(
            canvasGroup,
            flashMiddleAlpha,
            1f,
            flashRecoverDuration *
            0.55f
        );


        cardRect.localScale =
            displayTargetScale;


        canvasGroup.alpha =
            1f;
    }


    // =========================================================
    // Helper
    // Animate Scale
    // =========================================================

    private IEnumerator AnimateScale(
        RectTransform cardRect,
        Vector3 from,
        Vector3 to,
        float duration
    )
    {
        if (duration <= 0f)
        {
            cardRect.localScale =
                to;

            yield break;
        }


        float elapsed =
            0f;


        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    duration
                );


            cardRect.localScale =
                Vector3.Lerp(
                    from,
                    to,
                    t
                );


            yield return null;
        }


        cardRect.localScale =
            to;
    }


    // =========================================================
    // Helper
    // Smooth Scale
    // =========================================================

    private IEnumerator AnimateScaleSmooth(
        RectTransform cardRect,
        Vector3 from,
        Vector3 to,
        float duration
    )
    {
        if (duration <= 0f)
        {
            cardRect.localScale =
                to;

            yield break;
        }


        float elapsed =
            0f;


        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    duration
                );


            float smoothT =
                Smooth01(t);


            cardRect.localScale =
                Vector3.Lerp(
                    from,
                    to,
                    smoothT
                );


            yield return null;
        }


        cardRect.localScale =
            to;
    }


    // =========================================================
    // Helper
    // Position + Scale
    // =========================================================

    private IEnumerator AnimatePositionAndScale(
        RectTransform cardRect,

        Vector2 fromPosition,
        Vector2 toPosition,

        Vector3 fromScale,
        Vector3 toScale,

        float duration
    )
    {
        if (duration <= 0f)
        {
            cardRect.anchoredPosition =
                toPosition;


            cardRect.localScale =
                toScale;


            yield break;
        }


        float elapsed =
            0f;


        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    duration
                );


            float smoothT =
                Smooth01(t);


            cardRect.anchoredPosition =
                Vector2.Lerp(
                    fromPosition,
                    toPosition,
                    smoothT
                );


            cardRect.localScale =
                Vector3.Lerp(
                    fromScale,
                    toScale,
                    smoothT
                );


            yield return null;
        }


        cardRect.anchoredPosition =
            toPosition;


        cardRect.localScale =
            toScale;
    }


    // =========================================================
    // Helper
    // Scale + Alpha
    // =========================================================

    private IEnumerator AnimateScaleAndAlpha(
        RectTransform cardRect,
        CanvasGroup canvasGroup,

        Vector3 fromScale,
        Vector3 toScale,

        float fromAlpha,
        float toAlpha,

        float duration
    )
    {
        if (duration <= 0f)
        {
            cardRect.localScale =
                toScale;


            canvasGroup.alpha =
                toAlpha;


            yield break;
        }


        float elapsed =
            0f;


        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    duration
                );


            float smoothT =
                Smooth01(t);


            cardRect.localScale =
                Vector3.Lerp(
                    fromScale,
                    toScale,
                    smoothT
                );


            canvasGroup.alpha =
                Mathf.Lerp(
                    fromAlpha,
                    toAlpha,
                    smoothT
                );


            yield return null;
        }


        cardRect.localScale =
            toScale;


        canvasGroup.alpha =
            toAlpha;
    }


    // =========================================================
    // Helper
    // Position + Scale + Alpha
    // =========================================================

    private IEnumerator AnimatePositionScaleAndAlpha(
        RectTransform cardRect,
        CanvasGroup canvasGroup,

        Vector2 fromPosition,
        Vector2 toPosition,

        Vector3 fromScale,
        Vector3 toScale,

        float fromAlpha,
        float toAlpha,

        float duration
    )
    {
        if (duration <= 0f)
        {
            cardRect.anchoredPosition =
                toPosition;


            cardRect.localScale =
                toScale;


            canvasGroup.alpha =
                toAlpha;


            yield break;
        }


        float elapsed =
            0f;


        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    duration
                );


            float smoothT =
                Smooth01(t);


            cardRect.anchoredPosition =
                Vector2.Lerp(
                    fromPosition,
                    toPosition,
                    smoothT
                );


            cardRect.localScale =
                Vector3.Lerp(
                    fromScale,
                    toScale,
                    smoothT
                );


            canvasGroup.alpha =
                Mathf.Lerp(
                    fromAlpha,
                    toAlpha,
                    smoothT
                );


            yield return null;
        }


        cardRect.anchoredPosition =
            toPosition;


        cardRect.localScale =
            toScale;


        canvasGroup.alpha =
            toAlpha;
    }


    // =========================================================
    // Helper
    // Alpha
    // =========================================================

    private IEnumerator AnimateAlpha(
        CanvasGroup canvasGroup,

        float fromAlpha,
        float toAlpha,

        float duration
    )
    {
        if (duration <= 0f)
        {
            canvasGroup.alpha =
                toAlpha;

            yield break;
        }


        float elapsed =
            0f;


        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    duration
                );


            float smoothT =
                Smooth01(t);


            canvasGroup.alpha =
                Mathf.Lerp(
                    fromAlpha,
                    toAlpha,
                    smoothT
                );


            yield return null;
        }


        canvasGroup.alpha =
            toAlpha;
    }


    // =========================================================
    // Helper
    // Multiply XY Scale
    // =========================================================

    private Vector3 MultiplyScale(
        Vector3 baseScale,
        Vector2 multiplier
    )
    {
        return new Vector3(
            baseScale.x *
                multiplier.x,

            baseScale.y *
                multiplier.y,

            baseScale.z
        );
    }


    // =========================================================
    // Helper
    // SmoothStep 0 ~ 1
    // =========================================================

    private float Smooth01(
        float t
    )
    {
        t =
            Mathf.Clamp01(t);


        return
            t *
            t *
            (
                3f -
                2f *
                t
            );
    }


    // =========================================================
    // Outro
    // =========================================================

    public IEnumerator PlayOutro(
        CardViewUI cardView
    )
    {
        if (cardView == null)
            yield break;


        RectTransform cardRect =
            cardView.transform as RectTransform;


        if (cardRect == null)
            yield break;


        CanvasGroup canvasGroup =
            cardView.GetComponent<CanvasGroup>();


        if (canvasGroup == null)
        {
            canvasGroup =
                cardView.gameObject
                    .AddComponent<CanvasGroup>();
        }


        Vector3 startScale =
            cardRect.localScale;


        Vector3 targetScale =
            new Vector3(
                disappearScale.x,
                disappearScale.y,
                1f
            );


        float startAlpha =
            canvasGroup.alpha;


        float elapsed =
            0f;


        /*
         * =====================================================
         * 原本 Outro 功能保留：
         *
         * Scale ↓
         * Alpha ↓
         * =====================================================
         */

        while (elapsed < fadeDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                fadeDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(
                        elapsed /
                        fadeDuration
                    );


            float smoothT =
                Smooth01(t);


            cardRect.localScale =
                Vector3.Lerp(
                    startScale,
                    targetScale,
                    smoothT
                );


            canvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    0f,
                    smoothT
                );


            yield return null;
        }


        cardRect.localScale =
            targetScale;


        canvasGroup.alpha =
            0f;
    }
}