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
    public string stepId;

    [TextArea(3, 8)]
    public string message;

    [Header("Example Image")]
    public Sprite exampleSprite;
    public bool hideExampleWhenEmpty = true;

    [Header("Advance")]
    public TutorialAdvanceMode advanceMode =
        TutorialAdvanceMode.NextButton;

    [Tooltip("Advance Mode 是 WaitForSignal 時，填入要等待的事件 ID")]
    public string requiredSignal;

    [Tooltip("收到正確事件後，延遲多久進入下一步")]
    public float signalAdvanceDelay = 0.15f;

    [Tooltip("這一步要收到幾次事件才算完成。例如要求出牌 3 次")]
    [Min(1)]
    public int requiredSignalCount = 1;

    [Header("Buttons")]
    public bool showNextButton = true;
    public bool showBackButton = true;
    public bool showSkipButton = true;
    public bool allowBack = true;

    [Header("Highlight")]
    [Tooltip("對應場景中的 TutorialTarget.targetId")]
    public string targetId;

    public bool highlightTarget = false;
    public Vector2 highlightPadding = new Vector2(20f, 20f);

    [Header("Dialog Position")]
    public TutorialDialogPosition dialogPosition =
        TutorialDialogPosition.Auto;

    public float dialogSpacing = 24f;

    [Header("Auto Advance")]
    [Tooltip("大於 0 時，進入這一步後自動前進")]
    public float autoAdvanceAfterSeconds = 0f;

    [Header("Input Rule")]
    [Tooltip("WaitForSignal 步驟是否禁止用下一步按鈕跳過")]
    public bool requireConditionToAdvance = true;

    [Header("Debug")]
    public bool logStep = true;

    public bool UsesSignal()
    {
        return advanceMode == TutorialAdvanceMode.WaitForSignal &&
               !string.IsNullOrWhiteSpace(requiredSignal);
    }

    public int GetRequiredSignalCount()
    {
        return Mathf.Max(1, requiredSignalCount);
    }
}