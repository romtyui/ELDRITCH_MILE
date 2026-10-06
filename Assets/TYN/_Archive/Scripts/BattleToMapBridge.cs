using UnityEngine;

// ⚠️ 這一批是封存的舊碼，包進 namespace 是為了**不要污染全域命名空間**。
//    它們宣告的 RunNodeData / MapData 與 EldritchMile.Core 那組同名，
//    放在全域的話會蓋掉正式那組 —— 症狀是新檔案裡「MapData 沒有 GetNode」
//    這種看起來莫名其妙的錯誤，而且完全不會指向這裡。
//    （MapView.cs 第 9 行早就記過這個坑）
namespace EldritchMile.Archive
{

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
}
