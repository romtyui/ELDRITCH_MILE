using UnityEngine;
using UnityEngine.EventSystems;

public class BattleRewardExitHoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("要移動的美術，例如 Close / Image。")]
    public RectTransform movingVisual;

    [Tooltip("未 Hover 時的位置。")]
    public Vector2 collapsedPosition;

    [Tooltip("Hover 時的位置。")]
    public Vector2 expandedPosition;

    [Min(0f)]
    [Tooltip("移動至目標位置需要幾秒。")]
    public float moveDuration = 0.15f;

    private Vector2 startPosition;
    private Vector2 targetPosition;
    private float elapsed;
    private bool moving;

    private void OnEnable()
    {
        moving = false;
        if (movingVisual != null) movingVisual.anchoredPosition = collapsedPosition;
    }

    private void OnDisable()
    {
        moving = false;
        if (movingVisual != null) movingVisual.anchoredPosition = collapsedPosition;
    }

    private void Update()
    {
        if (!moving || movingVisual == null) return;

        elapsed += Time.unscaledDeltaTime;
        float progress = moveDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / moveDuration);
        float eased = progress * progress * (3f - 2f * progress);
        movingVisual.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, eased);

        if (progress >= 1f) moving = false;
    }

    private void MoveTo(Vector2 position)
    {
        if (movingVisual == null) return;

        startPosition = movingVisual.anchoredPosition;
        targetPosition = position;
        elapsed = 0f;
        moving = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        MoveTo(expandedPosition);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        MoveTo(collapsedPosition);
    }
}