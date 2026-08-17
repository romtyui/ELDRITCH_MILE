using System.Collections;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("UI")]
    public TutorialUI tutorialUI;

    [Header("Runtime")]
    [SerializeField] private TutorialSequenceData currentSequence;

    [SerializeField] private int currentStepIndex = -1;

    [SerializeField] private bool isPlaying;

    [Header("Signal Progress")]
    [SerializeField] private int currentSignalCount;

    [SerializeField] private string waitingSignalId;

    private float previousTimeScale = 1f;

    private Coroutine autoAdvanceCoroutine;

    private Coroutine signalAdvanceCoroutine;

    private bool dialoguePauseApplied;
    private float timeScaleBeforeDialogue = 1f;

    public bool IsPlaying => isPlaying;

    public TutorialStepData CurrentStep => GetCurrentStep();

    private RectTransform currentDialogueTargetRect;
    public enum TutorialPlaybackPhase
    {
        None,
        Dialogue,
        Instruction,
        CorrectionDialogue
    }
    [Header("Playback Phase")]
    [SerializeField]
    private TutorialPlaybackPhase playbackPhase = TutorialPlaybackPhase.None;


    [SerializeField]
    private int currentDialogueIndex = -1;
    [SerializeField]
    private int currentCorrectionDialogueIndex = -1;


    [SerializeField]
    private bool interactionVisualsHidden;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (tutorialUI == null)
        {
            tutorialUI = FindFirstObjectByType<TutorialUI>(
                FindObjectsInactive.Include
            );
        }

        BindUIEvents();
    }
    private void LateUpdate()
    {
        if (!isPlaying)
            return;

        bool isDialoguePhase =
            playbackPhase ==
                TutorialPlaybackPhase.Dialogue ||
            playbackPhase ==
                TutorialPlaybackPhase
                    .CorrectionDialogue;

        if (!isDialoguePhase)
            return;

        TutorialStepData step =
            GetCurrentStep();

        if (step == null)
            return;

        if (!step.positionDialoguePanel)
            return;

        if (!step.followDialogueTarget)
            return;

        if (tutorialUI == null)
            return;

        if (currentDialogueTargetRect == null)
        {
            currentDialogueTargetRect =
                GetDialogueTargetRect(
                    step,
                    null
                );
        }

        if (currentDialogueTargetRect == null)
            return;

        tutorialUI.PositionDialoguePanel(
            currentDialogueTargetRect,
            step.dialoguePosition,
            step.dialogueSpacing,
            step.dialogueScreenPadding
        );
    }
    private void OnEnable()
    {
        TutorialEventBus.OnSignalRaised += HandleSignal;
    }

    private void OnDisable()
    {
        TutorialEventBus.OnSignalRaised -= HandleSignal;
        /*
     * 防止物件在對話期間被停用後，
     * Time.timeScale 永遠停在 0。
     */
        RestoreDialogueGlobalPause();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        UnbindUIEvents();

        /*
         * 先恢復單一步驟對話暫停。
         */
        RestoreDialogueGlobalPause();

        /*
         * 再恢復 Sequence 層級的 pauseGame。
         */
        RestoreTimeScale();
    }

    private void BindUIEvents()
    {
        if (tutorialUI == null)
            return;

        tutorialUI.NextClicked += NextStep;
        tutorialUI.BackClicked += PreviousStep;
        tutorialUI.SkipClicked += SkipTutorial;
        tutorialUI.ScreenClicked += HandleScreenClicked;
        tutorialUI.DialogueClicked += ContinueDialogue;
    }

    private void UnbindUIEvents()
    {
        if (tutorialUI == null)
            return;

        tutorialUI.NextClicked -= NextStep;
        tutorialUI.BackClicked -= PreviousStep;
        tutorialUI.SkipClicked -= SkipTutorial;
        tutorialUI.ScreenClicked -= HandleScreenClicked;
        tutorialUI.DialogueClicked -= ContinueDialogue;
    }

    public bool TryPlay(TutorialSequenceData sequence)
    {
        if (sequence == null)
        {
            Debug.LogWarning(
                "[TutorialManager] sequence 是 null"
            );

            return false;
        }

        if (!sequence.IsValid())
        {
            Debug.LogWarning(
                $"[TutorialManager] 教學資料不完整：{sequence.name}",
                sequence
            );

            return false;
        }

        if (sequence.onlyShowOnce &&
            TutorialProgress.IsCompleted(sequence.tutorialId))
        {
            Debug.Log(
                $"[TutorialManager] 教學已完成，不播放：" +
                $"{sequence.tutorialId}"
            );

            return false;
        }

        Play(sequence, sequence.startingStepIndex);
        return true;
    }

    public void Play(
        TutorialSequenceData sequence,
        int startingIndex = 0
    )
    {
        if (sequence == null || !sequence.IsValid())
            return;

        if (isPlaying)
            StopTutorialWithoutSaving();

        currentSequence = sequence;

        currentStepIndex = Mathf.Clamp(
            startingIndex,
            0,
            sequence.steps.Count - 1
        );

        isPlaying = true;

        ApplyPauseState();

        if (tutorialUI != null)
            tutorialUI.Show(sequence.blockOtherUI);

        ShowCurrentStep();
    }

    private void ShowCurrentStep()
    {
        StopStepCoroutines();

        currentSignalCount = 0;
        waitingSignalId = string.Empty;

        currentDialogueIndex = -1;
        currentDialogueTargetRect = null;

        playbackPhase =
            TutorialPlaybackPhase.None;

        TutorialStepData step =
            GetCurrentStep();

        if (step == null)
        {
            FinishTutorial();
            return;
        }

        TutorialTarget target =
            TutorialTarget.Find(
                step.targetId
            );

        RectTransform targetRect =
            target != null
                ? target.RectTransform
                : null;

        if (step.HasDialogue())
        {
            StartDialoguePhase(
                step,
                targetRect
            );
        }
        else
        {
            StartInstructionPhase(
                step,
                targetRect
            );
        }

        if (step.logStep)
        {
            Debug.Log(
                $"[TutorialManager] Step " +
                $"{currentStepIndex + 1}/" +
                $"{currentSequence.steps.Count}：" +
                $"{step.stepId}"
            );
        }
    }
    private void StartDialoguePhase(TutorialStepData step, RectTransform targetRect)
    {
        if (step == null)
            return;

        if (step.pauseGameDuringDialogue)
        {
            ApplyDialogueGlobalPause();
        }
        else
        {
            RestoreDialogueGlobalPause();
        }

        playbackPhase = TutorialPlaybackPhase.Dialogue;

        currentDialogueIndex = 0;

        /*
         * 記住這一步 DialoguePanel 要跟隨的目標。
         *
         * step.dialogueTargetId 有填：
         * 使用 dialogueTargetId。
         *
         * dialogueTargetId 沒填：
         * 使用 step.targetId。
         */
        currentDialogueTargetRect =
            GetDialogueTargetRect(
                step,
                targetRect
            );

        if (tutorialUI != null)
        {
            tutorialUI.PrepareDialogueVisuals();

            /*
             * 先顯示第一句對話，
             * 讓 TMP 與 Layout 更新對話框尺寸。
             */
            tutorialUI.ShowDialogueLine(
                step.dialogueLines[
                    currentDialogueIndex
                ]
            );

            /*
             * 開啟對話框定位時，
             * 先在進入 Step 的當下定位一次。
             */
            if (step.positionDialoguePanel)
            {
                tutorialUI.PositionDialoguePanel(
                    currentDialogueTargetRect,
                    step.dialoguePosition,
                    step.dialogueSpacing,
                    step.dialogueScreenPadding
                );
            }

            /*
             * 以下保留你原本的對話期間高光功能。
             */
            if (step.highlightDuringDialogue &&
                step.highlightTarget &&
                targetRect != null)
            {
                tutorialUI.MoveLocatorToTarget(
                    targetRect,
                    step.highlightPadding
                );
            }
            else
            {
                tutorialUI.MoveLocatorToTarget(
                    null,
                    Vector2.zero
                );
            }
        }
    }
    private RectTransform GetDialogueTargetRect(
    TutorialStepData step,
    RectTransform fallbackTargetRect
)
    {
        if (step == null)
            return null;

        string finalTargetId =
            step.dialogueTargetId;

        /*
         * Dialogue Target Id 沒有填寫時，
         * 改用原本高光區的 Target Id。
         */
        if (string.IsNullOrWhiteSpace(
                finalTargetId))
        {
            finalTargetId =
                step.targetId;
        }

        /*
         * 兩個 Target Id 都沒有設定，
         * 這個對話就沒有定位目標。
         */
        if (string.IsNullOrWhiteSpace(
                finalTargetId))
        {
            return null;
        }

        finalTargetId =
            finalTargetId.Trim();

        string highlightTargetId =
            string.IsNullOrWhiteSpace(
                step.targetId)
                ? string.Empty
                : step.targetId.Trim();

        /*
         * 對話框和高光區使用相同目標時，
         * 直接使用 ShowCurrentStep 已找到的 targetRect。
         */
        if (string.Equals(
                finalTargetId,
                highlightTargetId,
                System.StringComparison
                    .OrdinalIgnoreCase))
        {
            return fallbackTargetRect;
        }

        /*
         * Dialogue Target Id 與高光 Target Id 不同，
         * 重新搜尋對話專用的 TutorialTarget。
         */
        TutorialTarget dialogueTarget =
            TutorialTarget.Find(
                finalTargetId
            );

        if (dialogueTarget == null)
        {
            Debug.LogWarning(
                $"[TutorialManager] " +
                $"Step「{step.stepId}」" +
                $"找不到 Dialogue Target：" +
                $"{finalTargetId}",
                step
            );

            return null;
        }

        return dialogueTarget.RectTransform;
    }
    private void StartInstructionPhase(TutorialStepData step, RectTransform targetRect)
    {
        RestoreDialogueGlobalPause();

        playbackPhase = TutorialPlaybackPhase.Instruction;

        currentDialogueIndex = -1;

        if (step.dialogueOnly)
        {
            TryAdvanceStep(true);
            return;
        }

        bool isLastStep =
            currentStepIndex >=
            currentSequence.steps.Count - 1;

        if (tutorialUI != null)
        {
            tutorialUI.PrepareWaitInteractionVisuals();
            interactionVisualsHidden = false;

            tutorialUI.HideDialoguePanel();
            tutorialUI.ShowInstructionPanel();

            tutorialUI.SetInstructionStep(
                step,
                currentStepIndex,
                currentSequence.steps.Count,
                isLastStep
            );

            if (step.highlightTarget && targetRect != null)
            {
                tutorialUI.MoveLocatorToTarget(
                    targetRect,
                    step.highlightPadding
                );
            }
            else
            {
                tutorialUI.MoveLocatorToTarget(
                    null,
                    Vector2.zero
                );
            }

            if (step.dialogPosition == TutorialDialogPosition.Center)
            {
                tutorialUI.CenterInstructionPanel();
            }
            else if (targetRect != null)
            {
                tutorialUI.PositionInstructionPanel(
                    targetRect,
                    step.dialogPosition,
                    step.dialogSpacing
                );
            }
            else
            {
                tutorialUI.ResetInstructionPosition();
            }
        }

        if (step.UsesSignal())
        {
            waitingSignalId =
                step.requiredSignal.Trim();

            Debug.Log(
                $"[TutorialManager] 等待操作：" +
                $"{waitingSignalId}，需要 " +
                $"{step.GetRequiredSignalCount()} 次"
            );
        }

        if (step.autoAdvanceAfterSeconds > 0f)
        {
            autoAdvanceCoroutine = StartCoroutine(
                AutoAdvanceRoutine(
                    step.autoAdvanceAfterSeconds,
                    currentStepIndex
                )
            );
        }
    }
    public void ContinueDialogue()
    {
        if (!isPlaying)
            return;

        if (playbackPhase == TutorialPlaybackPhase.CorrectionDialogue)
        {
            ContinueCorrectionDialogue();
            return;
        }

        if (playbackPhase !=
            TutorialPlaybackPhase.Dialogue)
        {
            return;
        }

        TutorialStepData step =
            GetCurrentStep();

        if (step == null)
            return;

        if (tutorialUI != null &&
            tutorialUI.TryCompleteDialogueTyping())
        {
            return;
        }

        currentDialogueIndex++;

        if (currentDialogueIndex <
            step.dialogueLines.Count)
        {
            tutorialUI.ShowDialogueLine(
                step.dialogueLines[
                    currentDialogueIndex
                ]
            );

            /*
             * 新一句文字可能改變 DialoguePanel 尺寸，
             * 因此重新定位並重新 Clamp。
             */
            if (step.positionDialoguePanel)
            {
                tutorialUI.PositionDialoguePanel(
                    currentDialogueTargetRect,
                    step.dialoguePosition,
                    step.dialogueSpacing,
                    step.dialogueScreenPadding
                );
            }

            return;
        }

        TutorialTarget target =
            TutorialTarget.Find(step.targetId);

        RectTransform targetRect =
            target != null
                ? target.RectTransform
                : null;

        StartInstructionPhase(
            step,
            targetRect
        );
    }

    private void ContinueCorrectionDialogue()
    {
        TutorialStepData step =
            GetCurrentStep();

        if (step == null)
            return;

        if (tutorialUI != null &&
            tutorialUI.TryCompleteDialogueTyping())
        {
            return;
        }

        currentCorrectionDialogueIndex++;

        if (step.correctionDialogueLines != null &&
            currentCorrectionDialogueIndex <
            step.correctionDialogueLines.Count)
        {
            if (tutorialUI != null)
            {
                tutorialUI.ShowDialogueLine(
                    step.correctionDialogueLines[
                        currentCorrectionDialogueIndex
                    ]
                );
            }

            return;
        }

        RestartCurrentInstructionStep();
    }



    private TutorialStepData GetCurrentStep()
    {
        if (currentSequence == null ||
            currentSequence.steps == null)
        {
            return null;
        }

        if (currentStepIndex < 0 ||
            currentStepIndex >= currentSequence.steps.Count)
        {
            return null;
        }

        return currentSequence.steps[currentStepIndex];
    }

    public void NextStep()
    {
        if (!isPlaying)
            return;

        if (playbackPhase ==
            TutorialPlaybackPhase.Dialogue)
        {
            ContinueDialogue();
            return;
        }

        if (playbackPhase !=
            TutorialPlaybackPhase.Instruction)
        {
            return;
        }

        TryAdvanceStep(false);
    }

    private bool TryAdvanceStep(
        bool conditionCompleted
    )
    {
        if (!isPlaying)
            return false;

        TutorialStepData step = GetCurrentStep();

        if (step == null)
            return false;

        if (step.advanceMode ==
                TutorialAdvanceMode.WaitForSignal &&
            step.requireConditionToAdvance &&
            !conditionCompleted)
        {
            Debug.Log(
                $"[TutorialManager] 必須先完成操作：" +
                $"{step.requiredSignal}"
            );

            return false;
        }

        currentStepIndex++;

        if (currentStepIndex >=
            currentSequence.steps.Count)
        {
            FinishTutorial();
            return true;
        }

        ShowCurrentStep();
        return true;
    }

    public void PreviousStep()
    {
        if (!isPlaying)
            return;

        TutorialStepData step = GetCurrentStep();

        if (step != null && !step.allowBack)
            return;

        if (currentStepIndex <= 0)
            return;

        currentStepIndex--;
        ShowCurrentStep();
    }

    public void SkipTutorial()
    {
        if (!isPlaying || currentSequence == null)
            return;

        bool shouldSave =
            currentSequence.skipCountsAsComplete &&
            currentSequence.saveCompletion;

        StopTutorialInternal(shouldSave);
    }

    public void FinishTutorial()
    {
        if (!isPlaying || currentSequence == null)
            return;

        StopTutorialInternal(
            currentSequence.saveCompletion
        );
    }

    public void StopTutorialWithoutSaving()
    {
        StopTutorialInternal(false);
    }

    private void StopTutorialInternal(
        bool saveCompletion
    )
    {
        RestoreDialogueGlobalPause();

        if (saveCompletion &&
            currentSequence != null)
        {
            TutorialProgress.MarkCompleted(
                currentSequence.tutorialId
            );
        }

        StopStepCoroutines();

        if (tutorialUI != null)
            tutorialUI.Hide();

        RestoreTimeScale();

        string finishedId =
            currentSequence != null
                ? currentSequence.tutorialId
                : "null";

        currentSequence = null;
        currentStepIndex = -1;
        isPlaying = false;

        Debug.Log(
            $"[TutorialManager] 教學結束：{finishedId}"
        );
    }

    private void HandleScreenClicked()
    {
        if (!isPlaying)
            return;

        if (playbackPhase !=
            TutorialPlaybackPhase.Instruction)
        {
            return;
        }

        TutorialStepData step =
            GetCurrentStep();

        if (step == null)
            return;

        if (step.advanceMode !=
            TutorialAdvanceMode.AnyScreenClick)
        {
            return;
        }

        TryAdvanceStep(true);
    }

    private void HandleSignal(string signalId)
    {
        if (!isPlaying)
            return;

        TutorialStepData step =
            GetCurrentStep();

        if (step == null)
            return;

        if (step.advanceMode !=
            TutorialAdvanceMode.WaitForSignal)
        {
            return;
        }

        if (playbackPhase !=
            TutorialPlaybackPhase.Instruction)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(
                signalId))
        {
            return;
        }

        string received =
            signalId.Trim();

        /*
         * 1. 玩家開始操作
         */
        if (step.UsesInteractionStartSignal() &&
            string.Equals(
                step.interactionStartSignal.Trim(),
                received,
                System.StringComparison
                    .OrdinalIgnoreCase))
        {
            HandleInteractionStarted(step);
            return;
        }

        /*
         * 2. 玩家操作失敗
         */
        if (step.IsIncorrectSignal(received))
        {
            HandleIncorrectInteraction(
                step,
                received
            );

            return;
        }

        /*
         * 3. 玩家操作成功
         */
        if (string.IsNullOrWhiteSpace(
                step.requiredSignal))
        {
            return;
        }

        string required =
            step.requiredSignal.Trim();

        if (!string.Equals(
                required,
                received,
                System.StringComparison
                    .OrdinalIgnoreCase))
        {
            return;
        }

        currentSignalCount++;

        int requiredCount =
            step.GetRequiredSignalCount();

        Debug.Log(
            $"[TutorialManager] 收到正確事件：" +
            $"{received}，進度 " +
            $"{currentSignalCount}/" +
            $"{requiredCount}"
        );

        if (currentSignalCount < requiredCount)
        {
            if (tutorialUI != null)
            {
                tutorialUI.SetConditionProgress(
                    currentSignalCount,
                    requiredCount
                );
            }

            return;
        }

        if (signalAdvanceCoroutine != null)
        {
            StopCoroutine(
                signalAdvanceCoroutine
            );
        }

        signalAdvanceCoroutine =
            StartCoroutine(
                SignalAdvanceRoutine(
                    step.signalAdvanceDelay,
                    currentStepIndex
                )
            );
    }

    private void HandleInteractionStarted(
    TutorialStepData step
)
    {
        if (step == null)
            return;

        if (!step.hideVisualsAfterInteractionStart)
            return;

        if (interactionVisualsHidden)
            return;

        interactionVisualsHidden = true;

        if (tutorialUI != null)
        {
            tutorialUI.HideWaitInteractionVisuals();
        }

        Debug.Log(
            $"[TutorialManager] 玩家開始操作，" +
            $"暫時隱藏教學 UI：" +
            $"{step.interactionStartSignal}"
        );
    }
    private void HandleIncorrectInteraction(
    TutorialStepData step,
    string receivedSignal
)
    {
        if (step == null)
            return;

        Debug.Log(
            $"[TutorialManager] 收到錯誤操作：" +
            $"{receivedSignal}"
        );

        StopStepCoroutines();

        currentSignalCount = 0;
        interactionVisualsHidden = false;

        if (!step.HasCorrectionDialogue())
        {
            RestartCurrentInstructionStep();
            return;
        }

        if (step.pauseGameDuringDialogue)
        {
            ApplyDialogueGlobalPause();
        }
        else
        {
            RestoreDialogueGlobalPause();
        }

        playbackPhase =
            TutorialPlaybackPhase.CorrectionDialogue;

        currentCorrectionDialogueIndex = 0;

        if (tutorialUI != null)
        {
            tutorialUI.ShowCorrectionDialogueVisuals();

            tutorialUI.ShowDialogueLine(
                step.correctionDialogueLines[
                    currentCorrectionDialogueIndex
                ]
            );
        }
    }
    private void RestartCurrentInstructionStep()
    {


        TutorialStepData step =
            GetCurrentStep();

        if (step == null)
            return;

        RestoreDialogueGlobalPause();

        currentSignalCount = 0;
        currentDialogueIndex = -1;
        currentCorrectionDialogueIndex = -1;

        interactionVisualsHidden = false;

        TutorialTarget target =
            TutorialTarget.Find(step.targetId);

        RectTransform targetRect =
            target != null
                ? target.RectTransform
                : null;

        StartInstructionPhase(
            step,
            targetRect
        );

        Debug.Log(
            $"[TutorialManager] 回到目前操作步驟：" +
            $"{step.stepId}"
        );
    }
    private IEnumerator SignalAdvanceRoutine(
        float delay,
        int expectedStepIndex
    )
    {
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                delay
            );
        }

        if (!isPlaying)
            yield break;

        if (currentStepIndex != expectedStepIndex)
            yield break;

        TryAdvanceStep(true);
    }

    private IEnumerator AutoAdvanceRoutine(
        float delay,
        int expectedStepIndex
    )
    {
        yield return new WaitForSecondsRealtime(
            delay
        );

        if (!isPlaying)
            yield break;

        if (currentStepIndex != expectedStepIndex)
            yield break;

        TutorialStepData step = GetCurrentStep();

        if (step == null)
            yield break;

        if (step.advanceMode ==
                TutorialAdvanceMode.WaitForSignal &&
            step.requireConditionToAdvance)
        {
            yield break;
        }


        TryAdvanceStep(true);
    }

    private void ApplyPauseState()
    {
        if (currentSequence == null ||
            !currentSequence.pauseGame)
        {
            return;
        }

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
    }

    private void RestoreTimeScale()
    {
        if (currentSequence == null ||
            !currentSequence.pauseGame)
        {
            return;
        }

        Time.timeScale = previousTimeScale;
    }

    private void StopStepCoroutines()
    {
        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
            autoAdvanceCoroutine = null;
        }

        if (signalAdvanceCoroutine != null)
        {
            StopCoroutine(signalAdvanceCoroutine);
            signalAdvanceCoroutine = null;
        }
    }
    private void ApplyDialogueGlobalPause()
    {
        if (dialoguePauseApplied)
            return;

        dialoguePauseApplied = true;

        timeScaleBeforeDialogue =
            Time.timeScale;

        Time.timeScale = 0f;

        Debug.Log(
            "[TutorialManager] 對話框顯示，全局暫停"
        );
    }
    private void RestoreDialogueGlobalPause()
    {
        if (!dialoguePauseApplied)
            return;

        dialoguePauseApplied = false;

        Time.timeScale =
            timeScaleBeforeDialogue;

        Debug.Log(
            "[TutorialManager] 對話框結束，恢復時間"
        );
    }
}