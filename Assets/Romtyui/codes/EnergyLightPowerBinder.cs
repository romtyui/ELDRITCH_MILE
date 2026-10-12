using System.Collections;
using UnityEngine;

public class EnergyLightPowerBinder : MonoBehaviour
{
    [Header("Refs")]
    public EnergySystem energySystem;
    public PSBMonsterLightReveal lightReveal;

    [Header("Light Power Mapping")]
    [Tooltip("SAN 為 0 時的最低燈光亮度。")]
    [Range(0f, 1f)]
    public float minLightPower = 0.25f;

    [Tooltip("SAN 全滿時的最高燈光亮度。")]
    [Range(0f, 1f)]
    public float maxLightPower = 1f;

    [Header("Response Curve")]
    [Tooltip("只影響實際燈光。數值越大，SAN 下降時燈光越快變暗。")]
    public float lightResponsePower = 2f;

    [Header("Smooth")]
    public bool smoothChange = true;
    public float smoothDuration = 0.25f;

    private Coroutine smoothRoutine;
    private bool hasInitialized;

    private void Awake()
    {
        if (energySystem == null)
            energySystem = FindFirstObjectByType<EnergySystem>();

        if (lightReveal == null)
            lightReveal = FindFirstObjectByType<PSBMonsterLightReveal>();
    }

    private void OnEnable()
    {
        if (energySystem == null)
            energySystem = FindFirstObjectByType<EnergySystem>();

        if (lightReveal == null)
            lightReveal = FindFirstObjectByType<PSBMonsterLightReveal>();

        if (energySystem != null)
            energySystem.OnEnergyChanged += RefreshLightPower;

        RefreshLightPower();
    }

    private void OnDisable()
    {
        if (energySystem != null)
            energySystem.OnEnergyChanged -= RefreshLightPower;

        if (smoothRoutine != null)
        {
            StopCoroutine(smoothRoutine);
            smoothRoutine = null;
        }
    }

    public void RefreshLightPower()
    {
        if (energySystem == null || lightReveal == null)
            return;

        float sanRatio = 0f;

        if (energySystem.maxEnergy > 0)
            sanRatio = Mathf.Clamp01((float)energySystem.currentEnergy / energySystem.maxEnergy);

        float responsePower = Mathf.Max(0.01f, lightResponsePower);
        float curvedEnergy01 = Mathf.Pow(sanRatio, responsePower);
        float targetLightPower = Mathf.Lerp(minLightPower, maxLightPower, curvedEnergy01);

        if (!hasInitialized || !smoothChange || smoothDuration <= 0f)
        {
            StopSmoothRoutine();
            lightReveal.SetSanRatio(sanRatio);
            lightReveal.SetLightPower(targetLightPower);
            hasInitialized = true;
        }
        else
        {
            SmoothSetValues(sanRatio, targetLightPower);
        }

        Debug.Log($"[EnergyLight] SAN = {energySystem.currentEnergy}/{energySystem.maxEnergy}, sanRatio = {sanRatio:F3}, curved = {curvedEnergy01:F3}, lightPower = {targetLightPower:F3}");
    }

    private void SmoothSetValues(float targetSanRatio, float targetLightPower)
    {
        StopSmoothRoutine();
        smoothRoutine = StartCoroutine(SmoothSetValuesRoutine(targetSanRatio, targetLightPower));
    }

    private IEnumerator SmoothSetValuesRoutine(float targetSanRatio, float targetLightPower)
    {
        float startSanRatio = lightReveal.sanRatio;
        float startLightPower = lightReveal.lightPower;
        float timer = 0f;

        while (timer < smoothDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / smoothDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            float currentSanRatio = Mathf.Lerp(startSanRatio, targetSanRatio, smoothT);
            float currentLightPower = Mathf.Lerp(startLightPower, targetLightPower, smoothT);

            lightReveal.SetSanRatio(currentSanRatio);
            lightReveal.SetLightPower(currentLightPower);

            yield return null;
        }

        lightReveal.SetSanRatio(targetSanRatio);
        lightReveal.SetLightPower(targetLightPower);
        smoothRoutine = null;
    }

    private void StopSmoothRoutine()
    {
        if (smoothRoutine == null)
            return;

        StopCoroutine(smoothRoutine);
        smoothRoutine = null;
    }
}