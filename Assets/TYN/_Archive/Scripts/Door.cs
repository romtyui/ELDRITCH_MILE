using UnityEngine;
using UnityEngine.EventSystems;

// ⚠️ 封存的舊碼。包進 namespace 是為了**不要污染全域命名空間** ——
//    這裡宣告的 RunNodeData / MapData / ICardInteractable 等等
//    與正式碼同名，放在全域會蓋掉正式那組，症狀是新檔案裡出現
//    「MapData 沒有 GetNode」這種完全不會指向這裡的錯誤。
namespace EldritchMile.Archive
{

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
}
