using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class EnemyInteractable : MonoBehaviour, IPointerClickHandler
{
    [Header("戰鬥設定")]
    [Tooltip("要載入的同學戰鬥場景名稱")]
    public string battleSceneName = "BattleScene";

    private bool hasTriggered = false;

    // --- 給卡牌系統 (UnityEvent) 呼叫的公開方法 ---
    public void TriggerBattle()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        if (PerspectiveMapGenerator.Instance != null)
        {
            // 【修正核心】：不要自己卸載自己，把工作交給地圖總管，讓它去更新紀錄並切換場景
            PerspectiveMapGenerator.Instance.TransferToBattleScene(battleSceneName);
        }
        else
        {
            // 容錯處理 (萬一你在沒有地圖的環境下單獨測試 ExploreScene)
            Debug.LogWarning("[EnemyInteractable] 找不到地圖總管，使用預設切換方式。");
            SceneManager.LoadSceneAsync(battleSceneName, LoadSceneMode.Additive);
            SceneManager.UnloadSceneAsync(gameObject.scene.name);
        }
    }

    // --- 保留滑鼠直接點擊觸發的功能 ---
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("[EnemyInteractable] 玩家點擊了怪物立繪，準備切換戰鬥！");
        TriggerBattle();
    }
}