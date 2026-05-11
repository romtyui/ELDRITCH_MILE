using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PSBMonsterLightReveal : MonoBehaviour
{
    [Header("Light Source")]
    public Transform lightOrigin;

    [Header("PSB Roots")]
    public Transform normalRoot;
    public Transform darkRoot;

    [Header("Optional Visual Light")]
    public Light2D visualLight;

    [Header("Light Settings")]
    [Range(0f, 1f)]
    public float lightPower = 1f;

    public float minRadius = 1.5f;
    public float maxRadius = 8f;
    public float feather = 2f;

    [Header("Visual Light Settings")]
    public float maxLightIntensity = 1.5f;
    public float maxLightOuterRadius = 8f;

    private SpriteRenderer[] normalRenderers;
    private SpriteRenderer[] darkRenderers;

    private MaterialPropertyBlock block;

    void Awake()
    {
        if (normalRoot != null)
            normalRenderers = normalRoot.GetComponentsInChildren<SpriteRenderer>(true);

        if (darkRoot != null)
            darkRenderers = darkRoot.GetComponentsInChildren<SpriteRenderer>(true);

        block = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (lightOrigin == null) return;

        float radius = Mathf.Lerp(minRadius, maxRadius, lightPower);

        UpdateRenderers(normalRenderers, radius);
        UpdateRenderers(darkRenderers, radius);

        if (visualLight != null)
        {
            visualLight.intensity = lightPower * maxLightIntensity;
            visualLight.pointLightOuterRadius = Mathf.Lerp(minRadius, maxLightOuterRadius, lightPower);
        }
    }

    void UpdateRenderers(SpriteRenderer[] renderers, float radius)
    {
        if (renderers == null) return;

        foreach (SpriteRenderer sr in renderers)
        {
            if (sr == null) continue;

            sr.GetPropertyBlock(block);

            block.SetVector("_LightWorldPos", lightOrigin.position);
            block.SetFloat("_Radius", radius);
            block.SetFloat("_Feather", feather);
            block.SetFloat("_LightPower", lightPower);

            sr.SetPropertyBlock(block);
        }
    }

    public void SetLightPower(float value)
    {
        lightPower = Mathf.Clamp01(value);
    }
}