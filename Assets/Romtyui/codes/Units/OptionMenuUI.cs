using UnityEngine;
using UnityEngine.UI;

public class OptionMenuUI : MonoBehaviour
{
    [Header("Refs")]
    public GameObject panelRoot;
    public BattleManager battleManager;

    [Header("Pages")]
    [Tooltip("主選單按鈕區，例如放 繼續 / 重新開始 / 設定 / 離開")]
    public GameObject mainPageRoot;

    [Tooltip("設定頁，例如放音量 Slider")]
    public GameObject settingsPageRoot;

    [Header("Buttons")]
    public Button continueButton;
    public Button restartButton;
    public Button settingsButton;
    public Button quitButton;
    public Button backFromSettingsButton;

    [Header("Button Visibility")]
    public bool showContinueButton = true;
    public bool showRestartButton = true;
    public bool showSettingsButton = true;
    public bool showQuitButton = true;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;

    public Slider masterVolumeSlider;

    [Tooltip("如果有指定 AudioSource，會同步調整這些 AudioSource 的 volume")]
    public AudioSource[] controlledAudioSources;

    [Header("Pause")]
    public bool pauseGameWhenOpen = false;

    private bool isDeathMenu;

    private void Awake()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueGame);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettingsPage);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        if (backFromSettingsButton != null)
            backFromSettingsButton.onClick.AddListener(OpenMainPage);

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.value = masterVolume;
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        ApplyVolume();
        Close();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (panelRoot != null && panelRoot.activeSelf)
            {
                if (settingsPageRoot != null && settingsPageRoot.activeSelf)
                    OpenMainPage();
                else
                    ContinueGame();
            }
            else
            {
                OpenPauseMenu();
            }
        }
    }

    public void OpenPauseMenu()
    {
        isDeathMenu = false;

        Open();

        RefreshButtonVisibility();
        OpenMainPage();
    }

    public void OpenDeathMenu()
    {
        isDeathMenu = true;

        Open();

        RefreshButtonVisibility();
        OpenMainPage();
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

        RefreshButtonVisibility();
        OpenMainPage();
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

        if (settingsPageRoot != null)
            settingsPageRoot.SetActive(false);

        if (mainPageRoot != null)
            mainPageRoot.SetActive(true);

        if (pauseGameWhenOpen)
            Time.timeScale = 1f;
    }

    public void OpenMainPage()
    {
        if (mainPageRoot != null)
            mainPageRoot.SetActive(true);

        if (settingsPageRoot != null)
            settingsPageRoot.SetActive(false);

        RefreshButtonVisibility();
    }

    public void OpenSettingsPage()
    {
        if (mainPageRoot != null)
            mainPageRoot.SetActive(false);

        if (settingsPageRoot != null)
            settingsPageRoot.SetActive(true);

        if (masterVolumeSlider != null)
            masterVolumeSlider.value = masterVolume;
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);

        ApplyVolume();

        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.Save();
    }

    private void ApplyVolume()
    {
        AudioListener.volume = masterVolume;

        if (controlledAudioSources == null)
            return;

        for (int i = 0; i < controlledAudioSources.Length; i++)
        {
            AudioSource source = controlledAudioSources[i];

            if (source == null)
                continue;

            source.volume = masterVolume;
        }
    }

    private void RefreshButtonVisibility()
    {
        if (continueButton != null)
        {
            bool visible = showContinueButton && !isDeathMenu;
            continueButton.gameObject.SetActive(visible);
        }

        if (restartButton != null)
            restartButton.gameObject.SetActive(showRestartButton);

        if (settingsButton != null)
            settingsButton.gameObject.SetActive(showSettingsButton);

        if (quitButton != null)
            quitButton.gameObject.SetActive(showQuitButton);
    }
}