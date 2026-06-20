using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TurnEndButtonAnimatorUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    public Animator animator;
    public Button button;
    public CanvasGroup canvasGroup;

    [Header("Animator Parameters")]
    public string isPlayerTurnParam = "IsPlayerTurn";
    public string isHoverParam = "IsHover";
    public string clickToEnemyTrigger = "ClickToEnemy";
    public string clickToPlayerTrigger = "ClickToPlayer";

    private bool isPlayerTurn = true;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (button == null)
            button = GetComponent<Button>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    public void SetPlayerTurnIdle()
    {
        isPlayerTurn = true;

        if (animator != null)
        {
            animator.SetBool(isPlayerTurnParam, true);
            animator.SetBool(isHoverParam, false);
            animator.ResetTrigger(clickToEnemyTrigger);
            animator.SetTrigger(clickToPlayerTrigger);
        }

        SetInteractable(true);
    }

    public void SetEnemyTurnIdle()
    {
        isPlayerTurn = false;

        if (animator != null)
        {
            animator.SetBool(isPlayerTurnParam, false);
            animator.SetBool(isHoverParam, false);
            animator.ResetTrigger(clickToPlayerTrigger);
            animator.SetTrigger(clickToEnemyTrigger);
        }

        SetInteractable(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isPlayerTurn)
            return;

        if (animator != null)
            animator.SetBool(isHoverParam, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (animator != null)
            animator.SetBool(isHoverParam, false);
    }

    private void SetInteractable(bool value)
    {
        if (button != null)
            button.interactable = value;

        if (canvasGroup != null)
        {
            canvasGroup.interactable = value;
            canvasGroup.blocksRaycasts = value;
        }
    }
}