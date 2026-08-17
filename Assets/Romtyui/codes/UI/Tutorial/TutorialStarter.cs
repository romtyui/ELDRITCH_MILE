using System.Collections;
using UnityEngine;

public class TutorialStarter : MonoBehaviour
{
    [Header("Tutorial")]
    public TutorialSequenceData sequence;

    [Header("Battle")]
    [SerializeField]
    private BattleManager battleManager;

    [Header("Start")]
    public bool playOnStart = true;
    public int waitFrames = 1;
    public float delaySeconds = 0f;
    /*
     * 避免每次玩家回合抽牌完成後，
     * 都重新啟動同一個教學。
     */
    private bool hasRequestedTutorial;

    private Coroutine delayedPlayCoroutine;

    //private IEnumerator Start()
    //{
    //    if (!playOnStart)
    //        yield break;

    //    for (int i = 0; i < waitFrames; i++)
    //        yield return null;

    //    if (delaySeconds > 0f)
    //        yield return new WaitForSecondsRealtime(delaySeconds);

    //    PlayTutorial();
    //}
    private void Awake()
    {
        if (battleManager == null)
        {
            battleManager =
                FindFirstObjectByType<BattleManager>();
        }
    }
    [ContextMenu("Play Tutorial")]
    public void PlayTutorial()
    {
        if (sequence == null)
        {
            Debug.LogWarning("[TutorialStarter] sequence 沒有指定", this);
            return;
        }

        if (TutorialManager.Instance == null)
        {
            Debug.LogWarning("[TutorialStarter] 場景中找不到 TutorialManager", this);
            return;
        }

        TutorialManager.Instance.TryPlay(sequence);
    }
    private void OnEnable()
    {
        TryBindBattleManager();
    }
    private void Start()
    {
        /*
         * OnEnable 執行時，BattleManager 可能還沒準備好。
         * Start 再補綁一次。
         */
        TryBindBattleManager();
    }
    private void OnDisable()
    {
        if (battleManager != null)
        {
            battleManager.PlayerInputReady -=
                HandlePlayerInputReady;
        }

        if (delayedPlayCoroutine != null)
        {
            StopCoroutine(
                delayedPlayCoroutine
            );

            delayedPlayCoroutine = null;
        }
    }
    private void TryBindBattleManager()
    {
        if (battleManager == null)
        {
            battleManager =
                FindFirstObjectByType<BattleManager>();
        }

        if (battleManager == null)
        {
            Debug.LogWarning(
                "[TutorialStarter] 場景中找不到 BattleManager",
                this
            );

            return;
        }

        /*
         * 先移除再加入，避免重複訂閱。
         */
        battleManager.PlayerInputReady -=
            HandlePlayerInputReady;

        battleManager.PlayerInputReady +=
            HandlePlayerInputReady;
    }
    private void HandlePlayerInputReady()
    {
        /*
         * 保留原本 Play On Start 的控制。
         */
        if (!playOnStart)
            return;

        /*
         * PlayerInputReady 每個玩家回合都可能觸發，
         * 但這個 Starter 只自動請求一次。
         */
        if (hasRequestedTutorial)
            return;

        hasRequestedTutorial = true;

        if (delayedPlayCoroutine != null)
        {
            StopCoroutine(
                delayedPlayCoroutine
            );
        }

        delayedPlayCoroutine =
            StartCoroutine(
                PlayAfterPlayerReadyRoutine()
            );
    }
    private IEnumerator PlayAfterPlayerReadyRoutine()
    {
        /*
         * 保留原本 Wait Frames 功能。
         * 但現在改成抽牌完成後才開始等待。
         */
        int finalWaitFrames =
            Mathf.Max(0, waitFrames);

        for (int i = 0;
             i < finalWaitFrames;
             i++)
        {
            yield return null;
        }

        /*
         * 保留原本 Delay Seconds 功能。
         * 使用 Realtime，不受 Time.timeScale 影響。
         */
        if (delaySeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(
                delaySeconds
            );
        }

        delayedPlayCoroutine = null;

        PlayTutorial();
    }
    [ContextMenu("Force Replay Tutorial")]
    public void ForceReplayTutorial()
    {
        if (sequence == null)
            return;

        TutorialProgress.ResetTutorial(sequence.tutorialId);

        if (TutorialManager.Instance != null)
            TutorialManager.Instance.Play(sequence);
    }

    [ContextMenu("Reset Tutorial Progress")]
    public void ResetTutorialProgress()
    {
        if (sequence == null)
            return;

        TutorialProgress.ResetTutorial(sequence.tutorialId);
    }
}