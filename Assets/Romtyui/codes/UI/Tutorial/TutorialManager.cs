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

    private float previousTimeScale = 1f;
    private Coroutine autoAdvanceCoroutine;
    private Coroutine signalAdvanceCoroutine;

    [Header("Signal Progress")]
    [SerializeField] private int currentSignalCount;
    [SerializeField] private string waitingSignalId;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (tutorialUI == null)
            tutorialUI = FindFirstObjectByType<TutorialUI>(FindObjectsInactive.Include);

        BindUIEvents();
    }

    private void OnEnable()
    {
        TutorialEventBus.OnSignalRaised += HandleSignal;
    }

    private void OnDisable()
    {
        TutorialEventBus.OnSignalRaised -= HandleSignal;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        UnbindUIEvents();
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
    }

    private void UnbindUIEvents()
    {
        if (tutorialUI == null)
            return;

        tutorialUI.NextClicked -= NextStep;
        tutorialUI.BackClicked -= PreviousStep;
        tutorialUI.SkipClicked -= SkipTutorial;
        tutorialUI.ScreenClicked -= HandleScreenClicked;
    }

    public bool TryPlay(TutorialSequenceData sequence)
    {
        if (sequence == null)
        {
            Debug.LogWarning("[TutorialManager] sequence 是 null");
            return false;
        }

        if (!sequence.IsValid())
        {
            Debug.LogWarning($"[TutorialManager] 教學資料不完整：{sequence.name}", sequence);
            return false;
        }

        if (sequence.onlyShowOnce && TutorialProgress.IsCompleted(sequence.tutorialId))
        {
            Debug.Log($"[TutorialManager] 教學已完成，不播放：{sequence.tutorialId}");
            return false;
        }

        Play(sequence, sequence.startingStepIndex);
        return true;
    }

    public void Play(TutorialSequenceData sequence, int startingIndex = 0)
    {
        if (sequence == null || !sequence.IsValid())
            return;

        if (isPlaying)
            StopTutorialWithoutSaving();

        currentSequence = sequence;
        currentStepIndex = Mathf.Clamp(startingIndex, 0, sequence.steps.Count - 1);
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

        TutorialStepData step = GetCurrentStep();

        if (step == null)
        {
            FinishTutorial();
            return;
        }

        bool isLastStep =
            currentStepIndex >= currentSequence.steps.Count - 1;

        if (tutorialUI != null)
        {
            tutorialUI.SetStep(
                step,
                currentStepIndex,
                currentSequence.steps.Count,
                isLastStep
            );
        }

        TutorialTarget target = TutorialTarget.Find(step.targetId);

        RectTransform targetRect =
            target != null ? target.RectTransform : null;

        if (tutorialUI != null)
        {
            if (step.highlightTarget && targetRect != null)
            {
                tutorialUI.MoveLocatorToTarget(targetRect,step.highlightPadding);

                tutorialUI.PositionDialog(targetRect,step.dialogPosition,step.dialogSpacing);
            }
            else
            {
                tutorialUI.MoveLocatorToTarget(null,Vector2.zero);
            }
        }

        if (step.UsesSignal())
        {
            waitingSignalId = step.requiredSignal.Trim();

            Debug.Log(
                $"[TutorialManager] 等待操作：" +
                $"{waitingSignalId}，" +
                $"需要 {step.GetRequiredSignalCount()} 次"
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
    private TutorialStepData GetCurrentStep()
    {
        if (currentSequence == null || currentSequence.steps == null)
            return null;

        if (currentStepIndex < 0 || currentStepIndex >= currentSequence.steps.Count)
            return null;

        return currentSequence.steps[currentStepIndex];
    }

    public void NextStep()
    {
        TryAdvanceStep(false);
    }
    private bool TryAdvanceStep(bool conditionCompleted)
    {
        if (!isPlaying)
            return false;

        TutorialStepData step = GetCurrentStep();

        if (step == null)
            return false;

        if (step.advanceMode == TutorialAdvanceMode.WaitForSignal &&
            step.requireConditionToAdvance &&
            !conditionCompleted)
        {
            Debug.Log(
                $"[TutorialManager] 這一步必須完成指定操作：" +
                $"{step.requiredSignal}"
            );

            return false;
        }

        currentStepIndex++;

        if (currentStepIndex >= currentSequence.steps.Count)
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

        StopTutorialInternal(currentSequence.saveCompletion);
    }

    public void StopTutorialWithoutSaving()
    {
        StopTutorialInternal(false);
    }

    private void StopTutorialInternal(bool saveCompletion)
    {
        if (saveCompletion && currentSequence != null)
            TutorialProgress.MarkCompleted(currentSequence.tutorialId);

        StopStepCoroutines();

        if (tutorialUI != null)
            tutorialUI.Hide();

        RestoreTimeScale();

        string finishedId = currentSequence != null ? currentSequence.tutorialId : "null";

        currentSequence = null;
        currentStepIndex = -1;
        isPlaying = false;

        Debug.Log($"[TutorialManager] 教學結束：{finishedId}");
    }

    private void HandleScreenClicked()
    {
        TutorialStepData step = GetCurrentStep();

        if (step == null)
            return;

        if (step.advanceMode != TutorialAdvanceMode.AnyScreenClick)
            return;

        TryAdvanceStep(true);
    }

    private void HandleSignal(string signalId)
    {
        if (!isPlaying)
            return;

        TutorialStepData step = GetCurrentStep();

        if (step == null)
            return;

        if (step.advanceMode != TutorialAdvanceMode.WaitForSignal)
            return;

        if (string.IsNullOrWhiteSpace(step.requiredSignal))
            return;

        if (string.IsNullOrWhiteSpace(signalId))
            return;

        string required = step.requiredSignal.Trim();
        string received = signalId.Trim();

        if (!string.Equals(
            required,
            received,
            System.StringComparison.OrdinalIgnoreCase
        ))
        {
            return;
        }

        currentSignalCount++;

        int requiredCount = step.GetRequiredSignalCount();

        Debug.Log(
            $"[TutorialManager] 收到教學事件：" +
            $"{received}，進度 " +
            $"{currentSignalCount}/{requiredCount}"
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
            StopCoroutine(signalAdvanceCoroutine);

        signalAdvanceCoroutine = StartCoroutine(SignalAdvanceRoutine(step.signalAdvanceDelay,currentStepIndex));
    }

    private IEnumerator SignalAdvanceRoutine(float delay,int expectedStepIndex)
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        if (!isPlaying)
            yield break;

        if (currentStepIndex != expectedStepIndex)
            yield break;

        TryAdvanceStep(true);
    }

    private IEnumerator AutoAdvanceRoutine(float delay,int expectedStepIndex)
    {
        yield return new WaitForSecondsRealtime(delay);

        if (!isPlaying)
            yield break;

        if (currentStepIndex != expectedStepIndex)
            yield break;

        TutorialStepData step = GetCurrentStep();

        if (step == null)
            yield break;

        if (step.advanceMode == TutorialAdvanceMode.WaitForSignal &&
            step.requireConditionToAdvance)
        {
            yield break;
        }

        TryAdvanceStep(true);
    }

    private void ApplyPauseState()
    {
        if (currentSequence == null || !currentSequence.pauseGame)
            return;

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
    }

    private void RestoreTimeScale()
    {
        if (currentSequence == null || !currentSequence.pauseGame)
            return;

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
}