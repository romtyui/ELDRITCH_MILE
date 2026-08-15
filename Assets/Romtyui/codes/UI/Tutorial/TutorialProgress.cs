using UnityEngine;

public static class TutorialProgress
{
    private const string Prefix = "Tutorial_Completed_";

    public static bool IsCompleted(string tutorialId)
    {
        if (string.IsNullOrWhiteSpace(tutorialId))
            return false;

        return PlayerPrefs.GetInt(BuildKey(tutorialId), 0) == 1;
    }

    public static void MarkCompleted(string tutorialId)
    {
        if (string.IsNullOrWhiteSpace(tutorialId))
            return;

        PlayerPrefs.SetInt(BuildKey(tutorialId), 1);
        PlayerPrefs.Save();

        Debug.Log($"[TutorialProgress] 完成教學：{tutorialId}");
    }

    public static void ResetTutorial(string tutorialId)
    {
        if (string.IsNullOrWhiteSpace(tutorialId))
            return;

        PlayerPrefs.DeleteKey(BuildKey(tutorialId));
        PlayerPrefs.Save();

        Debug.Log($"[TutorialProgress] 重置教學：{tutorialId}");
    }

    private static string BuildKey(string tutorialId)
    {
        return Prefix + tutorialId.Trim();
    }
}