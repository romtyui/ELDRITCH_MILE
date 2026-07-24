using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "TutorialSequence",
    menuName = "Tutorial/Tutorial Sequence"
)]
public class TutorialSequenceData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("每一套教學都要不同 ID")]
    public string tutorialId = "Tutorial_Default";

    public string displayName = "新手教學";

    [Header("Steps")]
    public List<TutorialStepData> steps = new();

    [Header("Play Rules")]
    public bool onlyShowOnce = true;

    [Tooltip("戰鬥教學建議 false，避免拖牌、動畫被暫停")]
    public bool pauseGame = false;

    [Tooltip("是否讓教學 UI 擋住其他 UI")]
    public bool blockOtherUI = true;

    public bool saveCompletion = true;
    public bool skipCountsAsComplete = true;

    [Header("Start")]
    public int startingStepIndex = 0;

    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(tutorialId))
            return false;

        if (steps == null || steps.Count == 0)
            return false;

        return true;
    }
}