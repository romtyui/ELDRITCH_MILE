using System.Collections;
using UnityEngine;

public class EnergyLightPowerBinder : MonoBehaviour
{
    [Header("Refs")]
    public EnergySystem energySystem;
    public PSBMonsterLightReveal lightReveal;

    [Header("Light Power Mapping")]
    [Tooltip("能量為 0 時的最低亮度。不要設成 0，否則會太黑。")]
    [Range(0f, 1f)]
    public float minLightPower = 0.25f;

    [Tooltip("能量滿時的最高亮度。")]
    [Range(0f, 1f)]
    public float maxLightPower = 1f;

    [Header("Response Curve")]
    [Tooltip("數值越大，能量下降時畫面越快變暗。建議 1.5 ~ 3。")]
    public float lightResponsePower = 2f;

    [Header("Smooth")]
    public bool smoothChange = true;
    public float smoothDuration = 0.25f;

    private Coroutine smoothRoutine;

    private void Awake()
    {
        if (energySystem == null)
            energySystem = FindFirstObjectByType<EnergySystem>();

        if (lightReveal == null)
            lightReveal = FindFirstObjectByType<PSBMonsterLightReveal>();
    }

    private void OnEnable()
    {
        if (energySystem != null)
            energySystem.OnEnergyChanged += RefreshLightPower;

        RefreshLightPower();
    }

    private void OnDisable()
    {
        if (energySystem != null)
            energySystem.OnEnergyChanged -= RefreshLightPower;
    }

    public void RefreshLightPower()
    {
        if (energySystem == null || lightReveal == null)
            return;

        float energy01 = 0f;

        if (energySystem.maxEnergy > 0)
            energy01 = Mathf.Clamp01((float)energySystem.currentEnergy / energySystem.maxEnergy);

        float curvedEnergy01 = Mathf.Pow(energy01, lightResponsePower);

        float targetLightPower = Mathf.Lerp(minLightPower, maxLightPower, curvedEnergy01);

        if (smoothChange)
            SmoothSetLightPower(targetLightPower);
        else
            lightReveal.SetLightPower(targetLightPower);

        Debug.Log($"[EnergyLight] Energy = {energySystem.currentEnergy}/{energySystem.maxEnergy}, energy01 = {energy01}, curved = {curvedEnergy01}, lightPower = {targetLightPower}");
    }

    private void SmoothSetLightPower(float targetLightPower)
    {
        if (smoothRoutine != null)
            StopCoroutine(smoothRoutine);

        smoothRoutine = StartCoroutine(SmoothSetLightPowerRoutine(targetLightPower));
    }

    private IEnumerator SmoothSetLightPowerRoutine(float targetLightPower)
    {
        float start = lightReveal.lightPower;
        float timer = 0f;

        while (timer < smoothDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / smoothDuration);
            float smoothT = t * t * (3f - 2f * t);

            float value = Mathf.Lerp(start, targetLightPower, smoothT);
            lightReveal.SetLightPower(value);

            yield return null;
        }

        lightReveal.SetLightPower(targetLightPower);
        smoothRoutine = null;
    }
}