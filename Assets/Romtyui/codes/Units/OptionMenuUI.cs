using UnityEngine;
using UnityEngine.UI;

public class OptionMenuUI : MonoBehaviour
{
    [Header("Refs")]
    public GameObject panelRoot;
    public BattleManager battleManager;

    [Header("Buttons")]
    public Button continueButton;
    public Button restartButton;
    public Button quitButton;

    [Header("Pause")]
    public bool pauseGameWhenOpen = false;

    private void Awake()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueGame);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        Close();
    }

    private void Update()
    {
        // 測試用：按 ESC 開關選項 UI
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (panelRoot != null && panelRoot.activeSelf)
                ContinueGame();
            else
                OpenPauseMenu();
        }
    }

    public void OpenPauseMenu()
    {
        Open();

        if (continueButton != null)
            continueButton.gameObject.SetActive(true);
    }

    public void OpenDeathMenu()
    {
        Open();

        // 死亡後不能繼續，只能重開或離開
        if (continueButton != null)
            continueButton.gameObject.SetActive(false);
    }
    public void OpenOptionMenu()
    {
        OpenPauseMenu();
    }
    public void Open()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (pauseGameWhenOpen)
            Time.timeScale = 0f;
    }

    public void ContinueGame()
    {
        Close();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        Close();

        if (battleManager != null)
            battleManager.RestartNewGame();
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (pauseGameWhenOpen)
            Time.timeScale = 1f;
    }
}