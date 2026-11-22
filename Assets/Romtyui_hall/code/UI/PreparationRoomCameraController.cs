using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PreparationRoomCameraController : MonoBehaviour
{
    [Header("攝影機")]
    [Tooltip("實際顯示準備室的攝影機。")]
    [SerializeField] private Camera sceneCamera;

    [Header("視角設定")]
    [Tooltip("食物視角相對於中央朝向的 Y 軸偏移角度。正負方向請依場景配置調整。")]
    [SerializeField] private float foodYawOffset = -30f;

    [Tooltip("遺物視角相對於中央朝向的 Y 軸偏移角度。正負方向請依場景配置調整。")]
    [SerializeField] private float relicYawOffset = 30f;

    private Quaternion centerRotation;
    private Coroutine rotationCoroutine;
    private bool hasCenterRotation;

    private void OnEnable()
    {
        hasCenterRotation = false;

        if (sceneCamera == null)
        {
            Debug.LogWarning("PreparationRoomCameraController：請指定準備室使用的攝影機。", this);
            return;
        }

        centerRotation = sceneCamera.transform.rotation;
        hasCenterRotation = true;
    }

    private void OnDisable()
    {
        StopRotation();

        if (sceneCamera == null || !hasCenterRotation) return;

        sceneCamera.transform.rotation = centerRotation;
        hasCenterRotation = false;
    }

    public void LookFood(float duration)
    {
        TurnToYaw(foodYawOffset, duration);
    }

    public void LookRelic(float duration)
    {
        TurnToYaw(relicYawOffset, duration);
    }

    public void LookCenter(float duration)
    {
        TurnToYaw(0f, duration);
    }

    public void ResetView()
    {
        if (!isActiveAndEnabled || sceneCamera == null || !hasCenterRotation) return;

        StopRotation();
        sceneCamera.transform.rotation = centerRotation;
    }

    private void TurnToYaw(float yawOffset, float duration)
    {
        if (!isActiveAndEnabled || sceneCamera == null || !hasCenterRotation) return;

        StopRotation();

        Quaternion targetRotation = Quaternion.AngleAxis(yawOffset, Vector3.up) * centerRotation;

        if (duration <= 0f)
        {
            sceneCamera.transform.rotation = targetRotation;
            return;
        }

        rotationCoroutine = StartCoroutine(RotateRoutine(targetRotation, duration));
    }

    private IEnumerator RotateRoutine(Quaternion targetRotation, float duration)
    {
        Quaternion startRotation = sceneCamera.transform.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            sceneCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, easedProgress);
            yield return null;
        }

        sceneCamera.transform.rotation = targetRotation;
        rotationCoroutine = null;
    }

    private void StopRotation()
    {
        if (rotationCoroutine == null) return;

        StopCoroutine(rotationCoroutine);
        rotationCoroutine = null;
    }
}