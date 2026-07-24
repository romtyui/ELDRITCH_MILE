using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TutorialUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Root")]
    public GameObject rootObject;
    public CanvasGroup rootCanvasGroup;
    public RectTransform overlayRect;

    [Header("Overlay")]
    public Image darkOverlay;

    [Header("Highlight")]
    public RectTransform highlightFrame;
    public Image highlightFrameImage;

    [Header("Dialog")]
    public RectTransform dialogPanel;
    public TMP_Text tutorialText;
    public TMP_Text stepCounterText;

    [Header("Example")]
    public Image exampleImage;

    [Header("Buttons")]
    public Button nextButton;
    public TMP_Text nextButtonText;
    public Button backButton;
    public Button skipButton;

    [Header("Text")]
    public string nextText = "下一步";
    public string finishText = "完成";

    [Header("Safe Area")]
    public float screenEdgePadding = 24f;

    private bool allowScreenClickAdvance;

    public event Action NextClicked;
    public event Action BackClicked;
    public event Action SkipClicked;
    public event Action ScreenClicked;

    private void Awake()
    {
        AutoFindReferences();
        BindButtons();
        Hide();
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    private void AutoFindReferences()
    {
        if (rootObject == null)
            rootObject = gameObject;

        if (rootCanvasGroup == null)
            rootCanvasGroup = GetComponent<CanvasGroup>();

        if (overlayRect == null)
            overlayRect = transform as RectTransform;
    }

    private void BindButtons()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(HandleNextClicked);

        if (backButton != null)
            backButton.onClick.AddListener(HandleBackClicked);

        if (skipButton != null)
            skipButton.onClick.AddListener(HandleSkipClicked);
    }

    private void UnbindButtons()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(HandleNextClicked);

        if (backButton != null)
            backButton.onClick.RemoveListener(HandleBackClicked);

        if (skipButton != null)
            skipButton.onClick.RemoveListener(HandleSkipClicked);
    }

    private void HandleNextClicked()
    {
        NextClicked?.Invoke();
    }

    private void HandleBackClicked()
    {
        BackClicked?.Invoke();
    }

    private void HandleSkipClicked()
    {
        SkipClicked?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!allowScreenClickAdvance)
            return;

        GameObject clicked = eventData.pointerCurrentRaycast.gameObject;

        if (clicked != null)
        {
            if (nextButton != null && clicked.transform.IsChildOf(nextButton.transform))
                return;

            if (backButton != null && clicked.transform.IsChildOf(backButton.transform))
                return;

            if (skipButton != null && clicked.transform.IsChildOf(skipButton.transform))
                return;
        }

        ScreenClicked?.Invoke();
    }

    public void Show(bool blockOtherUI)
    {
        if (rootObject != null)
            rootObject.SetActive(true);

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 1f;
            rootCanvasGroup.interactable = true;
            rootCanvasGroup.blocksRaycasts = blockOtherUI;
        }
    }

    public void Hide()
    {
        if (rootObject != null)
            rootObject.SetActive(true);

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.interactable = false;
            rootCanvasGroup.blocksRaycasts = false;
        }
    }

    public void SetStep(TutorialStepData step, int currentIndex, int totalCount, bool isLastStep)
    {
        if (step == null)
            return;

        if (tutorialText != null)
            tutorialText.text = step.message;

        if (stepCounterText != null)
            stepCounterText.text = $"{currentIndex + 1} / {totalCount}";

        SetExampleImage(step);
        SetButtons(step, currentIndex, isLastStep);

        allowScreenClickAdvance = step.advanceMode == TutorialAdvanceMode.AnyScreenClick;
    }

    private void SetExampleImage(TutorialStepData step)
    {
        if (exampleImage == null)
            return;

        bool hasSprite = step.exampleSprite != null;

        exampleImage.sprite = step.exampleSprite;

        if (step.hideExampleWhenEmpty)
            exampleImage.gameObject.SetActive(hasSprite);
        else
            exampleImage.gameObject.SetActive(true);

        exampleImage.enabled = hasSprite;
    }

    private void SetButtons(TutorialStepData step, int currentIndex, bool isLastStep)
    {
        if (nextButton != null)
        {
            bool showNext =
                step.showNextButton &&
                step.advanceMode == TutorialAdvanceMode.NextButton;

            nextButton.gameObject.SetActive(showNext);
        }

        if (nextButtonText != null)
            nextButtonText.text = isLastStep ? finishText : nextText;

        if (backButton != null)
        {
            bool showBack =
                currentIndex > 0 &&
                step.showBackButton &&
                step.allowBack;

            backButton.gameObject.SetActive(showBack);
        }

        if (skipButton != null)
            skipButton.gameObject.SetActive(step.showSkipButton);
    }

    public void SetHighlight(RectTransform target, Vector2 padding)
    {
        if (highlightFrame == null)
            return;

        if (target == null)
        {
            highlightFrame.gameObject.SetActive(false);
            return;
        }

        if (overlayRect == null)
            overlayRect = transform as RectTransform;

        if (overlayRect == null)
        {
            highlightFrame.gameObject.SetActive(false);
            return;
        }

        Canvas.ForceUpdateCanvases();

        highlightFrame.gameObject.SetActive(true);

        Vector3[] targetCorners = new Vector3[4];
        target.GetWorldCorners(targetCorners);

        Vector2 bottomLeft = WorldToOverlayLocal(targetCorners[0]);
        Vector2 topRight = WorldToOverlayLocal(targetCorners[2]);

        Vector2 center = (bottomLeft + topRight) * 0.5f;
        Vector2 size = topRight - bottomLeft;

        size.x = Mathf.Abs(size.x);
        size.y = Mathf.Abs(size.y);

        size += padding * 2f;

        highlightFrame.anchorMin = new Vector2(0.5f, 0.5f);
        highlightFrame.anchorMax = new Vector2(0.5f, 0.5f);
        highlightFrame.pivot = new Vector2(0.5f, 0.5f);

        highlightFrame.anchoredPosition = center;
        highlightFrame.sizeDelta = size;

        highlightFrame.SetAsLastSibling();

        if (dialogPanel != null)
            dialogPanel.SetAsLastSibling();
    }
    public void PositionDialog(RectTransform target, TutorialDialogPosition position, float spacing)
    {
        if (dialogPanel == null || overlayRect == null)
            return;

        if (target == null || position == TutorialDialogPosition.KeepCurrent)
            return;

        Vector3[] targetCorners = new Vector3[4];
        target.GetWorldCorners(targetCorners);

        Vector2 bottomLeft = WorldToOverlayLocal(targetCorners[0]);
        Vector2 topRight = WorldToOverlayLocal(targetCorners[2]);
        Vector2 center = (bottomLeft + topRight) * 0.5f;

        TutorialDialogPosition finalPosition = position;

        if (position == TutorialDialogPosition.Auto)
        {
            float spaceAbove = overlayRect.rect.yMax - topRight.y;
            float spaceBelow = bottomLeft.y - overlayRect.rect.yMin;

            finalPosition = spaceAbove >= spaceBelow
                ? TutorialDialogPosition.Top
                : TutorialDialogPosition.Bottom;
        }

        Vector2 dialogSize = dialogPanel.rect.size;
        Vector2 destination = center;

        switch (finalPosition)
        {
            case TutorialDialogPosition.Top:
                destination.y = topRight.y + spacing + dialogSize.y * 0.5f;
                break;

            case TutorialDialogPosition.Bottom:
                destination.y = bottomLeft.y - spacing - dialogSize.y * 0.5f;
                break;

            case TutorialDialogPosition.Left:
                destination.x = bottomLeft.x - spacing - dialogSize.x * 0.5f;
                break;

            case TutorialDialogPosition.Right:
                destination.x = topRight.x + spacing + dialogSize.x * 0.5f;
                break;
        }

        dialogPanel.anchoredPosition = ClampDialogPosition(destination, dialogSize);
    }

    private Vector2 WorldToOverlayLocal(Vector3 worldPosition)
    {
        Canvas canvas = overlayRect.GetComponentInParent<Canvas>();

        Camera cameraToUse = null;

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cameraToUse = canvas.worldCamera;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cameraToUse, worldPosition);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayRect,
            screenPoint,
            cameraToUse,
            out Vector2 localPoint
        );

        return localPoint;
    }

    private Vector2 ClampDialogPosition(Vector2 position, Vector2 dialogSize)
    {
        Rect area = overlayRect.rect;

        float halfWidth = dialogSize.x * 0.5f;
        float halfHeight = dialogSize.y * 0.5f;

        position.x = Mathf.Clamp(
            position.x,
            area.xMin + halfWidth + screenEdgePadding,
            area.xMax - halfWidth - screenEdgePadding
        );

        position.y = Mathf.Clamp(
            position.y,
            area.yMin + halfHeight + screenEdgePadding,
            area.yMax - halfHeight - screenEdgePadding
        );

        return position;
    }
}