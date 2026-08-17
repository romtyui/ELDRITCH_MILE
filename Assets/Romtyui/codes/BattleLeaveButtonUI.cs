using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleLeaveButtonUI : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("離開戰鬥按鈕")]
    public Button leaveButton;

    [Header("Scene")]
    [Tooltip("離開戰鬥後要回到的場景名稱，例如場景A")]
    public string returnSceneName = "SceneA";

    [Header("Behavior")]
    [Tooltip("是否把這次離開視為中途離開。勾選後，下次進戰鬥會還原到這場戰鬥開始狀態")]
    public bool restoreBattleStartNextTime = true;

    [Tooltip("離開時是否解除暫停")]
    public bool resetTimeScale = true;

    private void Awake()
    {
        if (leaveButton == null)
            leaveButton = GetComponent<Button>();

        if (leaveButton != null)
            leaveButton.onClick.AddListener(OnClickLeaveBattle);
        else
            Debug.LogWarning("[BattleLeaveButtonUI] 找不到 Button，請指定 leaveButton 或把腳本掛在 Button 上", gameObject);
    }

    private void OnDestroy()
    {
        if (leaveButton != null)
            leaveButton.onClick.RemoveListener(OnClickLeaveBattle);
    }

    public void OnClickLeaveBattle()
    {
        if (restoreBattleStartNextTime)
        {
            if (RunStateManager.Instance != null)
            {
                RunStateManager.Instance.pendingRestoreBattleStartDeckSnapshot = true;

                Debug.Log("[BattleLeaveButtonUI] 中途離開戰鬥：下次進入戰鬥時會還原戰鬥開始狀態");
            }
            else
            {
                Debug.LogWarning("[BattleLeaveButtonUI] 找不到 RunStateManager，無法設定下次還原戰鬥開始狀態");
            }
        }

        if (resetTimeScale)
            Time.timeScale = 1f;

        if (string.IsNullOrWhiteSpace(returnSceneName))
        {
            Debug.LogWarning("[BattleLeaveButtonUI] returnSceneName 是空的，無法切換場景");
            return;
        }

        Debug.Log($"[BattleLeaveButtonUI] 離開戰鬥，載入場景：{returnSceneName}");

        SceneManager.LoadScene(returnSceneName);
    }
}