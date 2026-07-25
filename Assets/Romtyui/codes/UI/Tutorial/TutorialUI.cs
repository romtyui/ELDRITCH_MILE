using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TutorialUI :
    MonoBehaviour,
    IPointerClickHandler
{
    [Header("Root")]
    public GameObject rootObject;

    public CanvasGroup rootCanvasGroup;

    public RectTransform overlayRect;

    [Header("Overlay")]
    public Image overlayImage;

    [SerializeField]
    private Material inverseMaskMaterialTemplate;

    [SerializeField]
    private TutorialInverseMaskRaycastFilter
        inverseMaskRaycastFilter;

    [SerializeField]
    private Color overlayColor =
        new Color(0f, 0f, 0f, 0.75f);

    [SerializeField]
    private float cornerRadius = 0.05f;

    [Header("Highlight Locator")]
    [SerializeField]
    private RectTransform highlightLocator;

    [Header("Dialogue Panel")]
    [SerializeField]
    private GameObject dialoguePanel;

    [SerializeField]
    private TMP_Text dialogueSpeakerNameText;

    [SerializeField]
    private TMP_Text dialogueText;

    [SerializeField]
    private Image leftPortraitImage;

    [SerializeField]
    private Image rightPortraitImage;

    [SerializeField]
    private GameObject dialogueContinueIndicator;

    [SerializeField]
    private Button dialogueClickArea;

    [Header("Instruction Panel")]
    [SerializeField]
    private GameObject instructionPanel;

    [SerializeField]
    private RectTransform instructionPanelRect;

    [SerializeField]
    private TMP_Text tutorialText;

    [SerializeField]
    private TMP_Text stepCounterText;


    [Header("Example")]
    public Image exampleImage;

    [Header("Buttons")]
    public Button nextButton;

    public TMP_Text nextButtonText;

    public Button backButton;

    public Button skipButton;

    [Header("Condition Progress")]
    public TMP_Text conditionProgressText;

    [Header("Text")]
    public string nextText = "下一步";

    public string finishText = "完成";

    [Header("對話")]

    private Coroutine dialogueTypewriterCoroutine;

    private TutorialDialogueLine currentDialogueLine;

    private string currentDialogueFullText =
        string.Empty;

    private bool isDialogueTyping;
    private sealed class DialoguePortraitCue
    {
        public int visibleCharacterIndex;
        public string styleId;
    }

    private readonly List<DialoguePortraitCue>
        currentPortraitCues = new();

    private int nextPortraitCueIndex;

    private static readonly Regex PortraitTagRegex =
        new Regex(
            @"\{portrait\s*:\s*([^}]+)\}",
            RegexOptions.IgnoreCase |
            RegexOptions.Compiled
        );
    [Header("Dialog Position")]
    public Vector2 defaultDialogPosition;

    public float screenEdgePadding = 24f;

    private Material runtimeInverseMaskMaterial;

    private bool allowScreenClickAdvance;

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

    public event Action NextClicked;

    public event Action BackClicked;

    public event Action SkipClicked;

    public event Action ScreenClicked;

    public event Action DialogueClicked;

    private void Awake()
    {
        AutoFindReferences();

        if (instructionPanelRect != null)
        {
            defaultDialogPosition =
                instructionPanelRect.anchoredPosition;
        }

        BindButtons();
        InitializeInverseMaskMaterial();
        Hide();
    }

    private void OnDestroy()
    {
        UnbindButtons();
        StopDialogueTypewriter();

        if (runtimeInverseMaskMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(runtimeInverseMaskMaterial);
            }
            else
            {
                DestroyImmediate(runtimeInverseMaskMaterial);
            }

            runtimeInverseMaskMaterial = null;
        }
    }

    private void AutoFindReferences()
    {
        if (rootObject == null)
            rootObject = gameObject;

        if (rootCanvasGroup == null)
        {
            rootCanvasGroup =
                GetComponent<CanvasGroup>();
        }

        if (overlayRect == null)
            overlayRect = transform as RectTransform;
    }

    private void BindButtons()
    {
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(
                HandleNextClicked
            );
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(
                HandleBackClicked
            );
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(
                HandleSkipClicked
            );
        }
        if (dialogueClickArea != null)
        {
            dialogueClickArea.onClick.AddListener(
                HandleDialogueClicked
            );
        }
    }
    private void HandleDialogueClicked()
    {
        DialogueClicked?.Invoke();
    }
    private void UnbindButtons()
    {
        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(
                HandleNextClicked
            );
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(
                HandleBackClicked
            );
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(
                HandleSkipClicked
            );
        }
        if (dialogueClickArea != null)
        {
            dialogueClickArea.onClick.RemoveListener(
                HandleDialogueClicked
            );
        }
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

    public void OnPointerClick(
        PointerEventData eventData
    )
    {
        if (!allowScreenClickAdvance)
            return;

        GameObject clicked =
            eventData.pointerCurrentRaycast.gameObject;

        if (clicked != null)
        {
            if (nextButton != null &&
                clicked.transform.IsChildOf(
                    nextButton.transform))
            {
                return;
            }

            if (backButton != null &&
                clicked.transform.IsChildOf(
                    backButton.transform))
            {
                return;
            }

            if (skipButton != null &&
                clicked.transform.IsChildOf(
                    skipButton.transform))
            {
                return;
            }
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
            rootCanvasGroup.blocksRaycasts =
                blockOtherUI;
        }
    }

    public void Hide()
    {
        StopDialogueTypewriter();

        currentDialogueLine = null;
        currentDialogueFullText = string.Empty;

        SetHoleEnabled(false);

        HideDialoguePanel();
        HideInstructionPanel();

        if (rootObject != null)
            rootObject.SetActive(true);

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.interactable = false;
            rootCanvasGroup.blocksRaycasts = false;
        }
    }

    public void SetInstructionStep(
    TutorialStepData step,
    int currentIndex,
    int totalCount,
    bool isLastStep
)
    {
        ClearConditionProgress();

        if (step == null)
            return;

        if (tutorialText != null)
            tutorialText.text = step.message;

        if (stepCounterText != null)
        {
            stepCounterText.text =
                $"{currentIndex + 1} / {totalCount}";
        }

        SetExampleImage(step);
        SetButtons(step, currentIndex, isLastStep);

        allowScreenClickAdvance =
            step.advanceMode ==
            TutorialAdvanceMode.AnyScreenClick;
    }









   





    private void SetExampleImage(
        TutorialStepData step
    )
    {
        if (exampleImage == null)
            return;

        bool hasSprite =
            step.exampleSprite != null;

        exampleImage.sprite =
            step.exampleSprite;

        if (step.hideExampleWhenEmpty)
        {
            exampleImage.gameObject.SetActive(
                hasSprite
            );
        }
        else
        {
            exampleImage.gameObject.SetActive(true);
        }

        exampleImage.enabled = hasSprite;
    }

    private void SetButtons(
        TutorialStepData step,
        int currentIndex,
        bool isLastStep
    )
    {
        if (nextButton != null)
        {
            bool canShowNext =
                step.showNextButton &&
                step.advanceMode !=
                TutorialAdvanceMode.WaitForSignal;

            nextButton.gameObject.SetActive(
                canShowNext
            );
        }

        if (nextButtonText != null)
        {
            nextButtonText.text =
                isLastStep
                    ? finishText
                    : nextText;
        }

        if (backButton != null)
        {
            bool showBack =
                currentIndex > 0 &&
                step.showBackButton &&
                step.allowBack;

            backButton.gameObject.SetActive(
                showBack
            );
        }

        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(
                step.showSkipButton
            );
        }
    }

    public void SetConditionProgress(
        int current,
        int required
    )
    {
        if (conditionProgressText == null)
            return;

        if (required <= 1)
        {
            ClearConditionProgress();
            return;
        }

        conditionProgressText.gameObject.SetActive(
            true
        );

        conditionProgressText.text =
            $"操作進度：{current}/{required}";
    }

    public void ClearConditionProgress()
    {
        if (conditionProgressText == null)
            return;

        conditionProgressText.text =
            string.Empty;

        conditionProgressText.gameObject.SetActive(
            false
        );
    }

    private void InitializeInverseMaskMaterial()
    {
        if (overlayImage == null ||
            inverseMaskMaterialTemplate == null)
        {
            return;
        }

        if (runtimeInverseMaskMaterial != null)
            return;

        runtimeInverseMaskMaterial =
            new Material(
                inverseMaskMaterialTemplate
            );

        runtimeInverseMaskMaterial.name =
            inverseMaskMaterialTemplate.name +
            "_Runtime";

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

    public void MoveLocatorToTarget(
        RectTransform targetRect,
        Vector2 padding
    )
    {
        InitializeInverseMaskMaterial();

        if (highlightLocator == null)
            return;

        if (targetRect == null)
        {
            highlightLocator.gameObject.SetActive(
                false
            );

            SetHoleEnabled(false);
            return;
        }

        highlightLocator.gameObject.SetActive(
            true
        );

        RectTransform locatorParent =
            highlightLocator.parent as RectTransform;

        if (locatorParent == null)
            return;

        Vector3[] worldCorners =
            new Vector3[4];

        targetRect.GetWorldCorners(worldCorners);

        Camera canvasCamera =
            GetCanvasCamera();

        RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                locatorParent,
                RectTransformUtility
                    .WorldToScreenPoint(
                        canvasCamera,
                        worldCorners[0]
                    ),
                canvasCamera,
                out Vector2 bottomLeft
            );

        RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                locatorParent,
                RectTransformUtility
                    .WorldToScreenPoint(
                        canvasCamera,
                        worldCorners[2]
                    ),
                canvasCamera,
                out Vector2 topRight
            );

        Vector2 center =
            (bottomLeft + topRight) * 0.5f;

        Vector2 size =
            topRight - bottomLeft;

        size.x = Mathf.Abs(size.x);
        size.y = Mathf.Abs(size.y);

        size += padding * 2f;

        highlightLocator.anchorMin =
            new Vector2(0.5f, 0.5f);

        highlightLocator.anchorMax =
            new Vector2(0.5f, 0.5f);

        highlightLocator.pivot =
            new Vector2(0.5f, 0.5f);

        highlightLocator.anchoredPosition =
            center;

        highlightLocator.sizeDelta = size;

        UpdateHoleFromLocator();
    }

    private void UpdateHoleFromLocator()
    {
        InitializeInverseMaskMaterial();

        if (runtimeInverseMaskMaterial == null ||
            overlayImage == null ||
            highlightLocator == null)
        {
            return;
        }

        RectTransform maskRect =
            overlayImage.rectTransform;

        Canvas.ForceUpdateCanvases();

        Vector3[] worldCorners =
            new Vector3[4];

        highlightLocator.GetWorldCorners(
            worldCorners
        );

        Camera canvasCamera =
            GetCanvasCamera();

        RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                maskRect,
                RectTransformUtility
                    .WorldToScreenPoint(
                        canvasCamera,
                        worldCorners[0]
                    ),
                canvasCamera,
                out Vector2 bottomLeft
            );

        RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                maskRect,
                RectTransformUtility
                    .WorldToScreenPoint(
                        canvasCamera,
                        worldCorners[2]
                    ),
                canvasCamera,
                out Vector2 topRight
            );

        Rect rect = maskRect.rect;

        Vector2 localCenter =
            (bottomLeft + topRight) * 0.5f;

        Vector2 localSize =
            topRight - bottomLeft;

        localSize.x = Mathf.Abs(localSize.x);
        localSize.y = Mathf.Abs(localSize.y);

        Vector2 holeCenter = new Vector2(
            Mathf.InverseLerp(
                rect.xMin,
                rect.xMax,
                localCenter.x
            ),
            Mathf.InverseLerp(
                rect.yMin,
                rect.yMax,
                localCenter.y
            )
        );

        Vector2 holeSize = new Vector2(
            localSize.x /
            Mathf.Max(1f, rect.width),

            localSize.y /
            Mathf.Max(1f, rect.height)
        );

        holeSize.x = Mathf.Clamp01(holeSize.x);
        holeSize.y = Mathf.Clamp01(holeSize.y);

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
            inverseMaskRaycastFilter
                .SetHoleEnabled(enabled);
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

    public void PositionInstructionPanel(RectTransform target,TutorialDialogPosition position,float spacing)
    {
        if (instructionPanelRect == null)
            return;

        if (position ==
            TutorialDialogPosition.KeepCurrent)
        {
            return;
        }

        if (position ==
            TutorialDialogPosition.Center)
        {
            CenterInstructionPanel();
            return;
        }

        if (overlayRect == null ||
            target == null)
        {
            CenterInstructionPanel();
            return;
        }

        Canvas.ForceUpdateCanvases();

        Vector3[] worldCorners =
            new Vector3[4];

        target.GetWorldCorners(worldCorners);

        Vector2 bottomLeft =
            WorldToOverlayLocal(worldCorners[0]);

        Vector2 topRight =
            WorldToOverlayLocal(worldCorners[2]);

        Vector2 center =
            (bottomLeft + topRight) * 0.5f;

        TutorialDialogPosition finalPosition =
            position;

        if (position ==
            TutorialDialogPosition.Auto)
        {
            float spaceAbove =
                overlayRect.rect.yMax -
                topRight.y;

            float spaceBelow =
                bottomLeft.y -
                overlayRect.rect.yMin;

            finalPosition =
                spaceAbove >= spaceBelow
                    ? TutorialDialogPosition.Top
                    : TutorialDialogPosition.Bottom;
        }

        Vector2 dialogSize =
            instructionPanelRect.rect.size;

        Vector2 destination = center;

        switch (finalPosition)
        {
            case TutorialDialogPosition.Top:
                destination.y =
                    topRight.y +
                    spacing +
                    dialogSize.y * 0.5f;
                break;

            case TutorialDialogPosition.Bottom:
                destination.y =
                    bottomLeft.y -
                    spacing -
                    dialogSize.y * 0.5f;
                break;

            case TutorialDialogPosition.Left:
                destination.x =
                    bottomLeft.x -
                    spacing -
                    dialogSize.x * 0.5f;
                break;

            case TutorialDialogPosition.Right:
                destination.x =
                    topRight.x +
                    spacing +
                    dialogSize.x * 0.5f;
                break;

            case TutorialDialogPosition.Center:
                CenterInstructionPanel();
                return;
        }

        instructionPanelRect.anchoredPosition =
            ClampDialogPosition(
                destination,
                dialogSize
            );
    }
    public void CenterInstructionPanel()
    {
        if (instructionPanelRect == null)
            return;

        instructionPanelRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        instructionPanelRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        instructionPanelRect.pivot =
            new Vector2(0.5f, 0.5f);

        instructionPanelRect.anchoredPosition =
            Vector2.zero;
    }
    public void ResetInstructionPosition()
    {
        if (instructionPanelRect == null)
            return;

        instructionPanelRect.anchoredPosition =
            defaultDialogPosition;
    }
    private Vector2 WorldToOverlayLocal(
        Vector3 worldPosition
    )
    {
        Canvas canvas =
            overlayRect.GetComponentInParent<Canvas>();

        Camera cameraToUse = null;

        if (canvas != null &&
            canvas.renderMode !=
            RenderMode.ScreenSpaceOverlay)
        {
            cameraToUse = canvas.worldCamera;
        }

        Vector2 screenPoint =
            RectTransformUtility.WorldToScreenPoint(
                cameraToUse,
                worldPosition
            );

        RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                overlayRect,
                screenPoint,
                cameraToUse,
                out Vector2 localPoint
            );

        return localPoint;
    }

    private Vector2 ClampDialogPosition(
        Vector2 position,
        Vector2 dialogSize
    )
    {
        Rect area = overlayRect.rect;

        float halfWidth =
            dialogSize.x * 0.5f;

        float halfHeight =
            dialogSize.y * 0.5f;

        position.x = Mathf.Clamp(
            position.x,
            area.xMin +
            halfWidth +
            screenEdgePadding,
            area.xMax -
            halfWidth -
            screenEdgePadding
        );

        position.y = Mathf.Clamp(
            position.y,
            area.yMin +
            halfHeight +
            screenEdgePadding,
            area.yMax -
            halfHeight -
            screenEdgePadding
        );

        return position;
    }
    public void ShowDialoguePanel()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);
    }

    public void HideDialoguePanel()
    {
        StopDialogueTypewriter();
        ClearDialogueVisuals();

        currentDialogueLine = null;
        currentDialogueFullText = string.Empty;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    public void ShowInstructionPanel()
    {
        if (instructionPanel != null)
            instructionPanel.SetActive(true);
    }

    public void HideInstructionPanel()
    {
        if (instructionPanel != null)
            instructionPanel.SetActive(false);
    }

    public void ShowDialogueLine(TutorialDialogueLine line)
    {
        StopDialogueTypewriter();

        currentDialogueLine = line;

        currentPortraitCues.Clear();
        nextPortraitCueIndex = 0;

        if (line == null)
        {
            ClearDialogueVisuals();
            return;
        }

        SetupDialogueSpeaker(line);

        currentDialogueFullText =
            ParseDialogueText(
                line,
                line.text ?? string.Empty
            );

        SetupDialoguePortrait(
            line,
            line.GetInitialPortraitStyleId()
        );

        ApplyPortraitCuesUpTo(0);

        if (dialogueText == null)
        {
            Debug.LogWarning(
                "[TutorialUI] Dialogue Text 沒有綁定",
                this
            );

            SetDialogueContinueIndicator(false);
            return;
        }

        if (!line.useTypewriter ||
            string.IsNullOrEmpty(currentDialogueFullText))
        {
            dialogueText.text =
                currentDialogueFullText;

            dialogueText.maxVisibleCharacters =
                int.MaxValue;

            ApplyAllPortraitCues();

            isDialogueTyping = false;

            SetDialogueContinueIndicator(true);
            return;
        }

        dialogueTypewriterCoroutine =
            StartCoroutine(
                DialogueTypewriterRoutine(line)
            );
    }
    private string ParseDialogueText(
    TutorialDialogueLine line,
    string rawText
)
    {
        currentPortraitCues.Clear();
        nextPortraitCueIndex = 0;

        if (string.IsNullOrEmpty(rawText))
            return string.Empty;

        if (line == null ||
            !line.allowInlinePortraitChange)
        {
            return rawText;
        }

        StringBuilder visibleText =
            new StringBuilder();

        MatchCollection matches =
            PortraitTagRegex.Matches(rawText);

        int sourceIndex = 0;

        for (int i = 0; i < matches.Count; i++)
        {
            Match match = matches[i];

            if (!match.Success)
                continue;

            int normalTextLength =
                match.Index - sourceIndex;

            if (normalTextLength > 0)
            {
                visibleText.Append(
                    rawText,
                    sourceIndex,
                    normalTextLength
                );
            }

            string styleId =
                match.Groups[1].Value.Trim();

            if (!string.IsNullOrWhiteSpace(styleId))
            {
                currentPortraitCues.Add(
                    new DialoguePortraitCue
                    {
                        visibleCharacterIndex =
                            visibleText.Length,

                        styleId = styleId
                    }
                );
            }

            sourceIndex =
                match.Index + match.Length;
        }

        if (sourceIndex < rawText.Length)
        {
            visibleText.Append(
                rawText,
                sourceIndex,
                rawText.Length - sourceIndex
            );
        }

        return visibleText.ToString();
    }

    private IEnumerator DialogueTypewriterRoutine(TutorialDialogueLine line)
    {
        if (dialogueText == null)
            yield break;

        if (line == null)
            yield break;

        isDialogueTyping = true;

        SetDialogueContinueIndicator(false);

        dialogueText.text =
            currentDialogueFullText;

        dialogueText.maxVisibleCharacters = 0;

        dialogueText.ForceMeshUpdate();

        int characterCount =
            dialogueText.textInfo.characterCount;

        float interval =
            Mathf.Max(
                0.001f,
                line.typewriterInterval
            );

        ApplyPortraitCuesUpTo(0);

        for (int i = 0; i <= characterCount; i++)
        {
            ApplyPortraitCuesUpTo(i);

            dialogueText.maxVisibleCharacters = i;

            yield return new WaitForSecondsRealtime(
                interval
            );
        }

        ApplyAllPortraitCues();

        dialogueText.maxVisibleCharacters =
            int.MaxValue;

        isDialogueTyping = false;
        dialogueTypewriterCoroutine = null;

        SetDialogueContinueIndicator(true);
    }
    private void ApplyPortraitCuesUpTo(
    int visibleCharacterIndex
)
    {
        while (nextPortraitCueIndex <
               currentPortraitCues.Count)
        {
            DialoguePortraitCue cue =
                currentPortraitCues[
                    nextPortraitCueIndex
                ];

            if (cue.visibleCharacterIndex >
                visibleCharacterIndex)
            {
                break;
            }

            ApplyPortraitStyle(cue.styleId);

            nextPortraitCueIndex++;
        }
    }
    private void ApplyAllPortraitCues()
    {
        while (nextPortraitCueIndex <
               currentPortraitCues.Count)
        {
            DialoguePortraitCue cue =
                currentPortraitCues[
                    nextPortraitCueIndex
                ];

            ApplyPortraitStyle(cue.styleId);

            nextPortraitCueIndex++;
        }
    }
    private void ApplyPortraitStyle(
    string styleId
)
    {
        if (currentDialogueLine == null)
            return;

        if (currentDialogueLine.speaker == null)
            return;

        Sprite portrait =
            currentDialogueLine.speaker.GetPortrait(
                styleId
            );

        if (portrait == null)
        {
            Debug.LogWarning(
                $"[TutorialUI] 找不到立繪樣式：" +
                $"{currentDialogueLine.speaker.displayName} / " +
                $"{styleId}",
                currentDialogueLine.speaker
            );

            return;
        }

        DialoguePortraitSide side =
            currentDialogueLine.GetPortraitSide();

        Image targetImage =
            side == DialoguePortraitSide.Right
                ? rightPortraitImage
                : leftPortraitImage;

        Image oppositeImage =
            side == DialoguePortraitSide.Right
                ? leftPortraitImage
                : rightPortraitImage;

        if (oppositeImage != null)
            oppositeImage.gameObject.SetActive(false);

        if (targetImage == null)
            return;

        targetImage.sprite = portrait;
        targetImage.preserveAspect = true;
        targetImage.gameObject.SetActive(true);
    }

    public bool TryCompleteDialogueTyping()
    {
        if (!isDialogueTyping)
            return false;

        if (currentDialogueLine == null)
            return false;

        if (!currentDialogueLine.allowClickToCompleteText)
            return false;

        CompleteDialogueTypingImmediately();

        return true;
    }
    public void CompleteDialogueTypingImmediately()
    {
        StopDialogueTypewriter();

        if (dialogueText != null)
        {
            dialogueText.text =
                currentDialogueFullText;

            dialogueText.maxVisibleCharacters =
                int.MaxValue;
        }

        ApplyAllPortraitCues();

        isDialogueTyping = false;

        SetDialogueContinueIndicator(
            currentDialogueLine != null
        );
    }
    private void StopDialogueTypewriter()
    {
        if (dialogueTypewriterCoroutine != null)
        {
            StopCoroutine(
                dialogueTypewriterCoroutine
            );

            dialogueTypewriterCoroutine = null;
        }

        isDialogueTyping = false;
    }
    private void ClearDialogueVisuals()
    {
        if (dialogueSpeakerNameText != null)
        {
            dialogueSpeakerNameText.text =
                string.Empty;

            dialogueSpeakerNameText.gameObject.SetActive(
                false
            );
        }

        if (dialogueText != null)
        {
            dialogueText.text =
                string.Empty;

            dialogueText.maxVisibleCharacters =
                int.MaxValue;
        }

        if (leftPortraitImage != null)
            leftPortraitImage.gameObject.SetActive(false);

        if (rightPortraitImage != null)
            rightPortraitImage.gameObject.SetActive(false);

        currentPortraitCues.Clear();
        nextPortraitCueIndex = 0;

        SetDialogueContinueIndicator(false);
    }
    private void SetupDialogueSpeaker(TutorialDialogueLine line)
    {
        if (dialogueSpeakerNameText == null)
            return;

        bool hasName =
            line != null &&
            line.speaker != null &&
            !string.IsNullOrWhiteSpace(
                line.speaker.displayName
            );

        dialogueSpeakerNameText.gameObject.SetActive(
            hasName
        );

        dialogueSpeakerNameText.text =
            hasName
                ? line.speaker.displayName
                : string.Empty;
    }
    private void SetupDialoguePortrait(
    TutorialDialogueLine line,
    string styleId
)
    {
        if (leftPortraitImage != null)
            leftPortraitImage.gameObject.SetActive(false);

        if (rightPortraitImage != null)
            rightPortraitImage.gameObject.SetActive(false);

        if (line == null)
            return;

        if (line.speaker == null)
            return;

        Sprite portrait =
            line.speaker.GetPortrait(styleId);

        if (portrait == null)
        {
            Debug.LogWarning(
                $"[TutorialUI] Speaker 沒有可用立繪：" +
                $"{line.speaker.displayName}",
                line.speaker
            );

            return;
        }

        DialoguePortraitSide side =
            line.GetPortraitSide();

        Image targetImage =
            side == DialoguePortraitSide.Right
                ? rightPortraitImage
                : leftPortraitImage;

        if (targetImage == null)
            return;

        targetImage.sprite = portrait;
        targetImage.preserveAspect = true;
        targetImage.gameObject.SetActive(true);
    }
    private void SetDialogueContinueIndicator(bool visible)
    {
        if (dialogueContinueIndicator == null)
            return;

        dialogueContinueIndicator.SetActive(
            visible
        );
    }
}