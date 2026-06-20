using UnityEngine;
using UnityEngine.EventSystems;

public class Door : MonoBehaviour, IPointerClickHandler
{
    private MapNodeExplore targetNode;

    // 由 RoomController 呼叫，用來分配這扇門通往哪個節點
    public void SetTarget(MapNodeExplore node)
    {
        targetNode = node;
    }

    // --- 新增：一個沒有參數的公開方法，讓 UnityEvent 可以順利在下拉選單找到它 ---
    public void OpenDoor()
    {
        if (targetNode != null)
        {
            Debug.Log($"[Door] 準備前往: {targetNode.roomName}");
            ExplorationManager.Instance.TransitionToNode(targetNode);
        }
        else
        {
            Debug.LogWarning("[Door] 這扇門尚未設定目標節點！");
        }
    }

    // --- 修改：保留滑鼠點擊功能，並讓它去呼叫 OpenDoor ---
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("[Door] 玩家直接點擊了門");
        OpenDoor();
    }
}