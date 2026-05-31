using UnityEngine;
using UnityEngine.EventSystems;

// 實作 IPointerClickHandler 確保與 Unity EventSystem 完美結合
public class Door : MonoBehaviour, IPointerClickHandler
{
    private MapNodeExplore targetNode;

    // 由 RoomController 呼叫，用來分配這扇門通往哪個節點
    public void SetTarget(MapNodeExplore node)
    {
        targetNode = node;
    }

    // 當掛載 PhysicsRaycaster 的相機照射到此物件的 Collider，且玩家點擊時觸發
    public void OnPointerClick(PointerEventData eventData)
    {
        if (targetNode != null)
        {
            Debug.Log($"[Door] 玩家點擊了門，準備前往: {targetNode.roomName}");
            ExplorationManager.Instance.TransitionToNode(targetNode);
        }
        else
        {
            Debug.LogWarning("[Door] 這扇門尚未設定目標節點！");
        }
    }
}