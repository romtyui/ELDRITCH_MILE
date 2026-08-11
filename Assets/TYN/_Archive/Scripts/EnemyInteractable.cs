using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// 1. 定義一個介面，讓未來的寶箱、NPC 也能通用卡牌互動邏輯
// 【修改】將回傳值改為 bool，讓卡牌系統知道檢定是否成功
public interface ICardInteractable
{
    bool OnCardPlayed(float baseProbability);
}

public class EnemyInteractable : MonoBehaviour, IPointerClickHandler, ICardInteractable
{
    [Header("戰鬥設定")]
    [Tooltip("要載入的戰鬥場景名稱")]
    public string battleSceneName = "BattleScene";

    [Header("機率與檢定設定")]
    [Tooltip("每次檢定失敗，後續的成功率會扣除多少？ (例如 0.2 代表扣 20%)")]
    public float penaltyPerFail = 0.2f;
    [Tooltip("容許失敗的次數，超過此數值將強制開戰")]
    public int maxFailsAllowed = 1;

    private int currentFails = 0;
    private bool hasTriggered = false;

    // --- 給卡牌系統拖曳完成時呼叫的方法 ---
    public bool OnCardPlayed(float baseProbability)
    {
        if (hasTriggered) return false;

        // 1. 計算最終機率 (基礎機率 - 失敗懲罰)
        float currentPenalty = currentFails * penaltyPerFail;
        float finalProbability = Mathf.Clamp01(baseProbability - currentPenalty);

        Debug.Log($"[機率檢定] 基礎機率: {baseProbability*100}% | 當前懲罰: -{currentPenalty*100}% | 最終機率: {finalProbability*100}%");

        // 2. 擲骰子判定 (0.0 ~ 1.0)
        float roll = Random.value;

        if (roll <= finalProbability)
        {
            // --- 判定成功 ---
            Debug.Log("<color=green>檢定成功！</color> 成功欺瞞/暗殺敵人！");
            HandleSuccess();
            return true; // 回傳成功，讓 CardExplorationManager 執行後續效果
        }
        else
        {
            // --- 判定失敗 ---
            currentFails++;
            Debug.Log($"<color=red>檢定失敗！</color> 已失敗次數: {currentFails}/{maxFailsAllowed}");

            // 通知玩家 UI
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowPopupText($"檢定失敗！\n敵人起疑心了...\n下次成功率 -{penaltyPerFail*100}%");
            }

            // 檢查是否超過容忍次數，強制開戰
            if (currentFails > maxFailsAllowed)
            {
                if (UIManager.Instance != null) UIManager.Instance.ShowPopupText("敵人發現你了！強制進入戰鬥！");
                TriggerBattle();
            }
            
            return false; // 回傳失敗，攔截卡牌原本的增益效果
        }
    }

    // 檢定成功時的處理
    private void HandleSuccess()
    {
        hasTriggered = true;
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowPopupText("你成功避開了戰鬥！");
        }
        
        gameObject.SetActive(false); // 暫時用隱藏代替消滅
    }

    // --- 保留原本的強制進入戰鬥邏輯 ---
    public void TriggerBattle()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        if (PerspectiveMapGenerator.Instance != null)
        {
            PerspectiveMapGenerator.Instance.TransferToBattleScene(battleSceneName);
        }
        else
        {
            Debug.LogWarning("[EnemyInteractable] 找不到地圖總管，使用預設切換方式。");
            SceneManager.LoadSceneAsync(battleSceneName, LoadSceneMode.Additive);
            SceneManager.UnloadSceneAsync(gameObject.scene.name);
        }
    }

    // --- 點擊測試用 ---
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("[EnemyInteractable] 模擬使用 60% 的卡牌進行檢定...");
        OnCardPlayed(0.6f);
    }
}