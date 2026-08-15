using UnityEngine;

public class MonsterVisualBlendController : MonoBehaviour
{
    [Header("Roots")]
    public Transform normalRoot;
    public Transform darkRoot;

    [Header("Blend")]
    [Range(0f, 1f)]
    public float lightPower = 1f;

    [Tooltip("勾選後，lightPower 越高 normal 越明顯，dark 越淡。")]
    public bool normalVisibleWhenLight = true;

    private SpriteRenderer[] normalRenderers;
    private SpriteRenderer[] darkRenderers;

    private void Awake()
    {
        RefreshRenderers();
        ApplyBlend();
    }

    [ContextMenu("Refresh Renderers")]
    public void RefreshRenderers()
    {
        normalRenderers = normalRoot != null
            ? normalRoot.GetComponentsInChildren<SpriteRenderer>(true)
            : new SpriteRenderer[0];

        darkRenderers = darkRoot != null
            ? darkRoot.GetComponentsInChildren<SpriteRenderer>(true)
            : new SpriteRenderer[0];
    }

    public void SetLightPower(float value)
    {
        lightPower = Mathf.Clamp01(value);
        ApplyBlend();
    }

    private void ApplyBlend()
    {
        float normalAlpha;
        float darkAlpha;

        if (normalVisibleWhenLight)
        {
            normalAlpha = lightPower;
            darkAlpha = 1f - lightPower;
        }
        else
        {
            normalAlpha = 1f - lightPower;
            darkAlpha = lightPower;
        }

        SetRenderersAlpha(normalRenderers, normalAlpha);
        SetRenderersAlpha(darkRenderers, darkAlpha);
    }

    private void SetRenderersAlpha(SpriteRenderer[] renderers, float alpha)
    {
        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];

            if (sr == null)
                continue;

            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }
}