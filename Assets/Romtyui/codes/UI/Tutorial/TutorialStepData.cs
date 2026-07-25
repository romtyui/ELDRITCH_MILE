using System.Collections.Generic;
using UnityEngine;

public enum TutorialAdvanceMode
{
    NextButton,
    AnyScreenClick,
    WaitForSignal
}

public enum TutorialDialogPosition
{
    KeepCurrent,
    Center,
    Top,
    Bottom,
    Left,
    Right,
    Auto
}

[CreateAssetMenu(
    fileName = "TutorialStep",
    menuName = "Tutorial/Tutorial Step"
)]
public class TutorialStepData : ScriptableObject
{
    [Header("Basic")]
    [Tooltip("每個步驟最好使用不同 ID")]
    public string stepId;

    [Header("Dialogue Before Instruction")]
    [Tooltip("這些對話播放完畢後，才會顯示功能說明框")]
    public List<TutorialDialogueLine> dialogueLines = new();

    [Header("Instruction")]
    [TextArea(3, 8)]
    public string message;

    [Tooltip("開啟後，播放完對話就直接進入下一步，不顯示功能說明框")]
    public bool dialogueOnly;

    [Header("Example Image")]
    public Sprite exampleSprite;

    public bool hideExampleWhenEmpty = true;

    [Header("Advance")]
    public TutorialAdvanceMode advanceMode =
        TutorialAdvanceMode.NextButton;

    [Tooltip("Advance Mode 為 WaitForSignal 時等待的事件 ID")]
    public string requiredSignal;

    [Tooltip("收到正確事件後，延遲多久才進入下一步")]
    [Min(0f)]
    public float signalAdvanceDelay = 0.15f;

    [Tooltip("要收到幾次正確事件才算完成")]
    [Min(1)]
    public int requiredSignalCount = 1;

    [Tooltip("WaitForSignal 步驟是否禁止使用下一步按鈕跳過")]
    public bool requireConditionToAdvance = true;

    [Header("Buttons")]
    public bool showNextButton = true;

    public bool showBackButton = true;

    public bool showSkipButton = true;

    public bool allowBack = true;

    [Header("Highlight")]
    [Tooltip("對應場景中的 TutorialTarget.targetId")]
    public string targetId;

    public bool highlightTarget;

    public Vector2 highlightPadding =
        new Vector2(20f, 20f);

    [Tooltip("播放前置對話時，是否就先顯示高亮洞口")]
    public bool highlightDuringDialogue;

    [Header("Instruction Position")]
    public TutorialDialogPosition dialogPosition =
        TutorialDialogPosition.Auto;

    public float dialogSpacing = 24f;

    [Header("Auto Advance")]
    [Tooltip("大於 0 時，在指定秒數後自動前進")]
    [Min(0f)]
    public float autoAdvanceAfterSeconds;

    [Header("Debug")]
    public bool logStep = true;

    public bool HasDialogue()
    {
        return dialogueLines != null &&
               dialogueLines.Count > 0;
    }

    public bool UsesSignal()
    {
        return
            advanceMode ==
            TutorialAdvanceMode.WaitForSignal &&
            !string.IsNullOrWhiteSpace(requiredSignal);
    }

    public int GetRequiredSignalCount()
    {
        return Mathf.Max(1, requiredSignalCount);
    }
}