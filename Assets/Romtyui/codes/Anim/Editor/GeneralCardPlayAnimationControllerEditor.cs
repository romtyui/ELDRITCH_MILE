using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GeneralCardPlayAnimationController))]
public class GeneralCardPlayAnimationControllerEditor : Editor
{
    // =========================================================
    // Animation Type
    // =========================================================

    private SerializedProperty animationType;


    // =========================================================
    // Common
    // =========================================================

    private SerializedProperty animationRoot;

    private SerializedProperty playedCardPosition;

    private SerializedProperty displayScale;

    private SerializedProperty moveDuration;

    private SerializedProperty holdDuration;

    private SerializedProperty fadeDuration;


    // =========================================================
    // Default
    // =========================================================

    private SerializedProperty punchScale;

    private SerializedProperty punchDuration;


    // =========================================================
    // Fast
    // =========================================================

    private SerializedProperty fastStretchScale;

    private SerializedProperty fastSettleDuration;


    // =========================================================
    // Heavy
    // =========================================================

    private SerializedProperty heavyChargeScale;

    private SerializedProperty heavyImpactScale;

    private SerializedProperty heavyChargeDuration;

    private SerializedProperty heavyImpactDuration;

    private SerializedProperty heavyRecoverDuration;


    // =========================================================
    // Bounce
    // =========================================================

    private SerializedProperty bounceHeight;

    private SerializedProperty bounceUpDuration;

    private SerializedProperty bounceDownDuration;

    private SerializedProperty bounceRecoverDuration;

    private SerializedProperty bounceUpScale;

    private SerializedProperty bounceSquashScale;


    // =========================================================
    // Shake
    // =========================================================

    private SerializedProperty shakeDuration;

    private SerializedProperty shakeAmount;

    private SerializedProperty shakeRotation;

    private SerializedProperty shakeScale;


    // =========================================================
    // PopIn
    // =========================================================

    private SerializedProperty popStartScale;

    private SerializedProperty popOvershootScale;

    private SerializedProperty popInDuration;

    private SerializedProperty popRecoverDuration;


    // =========================================================
    // DropDown
    // =========================================================

    private SerializedProperty dropStartHeight;

    private SerializedProperty dropDuration;

    private SerializedProperty dropSquashScale;

    private SerializedProperty dropRecoverDuration;


    // =========================================================
    // SpinIn
    // =========================================================

    private SerializedProperty spinStartRotation;

    private SerializedProperty spinStartScale;

    private SerializedProperty spinDuration;


    // =========================================================
    // Flip
    // =========================================================

    private SerializedProperty flipStartXScale;

    private SerializedProperty flipOvershootX;

    private SerializedProperty flipOpenDuration;

    private SerializedProperty flipRecoverDuration;


    // =========================================================
    // Slam
    // =========================================================

    private SerializedProperty slamStartHeight;

    private SerializedProperty slamDownDuration;

    private SerializedProperty slamOvershootDistance;

    private SerializedProperty slamImpactScale;

    private SerializedProperty slamBounceHeight;

    private SerializedProperty slamBounceDuration;

    private SerializedProperty slamRecoverDuration;


    // =========================================================
    // Flash
    // =========================================================

    private SerializedProperty flashStartScale;

    private SerializedProperty flashInDuration;

    private SerializedProperty flashMiddleAlpha;

    private SerializedProperty flashRecoverDuration;


    // =========================================================
    // Outro
    // =========================================================

    private SerializedProperty disappearScale;


    // =========================================================
    // OnEnable
    // =========================================================

    private void OnEnable()
    {
        // -----------------------------------------------------
        // Animation Type
        // -----------------------------------------------------

        animationType =
            serializedObject.FindProperty(
                "animationType"
            );


        // -----------------------------------------------------
        // Common
        // -----------------------------------------------------

        animationRoot =
            serializedObject.FindProperty(
                "animationRoot"
            );

        playedCardPosition =
            serializedObject.FindProperty(
                "playedCardPosition"
            );

        displayScale =
            serializedObject.FindProperty(
                "displayScale"
            );

        moveDuration =
            serializedObject.FindProperty(
                "moveDuration"
            );

        holdDuration =
            serializedObject.FindProperty(
                "holdDuration"
            );

        fadeDuration =
            serializedObject.FindProperty(
                "fadeDuration"
            );


        // -----------------------------------------------------
        // Default
        // -----------------------------------------------------

        punchScale =
            serializedObject.FindProperty(
                "punchScale"
            );

        punchDuration =
            serializedObject.FindProperty(
                "punchDuration"
            );


        // -----------------------------------------------------
        // Fast
        // -----------------------------------------------------

        fastStretchScale =
            serializedObject.FindProperty(
                "fastStretchScale"
            );

        fastSettleDuration =
            serializedObject.FindProperty(
                "fastSettleDuration"
            );


        // -----------------------------------------------------
        // Heavy
        // -----------------------------------------------------

        heavyChargeScale =
            serializedObject.FindProperty(
                "heavyChargeScale"
            );

        heavyImpactScale =
            serializedObject.FindProperty(
                "heavyImpactScale"
            );

        heavyChargeDuration =
            serializedObject.FindProperty(
                "heavyChargeDuration"
            );

        heavyImpactDuration =
            serializedObject.FindProperty(
                "heavyImpactDuration"
            );

        heavyRecoverDuration =
            serializedObject.FindProperty(
                "heavyRecoverDuration"
            );


        // -----------------------------------------------------
        // Bounce
        // -----------------------------------------------------

        bounceHeight =
            serializedObject.FindProperty(
                "bounceHeight"
            );

        bounceUpDuration =
            serializedObject.FindProperty(
                "bounceUpDuration"
            );

        bounceDownDuration =
            serializedObject.FindProperty(
                "bounceDownDuration"
            );

        bounceRecoverDuration =
            serializedObject.FindProperty(
                "bounceRecoverDuration"
            );

        bounceUpScale =
            serializedObject.FindProperty(
                "bounceUpScale"
            );

        bounceSquashScale =
            serializedObject.FindProperty(
                "bounceSquashScale"
            );


        // -----------------------------------------------------
        // Shake
        // -----------------------------------------------------

        shakeDuration =
            serializedObject.FindProperty(
                "shakeDuration"
            );

        shakeAmount =
            serializedObject.FindProperty(
                "shakeAmount"
            );

        shakeRotation =
            serializedObject.FindProperty(
                "shakeRotation"
            );

        shakeScale =
            serializedObject.FindProperty(
                "shakeScale"
            );


        // -----------------------------------------------------
        // PopIn
        // -----------------------------------------------------

        popStartScale =
            serializedObject.FindProperty(
                "popStartScale"
            );

        popOvershootScale =
            serializedObject.FindProperty(
                "popOvershootScale"
            );

        popInDuration =
            serializedObject.FindProperty(
                "popInDuration"
            );

        popRecoverDuration =
            serializedObject.FindProperty(
                "popRecoverDuration"
            );


        // -----------------------------------------------------
        // DropDown
        // -----------------------------------------------------

        dropStartHeight =
            serializedObject.FindProperty(
                "dropStartHeight"
            );

        dropDuration =
            serializedObject.FindProperty(
                "dropDuration"
            );

        dropSquashScale =
            serializedObject.FindProperty(
                "dropSquashScale"
            );

        dropRecoverDuration =
            serializedObject.FindProperty(
                "dropRecoverDuration"
            );


        // -----------------------------------------------------
        // SpinIn
        // -----------------------------------------------------

        spinStartRotation =
            serializedObject.FindProperty(
                "spinStartRotation"
            );

        spinStartScale =
            serializedObject.FindProperty(
                "spinStartScale"
            );

        spinDuration =
            serializedObject.FindProperty(
                "spinDuration"
            );


        // -----------------------------------------------------
        // Flip
        // -----------------------------------------------------

        flipStartXScale =
            serializedObject.FindProperty(
                "flipStartXScale"
            );

        flipOvershootX =
            serializedObject.FindProperty(
                "flipOvershootX"
            );

        flipOpenDuration =
            serializedObject.FindProperty(
                "flipOpenDuration"
            );

        flipRecoverDuration =
            serializedObject.FindProperty(
                "flipRecoverDuration"
            );


        // -----------------------------------------------------
        // Slam
        // -----------------------------------------------------

        slamStartHeight =
            serializedObject.FindProperty(
                "slamStartHeight"
            );

        slamDownDuration =
            serializedObject.FindProperty(
                "slamDownDuration"
            );

        slamOvershootDistance =
            serializedObject.FindProperty(
                "slamOvershootDistance"
            );

        slamImpactScale =
            serializedObject.FindProperty(
                "slamImpactScale"
            );

        slamBounceHeight =
            serializedObject.FindProperty(
                "slamBounceHeight"
            );

        slamBounceDuration =
            serializedObject.FindProperty(
                "slamBounceDuration"
            );

        slamRecoverDuration =
            serializedObject.FindProperty(
                "slamRecoverDuration"
            );


        // -----------------------------------------------------
        // Flash
        // -----------------------------------------------------

        flashStartScale =
            serializedObject.FindProperty(
                "flashStartScale"
            );

        flashInDuration =
            serializedObject.FindProperty(
                "flashInDuration"
            );

        flashMiddleAlpha =
            serializedObject.FindProperty(
                "flashMiddleAlpha"
            );

        flashRecoverDuration =
            serializedObject.FindProperty(
                "flashRecoverDuration"
            );


        // -----------------------------------------------------
        // Outro
        // -----------------------------------------------------

        disappearScale =
            serializedObject.FindProperty(
                "disappearScale"
            );
    }


    // =========================================================
    // Inspector
    // =========================================================

    public override void OnInspectorGUI()
    {
        serializedObject.Update();


        // =====================================================
        // Animation Type
        // =====================================================

        DrawSectionTitle(
            "Animation Type"
        );

        EditorGUILayout.PropertyField(
            animationType
        );


        EditorGUILayout.Space(8f);


        // =====================================================
        // Common
        // =====================================================

        DrawSectionTitle(
            "Common Settings"
        );


        EditorGUILayout.PropertyField(
            animationRoot
        );


        EditorGUILayout.PropertyField(
            playedCardPosition
        );


        EditorGUILayout.PropertyField(
            displayScale
        );


        EditorGUILayout.PropertyField(
            moveDuration
        );


        EditorGUILayout.PropertyField(
            holdDuration
        );


        EditorGUILayout.PropertyField(
            fadeDuration
        );


        EditorGUILayout.Space(8f);


        // =====================================================
        // 目前選擇的 Animation Type
        // =====================================================

        GeneralCardPlayAnimationType currentType =
            (
                GeneralCardPlayAnimationType
            )
            animationType.enumValueIndex;


        switch (currentType)
        {
            // =================================================
            // Default
            // =================================================

            case GeneralCardPlayAnimationType.Default:

                DrawSectionTitle(
                    "Default Settings"
                );


                EditorGUILayout.PropertyField(
                    punchScale
                );


                EditorGUILayout.PropertyField(
                    punchDuration
                );

                break;


            // =================================================
            // Fast
            // =================================================

            case GeneralCardPlayAnimationType.Fast:

                DrawSectionTitle(
                    "Fast Settings"
                );


                EditorGUILayout.PropertyField(
                    fastStretchScale
                );


                EditorGUILayout.PropertyField(
                    fastSettleDuration
                );

                break;


            // =================================================
            // Heavy
            // =================================================

            case GeneralCardPlayAnimationType.Heavy:

                DrawSectionTitle(
                    "Heavy Settings"
                );


                EditorGUILayout.PropertyField(
                    heavyChargeScale
                );


                EditorGUILayout.PropertyField(
                    heavyImpactScale
                );


                EditorGUILayout.PropertyField(
                    heavyChargeDuration
                );


                EditorGUILayout.PropertyField(
                    heavyImpactDuration
                );


                EditorGUILayout.PropertyField(
                    heavyRecoverDuration
                );

                break;


            // =================================================
            // Bounce
            // =================================================

            case GeneralCardPlayAnimationType.Bounce:

                DrawSectionTitle(
                    "Bounce Settings"
                );


                EditorGUILayout.PropertyField(
                    bounceHeight
                );


                EditorGUILayout.PropertyField(
                    bounceUpDuration
                );


                EditorGUILayout.PropertyField(
                    bounceDownDuration
                );


                EditorGUILayout.PropertyField(
                    bounceRecoverDuration
                );


                EditorGUILayout.PropertyField(
                    bounceUpScale
                );


                EditorGUILayout.PropertyField(
                    bounceSquashScale
                );

                break;


            // =================================================
            // Shake
            // =================================================

            case GeneralCardPlayAnimationType.Shake:

                DrawSectionTitle(
                    "Shake Settings"
                );


                EditorGUILayout.PropertyField(
                    shakeDuration
                );


                EditorGUILayout.PropertyField(
                    shakeAmount
                );


                EditorGUILayout.PropertyField(
                    shakeRotation
                );


                EditorGUILayout.PropertyField(
                    shakeScale
                );

                break;


            // =================================================
            // PopIn
            // =================================================

            case GeneralCardPlayAnimationType.PopIn:

                DrawSectionTitle(
                    "Pop In Settings"
                );


                EditorGUILayout.PropertyField(
                    popStartScale
                );


                EditorGUILayout.PropertyField(
                    popOvershootScale
                );


                EditorGUILayout.PropertyField(
                    popInDuration
                );


                EditorGUILayout.PropertyField(
                    popRecoverDuration
                );

                break;


            // =================================================
            // DropDown
            // =================================================

            case GeneralCardPlayAnimationType.DropDown:

                DrawSectionTitle(
                    "Drop Down Settings"
                );


                EditorGUILayout.PropertyField(
                    dropStartHeight
                );


                EditorGUILayout.PropertyField(
                    dropDuration
                );


                EditorGUILayout.PropertyField(
                    dropSquashScale
                );


                EditorGUILayout.PropertyField(
                    dropRecoverDuration
                );

                break;


            // =================================================
            // SpinIn
            // =================================================

            case GeneralCardPlayAnimationType.SpinIn:

                DrawSectionTitle(
                    "Spin In Settings"
                );


                EditorGUILayout.PropertyField(
                    spinStartRotation
                );


                EditorGUILayout.PropertyField(
                    spinStartScale
                );


                EditorGUILayout.PropertyField(
                    spinDuration
                );

                break;


            // =================================================
            // Flip
            // =================================================

            case GeneralCardPlayAnimationType.Flip:

                DrawSectionTitle(
                    "Flip Settings"
                );


                EditorGUILayout.PropertyField(
                    flipStartXScale
                );


                EditorGUILayout.PropertyField(
                    flipOvershootX
                );


                EditorGUILayout.PropertyField(
                    flipOpenDuration
                );


                EditorGUILayout.PropertyField(
                    flipRecoverDuration
                );

                break;


            // =================================================
            // Slam
            // =================================================

            case GeneralCardPlayAnimationType.Slam:

                DrawSectionTitle(
                    "Slam Settings"
                );


                EditorGUILayout.PropertyField(
                    slamStartHeight
                );


                EditorGUILayout.PropertyField(
                    slamDownDuration
                );


                EditorGUILayout.PropertyField(
                    slamOvershootDistance
                );


                EditorGUILayout.PropertyField(
                    slamImpactScale
                );


                EditorGUILayout.PropertyField(
                    slamBounceHeight
                );


                EditorGUILayout.PropertyField(
                    slamBounceDuration
                );


                EditorGUILayout.PropertyField(
                    slamRecoverDuration
                );

                break;


            // =================================================
            // Flash
            // =================================================

            case GeneralCardPlayAnimationType.Flash:

                DrawSectionTitle(
                    "Flash Settings"
                );


                EditorGUILayout.PropertyField(
                    flashStartScale
                );


                EditorGUILayout.PropertyField(
                    flashInDuration
                );


                EditorGUILayout.PropertyField(
                    flashMiddleAlpha
                );


                EditorGUILayout.PropertyField(
                    flashRecoverDuration
                );

                break;
        }


        EditorGUILayout.Space(8f);


        // =====================================================
        // Outro
        // =====================================================

        DrawSectionTitle(
            "Outro Settings"
        );


        EditorGUILayout.PropertyField(
            disappearScale
        );


        /*
         * fadeDuration 已經放在 Common Settings，
         * 所以這裡不用再畫一次。
         */


        serializedObject.ApplyModifiedProperties();
    }


    // =========================================================
    // Section Title
    // =========================================================

    private void DrawSectionTitle(
        string title
    )
    {
        EditorGUILayout.Space(4f);


        EditorGUILayout.LabelField(
            title,
            EditorStyles.boldLabel
        );
    }
}