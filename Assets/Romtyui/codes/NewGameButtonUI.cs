using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NewGameButtonUI : MonoBehaviour
{
    [Header("Refs")]
    public Button newGameButton;

    [Header("Scene")]
    [Tooltip("新遊戲開始後要進入的場景，例如場景A或戰鬥場景")]
    public string startSceneName = "SceneA";

    [Header("Behavior")]
    public bool resetTimeScale = true;

    private void Awake()
    {
        if (newGameButton == null)
            newGameButton = GetComponent<Button>();

        if (newGameButton != null)
            newGameButton.onClick.AddListener(OnClickNewGame);
        else
            Debug.LogWarning("[NewGameButtonUI] 找不到 Button", gameObject);
    }

    private void OnDestroy()
    {
        if (newGameButton != null)
            newGameButton.onClick.RemoveListener(OnClickNewGame);
    }

    public void OnClickNewGame()
    {
        if (RunStateManager.Instance != null)
        {
            RunStateManager.Instance.ClearAllRunData();
        }
        else
        {
            Debug.LogWarning("[NewGameButtonUI] 找不到 RunStateManager，無法清除紀錄");
        }

        if (resetTimeScale)
            Time.timeScale = 1f;

        if (string.IsNullOrWhiteSpace(startSceneName))
        {
            Debug.LogWarning("[NewGameButtonUI] startSceneName 是空的，無法載入新遊戲場景");
            return;
        }

        Debug.Log($"[NewGameButtonUI] 開始新遊戲，載入場景：{startSceneName}");

        SceneManager.LoadScene(startSceneName);
    }
}