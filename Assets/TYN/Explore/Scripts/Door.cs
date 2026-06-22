using UnityEngine;
using UnityEngine.EventSystems;

public class Door : MonoBehaviour, IPointerClickHandler
{
    // --- 新增：一個沒有參數的公開方法，讓 UnityEvent 可以順利在下拉選單找到它 ---
    public void OpenDoor()
    {
        Debug.Log("[Door] 準備離開房間，返回大地圖...");
        if (ExplorationManager.Instance != null)
        {
            ExplorationManager.Instance.ExitExploreScene();
        }
    }

    // --- 修改：保留滑鼠點擊功能，並讓它去呼叫 OpenDoor ---
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("[Door] 玩家直接點擊了門");
        OpenDoor();
    }
}