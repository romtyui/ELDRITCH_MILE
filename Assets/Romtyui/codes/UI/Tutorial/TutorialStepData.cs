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
    public TutorialAdvanceMode advanceMode = TutorialAdvanceMode.NextButton;

    [Tooltip("Advance Mode 是 WaitForSignal 時使用")]
    public string requiredSignal;

    public float signalAdvanceDelay = 0.15f;

    [Header("Buttons")]
    public bool showNextButton = true;
    public bool showBackButton = true;
    public bool showSkipButton = true;
    public bool allowBack = true;

    [Header("Highlight")]
    [Tooltip("對應場景中的 TutorialTarget targetId")]
    public string targetId;

    public bool highlightTarget = false;
    public Vector2 highlightPadding = new Vector2(20f, 20f);

    [Header("Dialog Position")]
    public TutorialDialogPosition dialogPosition = TutorialDialogPosition.Auto;
    public float dialogSpacing = 24f;

    [Header("Auto Advance")]
    [Tooltip("大於 0 時，進入這一步後會自動前進")]
    public float autoAdvanceAfterSeconds = 0f;

    [Header("Debug")]
    public bool logStep = true;
}