using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MenuToHallTransition : MonoBehaviour
{
    [Header("開始畫面")]
    [SerializeField, Tooltip("開始畫面的總根物件，包含場景背景與開始畫面 UI，轉場變黑後關閉。")]
    private GameObject mainMenuRoot;

    [SerializeField, Tooltip("OptionButton_Root 上的 CanvasGroup，用來讓四個按鈕淡出。")]
    private CanvasGroup menuButtonsGroup;

    [Header("開始畫面鏡頭放大")]
    [SerializeField, Range(1f, 179f), Tooltip("開始畫面放大後的 Field of View。必須小於攝影機原本的 FOV，才會呈現放大。")]
    private float menuZoomFov = 25f;

    [SerializeField, Tooltip("是否在放大時，讓鏡頭逐漸轉向指定的世界座標。")]
    private bool turnTowardMenuFocus = true;

    [SerializeField, Tooltip("開始畫面希望移向畫面中央的目標世界座標，例如椅子旁打勾的位置。")]
    private Vector3 menuFocusPosition;

    [SerializeField, Min(0.01f), Tooltip("開始畫面縮小 FOV 與轉向目標的時間。")]
    private float menuZoomDuration = 1.1f;

    [SerializeField, Min(0.01f), Tooltip("開始畫面按鈕淡出的時間。")]
    private float buttonFadeDuration = 0.2f;

    [SerializeField, Range(0f, 0.95f), Tooltip("開始畫面放大進行到多少比例時開始淡黑。")]
    private float blackStartRatio = 0.55f;

    [SerializeField, Min(0f), Tooltip("完全變黑後，在切換大廳前停留的時間。")]
    private float blackHoldDuration = 0.15f;

    [Header("大廳")]
    [SerializeField, Tooltip("測試場景內的大廳總根物件，包含 Hall、Preparation Room 等內容。")]
    private GameObject lobbyRoot;

    [SerializeField, Tooltip("顯示開始畫面背景與大廳的透視攝影機。")]
    private Camera sceneCamera;

    [SerializeField, Tooltip("轉場結束時，攝影機在大廳的世界座標位置。")]
    private Vector3 hallViewPosition;

    [SerializeField, Tooltip("轉場結束時，攝影機在大廳的世界旋轉角度，單位為度。")]
    private Vector3 hallViewEulerAngles;

    [Header("大廳 FOV 效果")]
    [UnityEngine.Serialization.FormerlySerializedAs("hallStartFov")]
    [SerializeField, Range(1f, 179f), Tooltip("進入大廳後，視野收縮到的最小 Field of View。")]
    private float hallMinFov = 40f;

    [SerializeField, Range(1f, 179f), Tooltip("大廳正常觀看時的 Field of View。進場時會從此值縮小，再恢復此值。")]
    private float hallFinalFov = 60f;

    [SerializeField, Range(0.05f, 0.95f), Tooltip("大廳進場動畫中，用多少時間比例讓 FOV 從正常值縮小。剩餘時間恢復正常。")]
    private float fovShrinkRatio = 0.35f;

    [Header("低頭抬頭效果")]
    [SerializeField, Range(0f, 60f), Tooltip("剛進入大廳時，相對正常朝向的低頭角度。")]
    private float lookDownAngle = 18f;

    [SerializeField, Min(0.01f), Tooltip("低頭抬回正前方、FOV 先縮小再恢復，以及暗角消失的時間。")]
    private float hallRevealDuration = 1.3f;

    [SerializeField, Min(0.01f), Tooltip("進入大廳後，全黑遮罩淡出的時間。")]
    private float blackRevealDuration = 0.45f;

    [Header("轉場遮罩")]
    [SerializeField, Tooltip("BlackImage 上的 CanvasGroup。")]
    private CanvasGroup blackGroup;

    [SerializeField, Tooltip("全螢幕 RawImage。腳本會自動建立中央透明、四周黑色的暗角。")]
    private RawImage vignetteImage;

    [SerializeField, Range(0f, 1f), Tooltip("剛進入大廳時的暗角強度。")]
    private float initialVignetteAlpha = 0.9f;

    [Header("攝影機控制")]
    [SerializeField, Tooltip("轉場期間需要暫停的鏡頭跟隨、邊緣移動等控制腳本。不要放入本腳本。")]
    private Behaviour[] cameraControlsToPause;

    [Header("完成事件")]
    [SerializeField, Tooltip("鏡頭回到大廳正常視角，且遮罩消失後觸發。")]
    private UnityEvent onTransitionCompleted = new UnityEvent();

    private bool isPlaying;
    private Texture2D vignetteTexture;
    private bool[] cameraControlStates;

    private void Awake()
    {
        if (blackGroup != null)
        {
            blackGroup.alpha = 0f;
            blackGroup.interactable = false;
            blackGroup.blocksRaycasts = false;
        }

        if (vignetteImage != null)
        {
            vignetteTexture = CreateVignetteTexture();
            vignetteImage.texture = vignetteTexture;
            vignetteImage.raycastTarget = false;
            SetVignetteAlpha(0f);
        }
    }

    private void OnDestroy()
    {
        if (vignetteTexture != null) Destroy(vignetteTexture);
    }

    public void PlayTransition()
    {
        if (!isActiveAndEnabled || isPlaying) return;

        if (mainMenuRoot == null || lobbyRoot == null || sceneCamera == null || blackGroup == null || vignetteImage == null)
        {
            Debug.LogWarning("MenuToHallTransition：請先指定開始畫面、大廳、攝影機與兩個遮罩。", this);
            return;
        }

        if (sceneCamera.orthographic)
        {
            Debug.LogWarning("MenuToHallTransition：攝影機需要使用 Perspective，才能透過 Field of View 放大畫面。", this);
            return;
        }

        isPlaying = true;
        StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        PauseCameraControls();

        blackGroup.alpha = 0f;
        blackGroup.blocksRaycasts = true;
        SetVignetteAlpha(0f);

        Quaternion originalMenuRotation = sceneCamera.transform.rotation;
        Quaternion targetMenuRotation = originalMenuRotation;
        float originalMenuFov = sceneCamera.fieldOfView;
        float targetMenuFov = Mathf.Clamp(menuZoomFov, 1f, originalMenuFov);

        if (turnTowardMenuFocus)
        {
            Vector3 focusDirection = menuFocusPosition - sceneCamera.transform.position;

            if (focusDirection.sqrMagnitude > 0.0001f)
            {
                Vector3 focusUp = sceneCamera.transform.up;

                if (Vector3.Cross(focusDirection.normalized, focusUp).sqrMagnitude < 0.0001f) focusUp = sceneCamera.transform.right;

                targetMenuRotation = Quaternion.LookRotation(focusDirection.normalized, focusUp);
            }
        }

        float originalButtonAlpha = menuButtonsGroup != null ? menuButtonsGroup.alpha : 1f;
        bool originalButtonInteractable = menuButtonsGroup != null && menuButtonsGroup.interactable;
        bool originalButtonBlocksRaycasts = menuButtonsGroup != null && menuButtonsGroup.blocksRaycasts;

        if (menuButtonsGroup != null)
        {
            menuButtonsGroup.interactable = false;
            menuButtonsGroup.blocksRaycasts = false;
        }

        float duration = Mathf.Max(0.01f, menuZoomDuration);
        float fadeStart = Mathf.Clamp(blackStartRatio, 0f, 0.95f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            sceneCamera.transform.rotation = Quaternion.Slerp(originalMenuRotation, targetMenuRotation, easedProgress);
            sceneCamera.fieldOfView = Mathf.Lerp(originalMenuFov, targetMenuFov, easedProgress);

            if (menuButtonsGroup != null) menuButtonsGroup.alpha = originalButtonAlpha * (1f - Mathf.Clamp01(elapsed / Mathf.Max(0.01f, buttonFadeDuration)));

            float blackProgress = Mathf.InverseLerp(fadeStart, 1f, progress);
            blackGroup.alpha = Mathf.SmoothStep(0f, 1f, blackProgress);

            yield return null;
        }

        sceneCamera.transform.rotation = targetMenuRotation;
        sceneCamera.fieldOfView = targetMenuFov;
        blackGroup.alpha = 1f;

        if (blackHoldDuration > 0f) yield return new WaitForSecondsRealtime(blackHoldDuration);

        Vector3 finalCameraPosition = hallViewPosition;
        Quaternion finalCameraRotation = Quaternion.Euler(hallViewEulerAngles);
        Quaternion downRotation = finalCameraRotation * Quaternion.Euler(lookDownAngle, 0f, 0f);

        mainMenuRoot.SetActive(false);

        // 開始畫面隱藏後，還原按鈕狀態。
        if (menuButtonsGroup != null)
        {
            menuButtonsGroup.alpha = originalButtonAlpha;
            menuButtonsGroup.interactable = originalButtonInteractable;
            menuButtonsGroup.blocksRaycasts = originalButtonBlocksRaycasts;
        }

        // 先設定正常大廳朝向，再啟用大廳。
        sceneCamera.transform.SetPositionAndRotation(finalCameraPosition, finalCameraRotation);
        sceneCamera.fieldOfView = hallFinalFov;
        lobbyRoot.SetActive(true);

        // 大廳從低頭與正常 FOV 開始。
        sceneCamera.transform.SetPositionAndRotation(finalCameraPosition, downRotation);
        sceneCamera.fieldOfView = hallFinalFov;
        SetVignetteAlpha(initialVignetteAlpha);

        yield return null;

        duration = Mathf.Max(0.01f, hallRevealDuration);
        float revealBlackDuration = Mathf.Max(0.01f, blackRevealDuration);
        float totalDuration = Mathf.Max(duration, revealBlackDuration);
        float shrinkRatio = Mathf.Clamp(fovShrinkRatio, 0.05f, 0.95f);
        float minimumFov = Mathf.Clamp(hallMinFov, 1f, hallFinalFov);
        elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            sceneCamera.transform.SetPositionAndRotation(finalCameraPosition, Quaternion.Slerp(downRotation, finalCameraRotation, easedProgress));

            if (progress <= shrinkRatio)
            {
                float shrinkProgress = Mathf.Clamp01(progress / shrinkRatio);
                float easedShrinkProgress = Mathf.SmoothStep(0f, 1f, shrinkProgress);
                sceneCamera.fieldOfView = Mathf.Lerp(hallFinalFov, minimumFov, easedShrinkProgress);
            }
            else
            {
                float restoreProgress = Mathf.Clamp01((progress - shrinkRatio) / (1f - shrinkRatio));
                float easedRestoreProgress = Mathf.SmoothStep(0f, 1f, restoreProgress);
                sceneCamera.fieldOfView = Mathf.Lerp(minimumFov, hallFinalFov, easedRestoreProgress);
            }

            float blackProgress = Mathf.Clamp01(elapsed / revealBlackDuration);
            blackGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, blackProgress);
            SetVignetteAlpha(initialVignetteAlpha * (1f - easedProgress));

            yield return null;
        }

        sceneCamera.transform.SetPositionAndRotation(finalCameraPosition, finalCameraRotation);
        sceneCamera.fieldOfView = hallFinalFov;

        blackGroup.alpha = 0f;
        blackGroup.blocksRaycasts = false;
        SetVignetteAlpha(0f);

        RestoreCameraControls();

        isPlaying = false;
        onTransitionCompleted.Invoke();
    }

    private void SetVignetteAlpha(float alpha)
    {
        if (vignetteImage == null) return;
        vignetteImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
    }

    private Texture2D CreateVignetteTexture()
    {
        const int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "MenuTransitionVignette";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = x / (size - 1f) * 2f - 1f;
                float ny = y / (size - 1f) * 2f - 1f;
                float radius = Mathf.Sqrt(nx * nx + ny * ny);
                float edgeProgress = Mathf.InverseLerp(0.45f, 1f, radius);
                float alpha = Mathf.SmoothStep(0f, 1f, edgeProgress);
                pixels[y * size + x] = new Color(0f, 0f, 0f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private void PauseCameraControls()
    {
        if (cameraControlsToPause == null) return;

        cameraControlStates = new bool[cameraControlsToPause.Length];

        for (int i = 0; i < cameraControlsToPause.Length; i++)
        {
            Behaviour control = cameraControlsToPause[i];
            if (control == null || control == this) continue;

            cameraControlStates[i] = control.enabled;
            control.enabled = false;
        }
    }

    private void RestoreCameraControls()
    {
        if (cameraControlsToPause == null || cameraControlStates == null) return;

        int count = Mathf.Min(cameraControlsToPause.Length, cameraControlStates.Length);

        for (int i = 0; i < count; i++)
        {
            Behaviour control = cameraControlsToPause[i];
            if (control == null || control == this) continue;

            control.enabled = cameraControlStates[i];
        }

        cameraControlStates = null;
    }
}