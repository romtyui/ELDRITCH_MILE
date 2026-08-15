using UnityEngine;

public class BattleManagerEnableWatcher : MonoBehaviour
{
    [Header("References")]
    public GameObject battleManagerObject;
    public BattleManager battleManager;

    [Header("Debug View")]
    [SerializeField] private bool previousActive;
    [SerializeField] private bool currentActive;

    private void Start()
    {
        AutoFindBattleManager();

        previousActive = IsBattleManagerActive();
        currentActive = previousActive;
    }

    private void Update()
    {
        if (battleManagerObject == null)
            return;

        currentActive = IsBattleManagerActive();

        bool changedFromOffToOn = !previousActive && currentActive;

        if (changedFromOffToOn)
        {
            OnBattleManagerEnabled();
        }

        previousActive = currentActive;
    }

    private bool IsBattleManagerActive()
    {
        if (battleManagerObject == null)
            return false;

        return battleManagerObject.activeInHierarchy;
    }

    private void OnBattleManagerEnabled()
    {
        AutoFindBattleManager();

        if (battleManager == null)
        {
            Debug.LogWarning("[BattleManagerEnableWatcher] BattleManager 沒有指定");
            return;
        }

        if (battleManager.autoStartNextBattleOnWin)
        {
            Debug.Log("[BattleManagerEnableWatcher] autoStartNextBattleOnWin = true，不使用外部 Enable 重開戰鬥");
            return;
        }

        Debug.Log("[BattleManagerEnableWatcher] 偵測到 BattleManager 從關閉變開啟，開始下一場戰鬥");

        battleManager.StartNextBattleKeepHpSanAndDeck();
    }

    private void AutoFindBattleManager()
    {
        if (battleManager == null && battleManagerObject != null)
            battleManager = battleManagerObject.GetComponent<BattleManager>();

        if (battleManagerObject == null && battleManager != null)
            battleManagerObject = battleManager.gameObject;
    }
}