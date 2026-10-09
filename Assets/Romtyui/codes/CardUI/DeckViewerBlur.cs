using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DeckViewerBlur : MonoBehaviour
{
    [Tooltip("預設開啟的面板。按鈕呼叫 Open() 時會開啟此面板。自動背景模式可以留空。")]
    [SerializeField] private GameObject deckViewerPanel;

    [Tooltip("顯示背景截圖的 RawImage，使用原本的模糊材質。")]
    [SerializeField] private RawImage blurBackground;

    [Header("畫面啟用時自動模糊")]
    [Tooltip("啟用此物件時，自動擷取背景。原本由按鈕開啟的控制器保持關閉。")]
    [SerializeField] private bool captureOnEnable = false;

    [Tooltip("截圖時要排除的畫面。留空時使用此腳本所在的物件。")]
    [SerializeField] private GameObject captureExcludedPanel;

    [Header("截圖時額外隱藏")]
    [Tooltip("截圖時暫時隱藏的物件。截圖完成、取消或停用時，恢復各自原本的透明度。")]
    [SerializeField] private List<GameObject> extraObjectsToHide = new List<GameObject>();

    private readonly List<CanvasGroup> extraHiddenGroups = new List<CanvasGroup>();
    private readonly List<float> extraOriginalAlphas = new List<float>();

    private RenderTexture capturedScreen;
    private Coroutine captureRoutine;
    private GameObject currentPanel;

    private CanvasGroup excludedGroup;
    private float excludedOriginalAlpha;
    private bool temporarilyHidden;

    public void Open()
    {
        OpenPanel(deckViewerPanel);
    }

    public void OpenPanel(GameObject targetPanel)
    {
        if (targetPanel == null || blurBackground == null) return;

        Close();

        currentPanel = targetPanel;
        currentPanel.SetActive(false);
        captureRoutine = StartCoroutine(CaptureAndOpen(targetPanel));
    }

    public void CaptureBackground(GameObject panelToExclude)
    {
        if (panelToExclude == null || blurBackground == null) return;

        if (captureRoutine != null)
        {
            StopCoroutine(captureRoutine);
            captureRoutine = null;
        }

        RestoreExcludedPanel();

        blurBackground.texture = null;
        if (capturedScreen != null) Destroy(capturedScreen);
        capturedScreen = null;

        excludedGroup = panelToExclude.GetComponent<CanvasGroup>();
        if (excludedGroup == null) excludedGroup = panelToExclude.AddComponent<CanvasGroup>();

        excludedOriginalAlpha = excludedGroup.alpha;
        temporarilyHidden = true;
        excludedGroup.alpha = 0f;

        if (extraObjectsToHide != null)
        {
            for (int i = 0; i < extraObjectsToHide.Count; i++)
            {
                GameObject target = extraObjectsToHide[i];
                if (target == null) continue;

                CanvasGroup group = target.GetComponent<CanvasGroup>();
                if (group == null) group = target.AddComponent<CanvasGroup>();
                if (group == excludedGroup || extraHiddenGroups.Contains(group)) continue;

                extraHiddenGroups.Add(group);
                extraOriginalAlphas.Add(group.alpha);
                group.alpha = 0f;
            }
        }

        captureRoutine = StartCoroutine(CaptureBackgroundRoutine(panelToExclude));
    }

    public void Close()
    {
        Debug.Log($"[DeckViewerBlur] {name}：Close 清空背景。\n{System.Environment.StackTrace}", this);

        if (captureRoutine != null)
        {
            StopCoroutine(captureRoutine);
            captureRoutine = null;
        }

        RestoreExcludedPanel();

        if (currentPanel != null) currentPanel.SetActive(false);
        if (deckViewerPanel != null) deckViewerPanel.SetActive(false);
        if (blurBackground != null) blurBackground.texture = null;
        if (capturedScreen != null) Destroy(capturedScreen);

        capturedScreen = null;
        currentPanel = null;
    }

    private IEnumerator CaptureAndOpen(GameObject targetPanel)
    {
        yield return new WaitForEndOfFrame();

        if (targetPanel == null || blurBackground == null)
        {
            currentPanel = null;
            captureRoutine = null;
            yield break;
        }

        int width = Screen.width;
        int height = Screen.height;
        RenderTexture screenshot = RenderTexture.GetTemporary(width, height, 0);
        ScreenCapture.CaptureScreenshotIntoRenderTexture(screenshot);

        capturedScreen = new RenderTexture(width, height, 0);
        Graphics.Blit(screenshot, capturedScreen, new Vector2(1f, -1f), new Vector2(0f, 1f));

        RenderTexture.ReleaseTemporary(screenshot);

        blurBackground.texture = capturedScreen;
        targetPanel.SetActive(true);
        captureRoutine = null;

        DeckViewerUI viewer = targetPanel.GetComponentInChildren<DeckViewerUI>(true);
        if (viewer != null) viewer.ReplayCurrentTabAnimation();
    }

    private IEnumerator CaptureBackgroundRoutine(GameObject panelToExclude)
    {
        yield return new WaitForEndOfFrame();

        if (panelToExclude == null || !panelToExclude.activeInHierarchy || blurBackground == null)
        {
            RestoreExcludedPanel();
            captureRoutine = null;
            yield break;
        }

        int width = Screen.width;
        int height = Screen.height;
        RenderTexture screenshot = RenderTexture.GetTemporary(width, height, 0);
        ScreenCapture.CaptureScreenshotIntoRenderTexture(screenshot);

        capturedScreen = new RenderTexture(width, height, 0);
        Graphics.Blit(screenshot, capturedScreen, new Vector2(1f, -1f), new Vector2(0f, 1f));

        RenderTexture.ReleaseTemporary(screenshot);

        blurBackground.texture = capturedScreen;
        RestoreExcludedPanel();
        captureRoutine = null;
    }

    private void RestoreExcludedPanel()
    {
        for (int i = extraHiddenGroups.Count - 1; i >= 0; i--)
        {
            CanvasGroup group = extraHiddenGroups[i];
            if (group == null) continue;

            group.alpha = extraOriginalAlphas[i];
        }

        extraHiddenGroups.Clear();
        extraOriginalAlphas.Clear();

        if (temporarilyHidden && excludedGroup != null) excludedGroup.alpha = excludedOriginalAlpha;

        excludedGroup = null;
        temporarilyHidden = false;
    }

    private void OnEnable()
    {
        if (!captureOnEnable) return;

        GameObject targetPanel = captureExcludedPanel != null ? captureExcludedPanel : gameObject;
        CaptureBackground(targetPanel);
    }
    private void OnDisable()
    {
        if (!captureOnEnable) return;

        Debug.Log($"[DeckViewerBlur] {name}：OnDisable 清空背景。\n{System.Environment.StackTrace}", this);


        if (captureRoutine != null)
        {
            StopCoroutine(captureRoutine);
            captureRoutine = null;
        }

        RestoreExcludedPanel();

        if (blurBackground != null) blurBackground.texture = null;
        if (capturedScreen != null) Destroy(capturedScreen);

        capturedScreen = null;
    }
}