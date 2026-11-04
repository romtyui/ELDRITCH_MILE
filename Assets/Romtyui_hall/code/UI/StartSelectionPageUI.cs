using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class StartSelectionPageUI : MonoBehaviour
{
    [Header("主配置區")]
    [SerializeField] private RectTransform mainPanel;

    [Header("版面定位點")]
    [SerializeField] private RectTransform centerTarget;
    [SerializeField] private RectTransform rightTarget;
    [SerializeField] private RectTransform leftTarget;

    [Header("食物面板")]
    [SerializeField] private GameObject foodSelectionPanel;
    [SerializeField] private CanvasGroup foodPanelCanvasGroup;
    [SerializeField] private FoodSelectionUI foodSelectionUI;

    [Header("遺物面板")]
    [SerializeField] private GameObject relicSelectionPanel;
    [SerializeField] private CanvasGroup relicPanelCanvasGroup;
    [SerializeField] private RelicSelectionUI relicSelectionUI;

    [Header("開啟動畫")]
    [SerializeField, Min(0f), Tooltip("主配置區移動至選擇版面的時間。")]
    private float mainMoveDuration = 0.4f;

    [SerializeField, Min(0f), Tooltip("選擇面板展開及淡入的時間。")]
    private float panelExpandDuration = 0.3f;

    [Header("關閉動畫")]
    [SerializeField, Min(0f), Tooltip("選擇面板收起及淡出的時間。")]
    private float panelCollapseDuration = 0.25f;

    [SerializeField, Min(0f), Tooltip("主配置區返回中央的時間。")]
    private float mainReturnDuration = 0.4f;

    private int editingItemSlotIndex = -1;
    private int editingRelicSlotIndex = -1;
    private Coroutine openingCoroutine;
    private bool isOpening;
    private bool isClosing;

    private Vector3 foodPanelOriginalScale = Vector3.one;
    private Vector3 relicPanelOriginalScale = Vector3.one;

    private GameObject activePanel;
    private CanvasGroup activeCanvasGroup;
    private Vector3 activeOriginalScale = Vector3.one;

    public int EditingItemSlotIndex => editingItemSlotIndex;
    public int EditingRelicSlotIndex => editingRelicSlotIndex;
    public bool IsFoodSelectionOpen => foodSelectionPanel != null && foodSelectionPanel.activeSelf;
    public bool IsRelicSelectionOpen => relicSelectionPanel != null && relicSelectionPanel.activeSelf;

    private void Awake()
    {
        if (foodSelectionPanel != null)
        {
            foodPanelOriginalScale = foodSelectionPanel.transform.localScale;
            foodPanelCanvasGroup = PrepareCanvasGroup(foodSelectionPanel, foodPanelCanvasGroup);
        }

        if (relicSelectionPanel != null)
        {
            relicPanelOriginalScale = relicSelectionPanel.transform.localScale;
            relicPanelCanvasGroup = PrepareCanvasGroup(relicSelectionPanel, relicPanelCanvasGroup);
        }
    }

    private CanvasGroup PrepareCanvasGroup(GameObject panel, CanvasGroup group)
    {
        if (group == null) group = panel.GetComponent<CanvasGroup>();
        if (group == null) group = panel.AddComponent<CanvasGroup>();
        return group;
    }

    private void Start()
    {
        ResetClosedState();
    }

    private void OnDisable()
    {
        if (openingCoroutine != null) StopCoroutine(openingCoroutine);
        ResetClosedState();
    }

    public void OpenFoodSelection(int slotIndex)
    {
        if (slotIndex < 0)
        {
            Debug.LogWarning("道具格編號不可小於 0。", this);
            return;
        }

        if (mainPanel == null || rightTarget == null || foodSelectionPanel == null || foodSelectionUI == null)
        {
            Debug.LogWarning("請先指定食物面板、控制器、MainPanel 與 RightTarget。", this);
            return;
        }

        if (!ValidateLayout(rightTarget) || isOpening || isClosing) return;
        if (!foodSelectionUI.BeginSelection(slotIndex)) return;

        editingItemSlotIndex = slotIndex;
        editingRelicSlotIndex = -1;

        if (IsFoodSelectionOpen) return;

        isOpening = true;
        openingCoroutine = StartCoroutine(PlayOpeningAnimation(foodSelectionPanel, foodPanelCanvasGroup, foodPanelOriginalScale, rightTarget));
    }

    public void OpenRelicSelection(int slotIndex)
    {
        if (slotIndex < 0)
        {
            Debug.LogWarning("遺物格編號不可小於 0。", this);
            return;
        }

        if (mainPanel == null || leftTarget == null || relicSelectionPanel == null || relicSelectionUI == null)
        {
            Debug.LogWarning("請先指定遺物面板、控制器、MainPanel 與 LeftTarget。", this);
            return;
        }

        if (!ValidateLayout(leftTarget) || isOpening || isClosing) return;
        if (!relicSelectionUI.BeginSelection(slotIndex)) return;

        editingRelicSlotIndex = slotIndex;
        editingItemSlotIndex = -1;

        if (IsRelicSelectionOpen) return;

        isOpening = true;
        openingCoroutine = StartCoroutine(PlayOpeningAnimation(relicSelectionPanel, relicPanelCanvasGroup, relicPanelOriginalScale, leftTarget));
    }

    public void CloseFoodSelection()
    {
        if (editingItemSlotIndex < 0 && activePanel != foodSelectionPanel) return;
        BeginClose();
    }

    public void CloseRelicSelection()
    {
        if (editingRelicSlotIndex < 0 && activePanel != relicSelectionPanel) return;
        BeginClose();
    }

    private void BeginClose()
    {
        if (isClosing || !ValidateLayout(centerTarget)) return;

        if (openingCoroutine != null) StopCoroutine(openingCoroutine);

        openingCoroutine = null;
        isOpening = false;
        isClosing = true;
        openingCoroutine = StartCoroutine(PlayClosingAnimation());
    }

    private IEnumerator PlayOpeningAnimation(GameObject panel, CanvasGroup group, Vector3 originalScale, RectTransform target)
    {
        if (activePanel != null && activePanel != panel)
        {
            yield return AnimatePanel(activePanel, activeCanvasGroup, activeOriginalScale, false);
            activePanel.SetActive(false);
            activePanel.transform.localScale = activeOriginalScale;
        }

        activePanel = panel;
        activeCanvasGroup = group;
        activeOriginalScale = originalScale;

        yield return AnimateMainPanel(target, mainMoveDuration);
        yield return AnimatePanel(panel, group, originalScale, true);

        isOpening = false;
        openingCoroutine = null;
    }

    private IEnumerator PlayClosingAnimation()
    {
        if (activePanel != null && activePanel.activeSelf)
        {
            yield return AnimatePanel(activePanel, activeCanvasGroup, activeOriginalScale, false);
            activePanel.SetActive(false);
            activePanel.transform.localScale = activeOriginalScale;
        }

        yield return AnimateMainPanel(centerTarget, mainReturnDuration);
        ResetClosedState();
    }

    private IEnumerator AnimateMainPanel(RectTransform target, float duration)
    {
        Vector3 startPosition = mainPanel.anchoredPosition3D;
        Vector2 startSize = mainPanel.sizeDelta;
        Vector3 startScale = mainPanel.localScale;
        Quaternion startRotation = mainPanel.localRotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            mainPanel.anchoredPosition3D = Vector3.LerpUnclamped(startPosition, target.anchoredPosition3D, easedProgress);
            mainPanel.sizeDelta = Vector2.LerpUnclamped(startSize, target.sizeDelta, easedProgress);
            mainPanel.localScale = Vector3.LerpUnclamped(startScale, target.localScale, easedProgress);
            mainPanel.localRotation = Quaternion.Slerp(startRotation, target.localRotation, easedProgress);

            yield return null;
        }

        ApplyLayout(target);
    }

    private IEnumerator AnimatePanel(GameObject panel, CanvasGroup group, Vector3 originalScale, bool expand)
    {
        Vector3 startScale = panel.transform.localScale;
        float startAlpha = group != null ? group.alpha : 1f;

        if (expand)
        {
            startScale = originalScale;
            startScale.x = 0f;
            startAlpha = 0f;
            panel.transform.localScale = startScale;
        }

        if (group != null)
        {
            group.alpha = startAlpha;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        if (expand) panel.SetActive(true);

        float duration = expand ? panelExpandDuration : panelCollapseDuration;
        float elapsed = 0f;
        Vector3 endScale = originalScale;
        if (!expand) endScale.x = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = expand ? 1f - Mathf.Pow(1f - progress, 3f) : Mathf.SmoothStep(0f, 1f, progress);

            panel.transform.localScale = Vector3.LerpUnclamped(startScale, endScale, easedProgress);
            if (group != null) group.alpha = Mathf.Lerp(startAlpha, expand ? 1f : 0f, easedProgress);

            yield return null;
        }

        panel.transform.localScale = endScale;

        if (group != null)
        {
            group.alpha = expand ? 1f : 0f;
            group.interactable = expand;
            group.blocksRaycasts = expand;
        }
    }

    private void ResetClosedState()
    {
        openingCoroutine = null;
        isOpening = false;
        isClosing = false;
        editingItemSlotIndex = -1;
        editingRelicSlotIndex = -1;
        activePanel = null;
        activeCanvasGroup = null;
        activeOriginalScale = Vector3.one;

        ResetPanel(foodSelectionPanel, foodPanelCanvasGroup, foodPanelOriginalScale);
        ResetPanel(relicSelectionPanel, relicPanelCanvasGroup, relicPanelOriginalScale);
        ApplyLayout(centerTarget);
    }

    private void ResetPanel(GameObject panel, CanvasGroup group, Vector3 originalScale)
    {
        if (panel != null)
        {
            panel.SetActive(false);
            panel.transform.localScale = originalScale;
        }

        if (group != null)
        {
            group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }

    private bool ValidateLayout(RectTransform target)
    {
        if (mainPanel == null || target == null) return false;

        if (mainPanel == target)
        {
            Debug.LogWarning("版面定位點不可指定為 MainPanel 本身。", this);
            return false;
        }

        if (mainPanel.parent != target.parent)
        {
            Debug.LogWarning("MainPanel 與版面定位點必須放在同一個父物件下。", this);
            return false;
        }

        return true;
    }

    private bool ApplyLayout(RectTransform target)
    {
        if (!ValidateLayout(target)) return false;

        mainPanel.anchorMin = target.anchorMin;
        mainPanel.anchorMax = target.anchorMax;
        mainPanel.pivot = target.pivot;
        mainPanel.sizeDelta = target.sizeDelta;
        mainPanel.anchoredPosition3D = target.anchoredPosition3D;
        mainPanel.localRotation = target.localRotation;
        mainPanel.localScale = target.localScale;
        return true;
    }

    [ContextMenu("預覽中央版面")]
    private void PreviewCenterLayout()
    {
        ApplyLayout(centerTarget);
    }

    [ContextMenu("預覽右側版面")]
    private void PreviewRightLayout()
    {
        ApplyLayout(rightTarget);
    }

    [ContextMenu("預覽左側版面")]
    private void PreviewLeftLayout()
    {
        ApplyLayout(leftTarget);
    }
}