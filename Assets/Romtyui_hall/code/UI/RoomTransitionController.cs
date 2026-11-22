using System.Collections;
using UnityEngine;

public class RoomTransitionController : MonoBehaviour
{
    [Header("房間設定")]
    [Tooltip("離開時要關閉的房間，例如 Hall。")]
    [SerializeField] private GameObject currentRoom;



    [Header("攝影機設定")]
    [Tooltip("實際顯示房間的攝影機。")]
    [SerializeField] private Camera sceneCamera;



    [Header("走入效果")]
    [Tooltip("走向門口的總時間，後段會同時淡黑。")]
    [Min(0.01f)]
    [SerializeField] private float approachDuration = 1.4f;

    [Tooltip("走向門口時的腳步起伏次數，可以使用小數。")]
    [Min(0f)]
    [SerializeField] private float walkStepCount = 2.5f;

    [Tooltip("上下起伏的幅度，單位為世界座標。")]
    [Min(0f)]
    [SerializeField] private float walkBobHeight = 0.04f;

    [Tooltip("左右晃動的幅度，單位為世界座標。")]
    [Min(0f)]
    [SerializeField] private float walkSwayWidth = 0.025f;

    [Tooltip("正交攝影機使用的放大倍率，模擬逐漸接近門口。")]
    [Min(1f)]
    [SerializeField] private float zoomMultiplier = 1.8f;

    [Tooltip("透視攝影機沿觀看方向前進到門口距離的比例。正交攝影機不使用此設定。")]
    [Range(0f, 0.9f)]
    [SerializeField] private float perspectiveForwardRatio = 0.6f;

    [Header("黑幕設定")]
    [Tooltip("全螢幕黑色 Image 上的 CanvasGroup。")]
    [SerializeField] private CanvasGroup fadeGroup;

    [Tooltip("走入過程進行到多少比例時開始淡黑。0.65 代表到 65% 時開始。")]
    [Range(0f, 0.95f)]
    [SerializeField] private float fadeStartRatio = 0.65f;

    [Tooltip("完全變黑後，在切換房間前停留的時間。")]
    [Min(0f)]
    [SerializeField] private float blackHoldDuration = 0.05f;

    [Tooltip("新房間從黑色淡入的時間。")]
    [Min(0.01f)]
    [SerializeField] private float fadeInDuration = 0.35f;

    private bool isTransitioning;

    private void Awake()
    {
        if (fadeGroup == null) return;
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;
        fadeGroup.interactable = false;
    }

    public void EnterRoom(GameObject destinationRoom, Transform entrancePoint, Transform destinationCameraPoint)
    {
        if (!isActiveAndEnabled || isTransitioning) return;

        if (currentRoom == null || destinationRoom == null || sceneCamera == null || entrancePoint == null || fadeGroup == null)
        {
            Debug.LogWarning("RoomTransitionController：請確認目前房間、目的地房間、攝影機、入口位置與黑幕 CanvasGroup 都已指定。", this);
            return;
        }

        if (currentRoom == destinationRoom) return;

        isTransitioning = true;
        StartCoroutine(TransitionRoutine(destinationRoom, entrancePoint, destinationCameraPoint));
    }

    private IEnumerator TransitionRoutine(GameObject destinationRoom, Transform entrancePoint, Transform destinationCameraPoint)
    {
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = true;

        GameObject sourceRoom = currentRoom;
        Transform cameraTransform = sceneCamera.transform;
        Vector3 originalPosition = cameraTransform.position;
        Quaternion originalRotation = cameraTransform.rotation;
        float originalSize = sceneCamera.orthographicSize;
        bool isOrthographic = sceneCamera.orthographic;

        Vector3 cameraRight = cameraTransform.right;
        Vector3 cameraUp = cameraTransform.up;
        Vector3 cameraForward = cameraTransform.forward;

        Vector3 offset = entrancePoint.position - originalPosition;
        Vector3 lateralOffset = Vector3.ProjectOnPlane(offset, cameraForward);
        Vector3 approachPosition = originalPosition + lateralOffset;

        if (!isOrthographic)
        {
            float forwardDistance = Mathf.Max(0f, Vector3.Dot(offset, cameraForward));
            approachPosition += cameraForward * forwardDistance * Mathf.Clamp(perspectiveForwardRatio, 0f, 0.9f);
        }

        // 在關閉來源房間前，先記住目的地的觀看位置。
        Vector3 destinationPosition = destinationCameraPoint != null ? destinationCameraPoint.position : originalPosition;
        Quaternion destinationRotation = destinationCameraPoint != null ? destinationCameraPoint.rotation : originalRotation;

        float targetSize = originalSize / Mathf.Max(1f, zoomMultiplier);
        float duration = Mathf.Max(0.01f, approachDuration);
        float fadeStart = Mathf.Clamp(fadeStartRatio, 0f, 0.95f);
        float stepCount = Mathf.Max(0f, walkStepCount);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float moveProgress = Mathf.SmoothStep(0f, 1f, progress);

            // 起步與停步時逐漸減少晃動。
            float walkEnvelope = Mathf.Sin(progress * Mathf.PI);
            float stepPhase = moveProgress * stepCount * Mathf.PI * 2f;
            float verticalOffset = 0f;
            float horizontalOffset = 0f;

            if (stepCount > 0f)
            {
                verticalOffset = (Mathf.Cos(stepPhase) - 1f) * 0.5f * walkBobHeight * walkEnvelope;
                horizontalOffset = Mathf.Sin(stepPhase * 0.5f) * walkSwayWidth * walkEnvelope;
            }

            Vector3 basePosition = Vector3.Lerp(originalPosition, approachPosition, moveProgress);
            cameraTransform.position = basePosition + cameraUp * verticalOffset + cameraRight * horizontalOffset;

            // 正交攝影機以縮小 Size 模擬前進。
            if (isOrthographic) sceneCamera.orthographicSize = Mathf.Lerp(originalSize, targetSize, moveProgress);

            float fadeProgress = Mathf.InverseLerp(fadeStart, 1f, progress);
            fadeGroup.alpha = Mathf.SmoothStep(0f, 1f, fadeProgress);

            yield return null;
        }

        cameraTransform.position = approachPosition;
        fadeGroup.alpha = 1f;

        if (blackHoldDuration > 0f) yield return new WaitForSecondsRealtime(blackHoldDuration);

        sourceRoom.SetActive(false);

        cameraTransform.SetPositionAndRotation(destinationPosition, destinationRotation);
        sceneCamera.orthographicSize = originalSize;

        currentRoom = destinationRoom;
        destinationRoom.SetActive(true);

        // 讓新房間先完成啟用與第一幀更新，再開始淡入。
        yield return null;

        duration = Mathf.Max(0.01f, fadeInDuration);
        elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            fadeGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, progress);
            yield return null;
        }

        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;
        isTransitioning = false;
    }
}