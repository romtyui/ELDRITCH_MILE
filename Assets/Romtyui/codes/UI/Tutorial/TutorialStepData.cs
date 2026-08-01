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

    [Header("Dialogue Pause")]

    [Tooltip(
    "開啟後，這個步驟播放對話時會將 Time.timeScale 設為 0。" +
    "對話結束並進入 Instruction 或下一步時，會恢復原本的時間速度。"
)]
    public bool pauseGameDuringDialogue;

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

    [Header("Wait For Signal Interaction")]

    [Tooltip(
    "只對 WaitForSignal 有效。" +
    "收到開始操作 Signal 後，暫時隱藏黑幕、高光與說明框。"
)]
    public bool hideVisualsAfterInteractionStart;

    [Tooltip(
        "玩家開始操作時發送的 Signal。" +
        "例如抓起卡牌：Battle_CardGrabStarted"
    )]
    public string interactionStartSignal;

    [Tooltip(
        "代表玩家操作不正確的 Signal。" +
        "可設定多種失敗情況。"
    )]
    public List<string> incorrectSignals = new();

    [Tooltip(
        "收到 Incorrect Signal 後播放的糾正對話。" +
        "播放完會回到目前這個步驟。"
    )]
    public List<TutorialDialogueLine>
        correctionDialogueLines = new();
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

    [Header("Dialogue Position")]

    [Tooltip(
    "是否讓 DialoguePanel 依照指定目標定位。" +
    "關閉時維持 Prefab 原本的位置。"
)]
    public bool positionDialoguePanel;

    [Tooltip(
        "對話框定位使用的 TutorialTarget ID。" +
        "留空時自動使用 Highlight 的 targetId。"
    )]
    public string dialogueTargetId;

    [Tooltip(
        "對話框出現在目標的哪個方向。" +
        "Auto 會自動選擇較不容易超出畫面的位置。"
    )]
    public TutorialDialogPosition dialoguePosition =
        TutorialDialogPosition.Auto;

    [Tooltip("對話框與目標之間的距離")]
    public float dialogueSpacing = 24f;

    [Tooltip(
        "對話框距離畫面邊緣至少保留多少空間"
    )]
    public Vector2 dialogueScreenPadding =
        new Vector2(24f, 24f);

    [Tooltip(
        "目標移動時，是否讓對話框持續跟隨。" +
        "適合動畫物件、移動中的敵人或動態 UI。"
    )]
    public bool followDialogueTarget;

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
    public bool UsesInteractionStartSignal()
    {
        return
            UsesSignal() &&
            hideVisualsAfterInteractionStart &&
            !string.IsNullOrWhiteSpace(
                interactionStartSignal
            );
    }

    public bool HasCorrectionDialogue()
    {
        return
            correctionDialogueLines != null &&
            correctionDialogueLines.Count > 0;
    }

    public bool IsIncorrectSignal(
        string receivedSignal
    )
    {
        if (string.IsNullOrWhiteSpace(
                receivedSignal))
        {
            return false;
        }

        if (incorrectSignals == null)
            return false;

        string received =
            receivedSignal.Trim();

        for (int i = 0;
             i < incorrectSignals.Count;
             i++)
        {
            string incorrectSignal =
                incorrectSignals[i];

            if (string.IsNullOrWhiteSpace(
                    incorrectSignal))
            {
                continue;
            }

            if (string.Equals(
                    incorrectSignal.Trim(),
                    received,
                    System.StringComparison
                        .OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}