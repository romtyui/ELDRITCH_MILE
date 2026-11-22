using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MainMenuUI : MonoBehaviour
{
    [Header("按鈕")]
    [SerializeField, Tooltip("繼續遊戲按鈕。沒有可繼續的存檔時隱藏。")]
    private Button continueButton;

    [SerializeField, Tooltip("開始遊戲按鈕。")]
    private Button startButton;

    [SerializeField, Tooltip("開啟設定選單的按鈕。")]
    private Button settingButton;

    [SerializeField, Tooltip("離開遊戲的按鈕。")]
    private Button exitButton;

    [Header("設定選單")]
    [SerializeField, Tooltip("設定選單根物件。請先在 Hierarchy 關閉，點擊 Setting 時開啟。")]
    private GameObject settingsRoot;

    [Header("存檔功能")]
    [SerializeField, Tooltip("目前保持關閉，Start 直接進入大廳。未來存檔流程完成後才開啟。")]
    private bool saveFlowEnabled;

    [Header("未來存檔流程事件")]
    [SerializeField, Tooltip("啟用存檔流程後，點擊 Start 時觸發。由外部系統處理建立、選擇與載入存檔。")]
    private UnityEvent onStartSaveFlowRequested = new UnityEvent();

    [SerializeField, Tooltip("點擊 Continue 時觸發。由外部系統載入上次離開時使用的存檔。")]
    private UnityEvent onContinueSaveRequested = new UnityEvent();

    [Header("進入遊戲")]
    [SerializeField, Tooltip("可以進入遊戲時觸發。測試時可綁定開啟大廳、關閉開始畫面；正式整合時由同事改綁流程入口。")]
    private UnityEvent onEnterHallRequested = new UnityEvent();

    private bool hasContinueSave;
    private bool waitingForSaveFlow;
    private bool hasEnteredGame;

    private void Awake()
    {
        if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
        if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
        if (settingButton != null) settingButton.onClick.AddListener(OnSettingClicked);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);
    }

    private void OnEnable()
    {
        waitingForSaveFlow = false;
        hasEnteredGame = false;
        RefreshButtons();
    }

    private void OnDestroy()
    {
        if (continueButton != null) continueButton.onClick.RemoveListener(OnContinueClicked);
        if (startButton != null) startButton.onClick.RemoveListener(OnStartClicked);
        if (settingButton != null) settingButton.onClick.RemoveListener(OnSettingClicked);
        if (exitButton != null) exitButton.onClick.RemoveListener(OnExitClicked);
    }

    public void OnStartClicked()
    {
        if (!isActiveAndEnabled || waitingForSaveFlow || hasEnteredGame) return;

        if (saveFlowEnabled)
        {
            waitingForSaveFlow = true;
            RefreshButtons();
            onStartSaveFlowRequested.Invoke();
            return;
        }

        CompleteGameEntry();
    }

    public void OnContinueClicked()
    {
        if (!isActiveAndEnabled || waitingForSaveFlow || hasEnteredGame) return;
        if (!saveFlowEnabled || !hasContinueSave) return;

        waitingForSaveFlow = true;
        RefreshButtons();
        onContinueSaveRequested.Invoke();
    }

    public void OnSettingClicked()
    {
        if (!isActiveAndEnabled || waitingForSaveFlow || hasEnteredGame) return;

        if (settingsRoot == null)
        {
            Debug.LogWarning("MainMenuUI：請指定設定選單根物件。", this);
            return;
        }

        settingsRoot.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsRoot == null) return;
        settingsRoot.SetActive(false);
    }

    public void OnExitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void SetContinueAvailable(bool available)
    {
        hasContinueSave = available;
        RefreshButtons();
    }

    public void CompleteGameEntry()
    {
        if (!isActiveAndEnabled || hasEnteredGame) return;

        hasEnteredGame = true;
        waitingForSaveFlow = false;

        CloseSettings();
        RefreshButtons();
        onEnterHallRequested.Invoke();
    }

    public void CancelSaveFlow()
    {
        if (hasEnteredGame) return;

        waitingForSaveFlow = false;
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        bool canInteract = !waitingForSaveFlow && !hasEnteredGame;

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(saveFlowEnabled && hasContinueSave);
            continueButton.interactable = canInteract;
        }

        if (startButton != null) startButton.interactable = canInteract;
        if (settingButton != null) settingButton.interactable = canInteract;
    }
}