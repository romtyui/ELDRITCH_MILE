using UnityEngine;

[DisallowMultipleComponent]
public class RoomEntrance : MonoBehaviour
{
    [Header("共用控制器")]
    [Tooltip("場景中共用的房間過渡控制器。")]
    [SerializeField] private RoomTransitionController transitionController;

    [Header("入口設定")]
    [Tooltip("這個入口要前往的房間根物件。")]
    [SerializeField] private GameObject destinationRoom;

    [Tooltip("鏡頭走向的門口位置。留空時使用這個入口物件本身的位置。")]
    [SerializeField] private Transform entrancePoint;

    [Tooltip("進入目的地後的攝影機位置與旋轉。留空時沿用走入前的攝影機位置與旋轉。")]
    [SerializeField] private Transform destinationCameraPoint;

    public void EnterRoom()
    {
        if (!isActiveAndEnabled) return;

        if (transitionController == null || destinationRoom == null)
        {
            Debug.LogWarning("RoomEntrance：請指定共用過渡控制器與目的地房間。", this);
            return;
        }

        Transform targetEntrance = entrancePoint != null ? entrancePoint : transform;
        transitionController.EnterRoom(destinationRoom, targetEntrance, destinationCameraPoint);
    }
}