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
    [SerializeField]
    private TutorialInverseMaskRaycastFilter inverseMaskRaycastFilter;
    [Header("Condition Progress")]
    public TMP_Text conditionProgressText;

    [Header("Inverse Mask")]
    [SerializeField] private UnityEngine.UI.Image overlayImage;

    [SerializeField] private RectTransform highlightLocator;

    [SerializeField] private Material inverseMaskMaterialTemplate;

    [SerializeField]
    private Color overlayColor =
        new Color(0f, 0f, 0f, 0.75f);

    [SerializeField] private float cornerRadius = 0.05f;

    [SerializeField]
    private Vector2 locatorPadding =
        new Vector2(20f, 20f);

    private Material runtimeInverseMaskMaterial;

    private static readonly int OverlayColorId =
    Shader.PropertyToID("_OverlayColor");

    private static readonly int HoleCenterId =
        Shader.PropertyToID("_HoleCenter");

    private static readonly int HoleSizeId =
        Shader.PropertyToID("_HoleSize");

    private static readonly int CornerRadiusId =
        Shader.PropertyToID("_CornerRadius");

    private static readonly int HoleEnabledId =
        Shader.PropertyToID("_HoleEnabled");

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
        InitializeInverseMaskMaterial();
        Hide();
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }
    private void InitializeInverseMaskMaterial()
    {
        if (overlayImage == null)
        {
            Debug.LogWarning(
                "[TutorialUI] Overlay Image 沒有指定",
                this
            );

            return;
        }

        if (inverseMaskMaterialTemplate == null)
        {
            Debug.LogWarning(
                "[TutorialUI] Inverse Mask Material Template 沒有指定",
                this
            );

            return;
        }

        if (runtimeInverseMaskMaterial != null)
            return;

        runtimeInverseMaskMaterial =
            new Material(inverseMaskMaterialTemplate);

        runtimeInverseMaskMaterial.name =
            inverseMaskMaterialTemplate.name + "_Runtime";

        overlayImage.material =
            runtimeInverseMaskMaterial;

        runtimeInverseMaskMaterial.SetColor(
            OverlayColorId,
            overlayColor
        );

        runtimeInverseMaskMaterial.SetFloat(
            CornerRadiusId,
            cornerRadius
        );

        runtimeInverseMaskMaterial.SetFloat(
            HoleEnabledId,
            0f
        );
    }
    public void UpdateHoleFromLocator()
    {
        InitializeInverseMaskMaterial();

        if (runtimeInverseMaskMaterial == null)
            return;

        if (overlayImage == null)
            return;

        if (highlightLocator == null)
        {
            SetHoleEnabled(false);
            return;
        }

        RectTransform overlayRect =
            overlayImage.rectTransform;

        if (overlayRect == null)
            return;

        Canvas.ForceUpdateCanvases();

        Vector3[] locatorWorldCorners =
            new Vector3[4];

        highlightLocator.GetWorldCorners(
            locatorWorldCorners
        );

        Camera canvasCamera =
            GetCanvasCamera();

        Vector2 localBottomLeft;
        Vector2 localTopRight;

        bool gotBottomLeft =
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                overlayRect,
                RectTransformUtility.WorldToScreenPoint(
                    canvasCamera,
                    locatorWorldCorners[0]
                ),
                canvasCamera,
                out localBottomLeft
            );

        bool gotTopRight =
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                overlayRect,
                RectTransformUtility.WorldToScreenPoint(
                    canvasCamera,
                    locatorWorldCorners[2]
                ),
                canvasCamera,
                out localTopRight
            );

        if (!gotBottomLeft || !gotTopRight)
            return;

        Vector2 halfPadding =
            locatorPadding * 0.5f;

        localBottomLeft -= halfPadding;
        localTopRight += halfPadding;

        Rect overlayLocalRect =
            overlayRect.rect;

        float overlayWidth =
            Mathf.Max(1f, overlayLocalRect.width);

        float overlayHeight =
            Mathf.Max(1f, overlayLocalRect.height);

        Vector2 localCenter =
            (localBottomLeft + localTopRight) * 0.5f;

        Vector2 localSize =
            localTopRight - localBottomLeft;

        Vector2 holeCenter = new Vector2(
            Mathf.InverseLerp(
                overlayLocalRect.xMin,
                overlayLocalRect.xMax,
                localCenter.x
            ),
            Mathf.InverseLerp(
                overlayLocalRect.yMin,
                overlayLocalRect.yMax,
                localCenter.y
            )
        );

        Vector2 holeSize = new Vector2(
            localSize.x / overlayWidth,
            localSize.y / overlayHeight
        );

        holeSize.x =
            Mathf.Clamp01(holeSize.x);

        holeSize.y =
            Mathf.Clamp01(holeSize.y);
        
        runtimeInverseMaskMaterial.SetVector(
            HoleCenterId,
            holeCenter
        );

        runtimeInverseMaskMaterial.SetVector(
            HoleSizeId,
            holeSize
        );

        runtimeInverseMaskMaterial.SetFloat(
            CornerRadiusId,
            cornerRadius
        );

        runtimeInverseMaskMaterial.SetFloat(
            HoleEnabledId,
            1f
        );

        if (inverseMaskRaycastFilter != null)
        {
            inverseMaskRaycastFilter.SetHole(
                holeCenter,
                holeSize,
                cornerRadius
            );
        }
    }
    private Camera GetCanvasCamera()
    {
        if (overlayImage == null)
            return null;

        Canvas canvas =
            overlayImage.GetComponentInParent<Canvas>();

        if (canvas == null)
            return null;

        if (canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        if (canvas.worldCamera != null)
            return canvas.worldCamera;

        return Camera.main;
    }

    public void SetHoleEnabled(bool enabled)
    {
        InitializeInverseMaskMaterial();

        if (runtimeInverseMaskMaterial != null)
        {
            runtimeInverseMaskMaterial.SetFloat(
                HoleEnabledId,
                enabled ? 1f : 0f
            );
        }

        if (inverseMaskRaycastFilter != null)
        {
            inverseMaskRaycastFilter.SetHoleEnabled(
                enabled
            );
        }
    }
    public void MoveLocatorToTarget(
    RectTransform targetRect,
    Vector2 padding
)
    {
        if (highlightLocator == null)
            return;

        if (targetRect == null)
        {
            highlightLocator.gameObject.SetActive(false);
            SetHoleEnabled(false);
            return;
        }

        highlightLocator.gameObject.SetActive(true);

        Vector3[] worldCorners =
            new Vector3[4];

        targetRect.GetWorldCorners(worldCorners);

        RectTransform locatorParent =
            highlightLocator.parent as RectTransform;

        if (locatorParent == null)
            return;

        Camera canvasCamera =
            GetCanvasCamera();

        Vector2 localBottomLeft;
        Vector2 localTopRight;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            locatorParent,
            RectTransformUtility.WorldToScreenPoint(
                canvasCamera,
                worldCorners[0]
            ),
            canvasCamera,
            out localBottomLeft
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            locatorParent,
            RectTransformUtility.WorldToScreenPoint(
                canvasCamera,
                worldCorners[2]
            ),
            canvasCamera,
            out localTopRight
        );

        Vector2 center =
            (localBottomLeft + localTopRight) * 0.5f;

        Vector2 size =
            localTopRight - localBottomLeft;

        size += padding;

        highlightLocator.anchorMin =
            new Vector2(0.5f, 0.5f);

        highlightLocator.anchorMax =
            new Vector2(0.5f, 0.5f);

        highlightLocator.pivot =
            new Vector2(0.5f, 0.5f);

        highlightLocator.anchoredPosition =
            center;

        highlightLocator.sizeDelta =
            size;

        locatorPadding = Vector2.zero;

        UpdateHoleFromLocator();
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
        ClearConditionProgress();
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

            bool canShowNextButton =step.showNextButton &&step.advanceMode != TutorialAdvanceMode.WaitForSignal;

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(canShowNextButton);
            }
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
    public void SetConditionProgress(int current,int required)
    {
        if (conditionProgressText == null)
            return;

        if (required <= 1)
        {
            conditionProgressText.text = "";
            conditionProgressText.gameObject.SetActive(false);
            return;
        }

        conditionProgressText.gameObject.SetActive(true);
        conditionProgressText.text =
            $"操作進度：{current}/{required}";
    }
    public void ClearConditionProgress()
    {
        if (conditionProgressText == null)
            return;

        conditionProgressText.text = "";
        conditionProgressText.gameObject.SetActive(false);
    }
}