using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DeckViewerBlur : MonoBehaviour
{
    [SerializeField] private GameObject deckViewerPanel;
    [SerializeField] private RawImage blurBackground;

    private RenderTexture capturedScreen;
    private Coroutine captureRoutine;

    public void Open()
    {
        if (deckViewerPanel == null || blurBackground == null) return;

        Close();
        captureRoutine = StartCoroutine(CaptureAndOpen());
    }

    public void Close()
    {
        if (captureRoutine != null)
        {
            StopCoroutine(captureRoutine);
            captureRoutine = null;
        }

        if (deckViewerPanel != null) deckViewerPanel.SetActive(false);
        if (blurBackground != null) blurBackground.texture = null;
        if (capturedScreen != null) Destroy(capturedScreen);
        capturedScreen = null;
    }

    private IEnumerator CaptureAndOpen()
    {
        yield return new WaitForEndOfFrame();

        int width = Screen.width;
        int height = Screen.height;
        RenderTexture screenshot = RenderTexture.GetTemporary(width, height, 0);
        ScreenCapture.CaptureScreenshotIntoRenderTexture(screenshot);

        capturedScreen = new RenderTexture(width, height, 0);
        Graphics.Blit(screenshot, capturedScreen, new Vector2(1f, -1f), new Vector2(0f, 1f));

        RenderTexture.ReleaseTemporary(screenshot);

        blurBackground.texture = capturedScreen;
        deckViewerPanel.SetActive(true);
        captureRoutine = null;
    }

    private void OnDestroy()
    {
        Close();
    }
}