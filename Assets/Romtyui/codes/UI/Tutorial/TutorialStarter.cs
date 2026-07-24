using System.Collections;
using UnityEngine;

public class TutorialStarter : MonoBehaviour
{
    [Header("Tutorial")]
    public TutorialSequenceData sequence;

    [Header("Start")]
    public bool playOnStart = true;
    public int waitFrames = 1;
    public float delaySeconds = 0f;

    private IEnumerator Start()
    {
        if (!playOnStart)
            yield break;

        for (int i = 0; i < waitFrames; i++)
            yield return null;

        if (delaySeconds > 0f)
            yield return new WaitForSecondsRealtime(delaySeconds);

        PlayTutorial();
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