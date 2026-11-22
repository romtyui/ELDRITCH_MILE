using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class SceneObjectButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("互動設定")]
    [Tooltip("是否允許這個場景物件接收互動事件。")]
    [SerializeField] private bool interactable = true;

    [Header("互動事件")]
    [Tooltip("滑鼠左鍵點擊物件時執行。")]
    [SerializeField] private UnityEvent onClick = new UnityEvent();

    [Tooltip("滑鼠移入物件的碰撞範圍時執行。")]
    [SerializeField] private UnityEvent onPointerEnter = new UnityEvent();

    [Tooltip("滑鼠移出物件，或停留期間互動被關閉時執行。")]
    [SerializeField] private UnityEvent onPointerExit = new UnityEvent();

    private bool isPointerInside;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isActiveAndEnabled || !interactable) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;
        onClick.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isActiveAndEnabled || !interactable || isPointerInside) return;
        isPointerInside = true;
        onPointerEnter.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ClearPointerState();
    }

    public void SetInteractable(bool value)
    {
        if (interactable == value) return;
        interactable = value;
        if (!interactable) ClearPointerState();
    }

    private void OnDisable()
    {
        ClearPointerState();
    }

    private void ClearPointerState()
    {
        if (!isPointerInside) return;
        isPointerInside = false;
        onPointerExit.Invoke();
    }
}