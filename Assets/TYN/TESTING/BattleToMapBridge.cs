using UnityEngine;

public class BattleToMapBridge : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("拖入同學場景中的 BattleManager")]
    public BattleManager battleManager;

    private bool wasBattleManagerActive;

    private void Start()
    {
        if (battleManager == null)
        {
            // 修正警告：使用 FindAnyObjectByType 替代舊版的寫法
            battleManager = FindAnyObjectByType<BattleManager>(FindObjectsInactive.Include);
        }
        
        if (battleManager != null)
        {
            wasBattleManagerActive = battleManager.gameObject.activeSelf;
            
            // 強制關閉同學腳本的「自動開始下一場」，這樣打贏後它才會呼叫 SetActive(false)
            battleManager.autoStartNextBattleOnWin = false; 
        }
        else
        {
            Debug.LogWarning("[BattleToMapBridge] 找不到 BattleManager！");
        }
    }

    private void Update()
    {
        if (battleManager == null) return;

        bool isCurrentlyActive = battleManager.gameObject.activeSelf;

        // 當 BattleManager 從開啟變成關閉時觸發 (戰鬥結束)
        if (wasBattleManagerActive && !isCurrentlyActive)
        {
            ReturnToMap();
        }

        wasBattleManagerActive = isCurrentlyActive;
    }

    private void ReturnToMap()
    {
        Debug.Log("[BattleToMapBridge] 戰鬥結束，準備卸載戰鬥場景並返回大地圖...");
        
        if (PerspectiveMapGenerator.Instance != null)
        {
            // 呼叫地圖總管醒來，並把目前所在的戰鬥場景卸載
            PerspectiveMapGenerator.Instance.WakeUpMapAndUnload();
        }
        else
        {
            Debug.LogWarning("[BattleToMapBridge] 找不到 PerspectiveMapGenerator 大地圖總管！");
        }
    }
}