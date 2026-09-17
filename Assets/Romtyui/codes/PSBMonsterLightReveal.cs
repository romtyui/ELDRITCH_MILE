using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PSBMonsterLightReveal : MonoBehaviour
{
    [System.Serializable]
    public class MonsterRevealTarget
    {
        [Header("Runtime Roots")]
        public Transform normalRoot;
        public Transform darkRoot;

        [Header("Runtime Renderers")]
        public SpriteRenderer[] normalRenderers;
        public SpriteRenderer[] darkRenderers;

        public MonsterRevealTarget(
            Transform normalRoot,
            Transform darkRoot
        )
        {
            this.normalRoot = normalRoot;
            this.darkRoot = darkRoot;

            RefreshRenderers();
        }

        public void RefreshRenderers()
        {
            normalRenderers = normalRoot != null
                ? normalRoot.GetComponentsInChildren<SpriteRenderer>(true)
                : new SpriteRenderer[0];

            darkRenderers = darkRoot != null
                ? darkRoot.GetComponentsInChildren<SpriteRenderer>(true)
                : new SpriteRenderer[0];
        }
    }

    [Header("Runtime Monster Targets")]
    public List<MonsterRevealTarget> monsterTargets = new();

    [Header("Runtime Values")]
    [Tooltip("經過曲線處理的燈光數值，只控制 Light 2D。")]
    [Range(0f, 1f)]
    public float lightPower = 1f;

    [Tooltip("原始 SAN 比例。0 = SAN 0%，1 = SAN 100%。")]
    [Range(0f, 1f)]
    public float sanRatio = 1f;

    [Header("Form Blend Threshold")]
    [Tooltip("SAN 低於此比例時，只顯示完整黑暗型態。")]
    [Range(0f, 1f)]
    public float darkToNormalStart = 0.25f;

    [Tooltip("SAN 高於此比例時，只顯示完整亮型態。")]
    [Range(0f, 1f)]
    public float darkToNormalEnd = 0.75f;

    [Header("Renderer Optimization")]
    [Tooltip("在 Form Blend 完全為 0 或 1 時，停用看不到的另一套 Renderer。")]
    public bool disableHiddenFormRenderers = true;

    [Tooltip("判斷 Form Blend 是否已到達端點的誤差。")]
    [Range(0f, 0.01f)]
    public float endpointEpsilon = 0.0001f;

    [Header("Freeform Visual Light")]
    [Tooltip("控制怪物照明與溶解區域的 Freeform Light 2D。")]
    public Light2D visualLight;

    [Header("Visual Light Intensity")]
    [Tooltip("lightPower 為 0 時的 Freeform Light 強度。")]
    public float minLightIntensity = 0.25f;

    [Tooltip("lightPower 為 1 時的 Freeform Light 強度。")]
    public float maxLightIntensity = 5.01f;

    [Header("Freeform Light Falloff")]
    [Tooltip("SAN 為 0% 時的最小 Falloff。")]
    [Min(0f)]
    public float minLightFalloff = 0.1f;

    [Tooltip("SAN 為 100% 時的最大 Falloff。")]
    [Min(0f)]
    public float maxLightFalloff = 0.65f;

    [Tooltip("Falloff 是否使用原始 SAN 比例控制。建議開啟，才能準確到達最小值與最大值。")]
    public bool useSanRatioForFalloff = true;

    [Tooltip("Falloff 反應曲線。1 = 線性；大於 1 時，SAN 降低後 Falloff 會更快縮小。")]
    [Min(0.01f)]
    public float falloffResponsePower = 1f;

    [Header("Legacy Point Light Radius")]
    [Tooltip("Freeform Light 不使用此半徑；保留供 Point Light 相容。")]
    public float minLightOuterRadius = 1.5f;

    [Tooltip("Freeform Light 不使用此半徑；保留供 Point Light 相容。")]
    public float maxLightOuterRadius = 8f;

    private static readonly int FormBlendId =
        Shader.PropertyToID("_FormBlend");

    private static readonly int RevealLightIntensityId =
        Shader.PropertyToID("_RevealLightIntensity");

    private readonly HashSet<Material> runtimeMaterials = new();

    private void Awake()
    {
        RefreshAllTargets();
        PrepareAllTargets();
    }

    private void OnEnable()
    {
        RefreshAllTargets();
        PrepareAllTargets();

        UpdateVisualLight();
        ApplyShaderValues(CalculateFormBlend());
    }

    private void Update()
    {
        // 先更新 Freeform Light，接著把這一幀的實際強度傳給 Shader。
        UpdateVisualLight();

        float formBlend = CalculateFormBlend();
        ApplyShaderValues(formBlend);
    }

    private float CalculateFormBlend()
    {
        float clampedSanRatio = Mathf.Clamp01(sanRatio);

        float start = Mathf.Min(
            darkToNormalStart,
            darkToNormalEnd
        );

        float end = Mathf.Max(
            darkToNormalStart,
            darkToNormalEnd
        );

        if (Mathf.Approximately(start, end))
        {
            return clampedSanRatio >= end
                ? 1f
                : 0f;
        }

        // 使用線性過渡，讓變化平均分布在 SAN 25%～75%。
        return Mathf.InverseLerp(
            start,
            end,
            clampedSanRatio
        );
    }

    private void ApplyShaderValues(float formBlend)
    {
        formBlend = Mathf.Clamp01(formBlend);

        bool normalVisible =
            !disableHiddenFormRenderers ||
            formBlend > endpointEpsilon;

        bool darkVisible =
            !disableHiddenFormRenderers ||
            formBlend < 1f - endpointEpsilon;

        runtimeMaterials.Clear();

        for (int i = monsterTargets.Count - 1; i >= 0; i--)
        {
            MonsterRevealTarget target = monsterTargets[i];

            if (target == null)
            {
                monsterTargets.RemoveAt(i);
                continue;
            }

            if (target.normalRoot == null &&
                target.darkRoot == null)
            {
                monsterTargets.RemoveAt(i);
                continue;
            }

            CollectMaterialsAndSetVisibility(
                target.normalRenderers,
                normalVisible
            );

            CollectMaterialsAndSetVisibility(
                target.darkRenderers,
                darkVisible
            );
        }

        float revealLightIntensity = visualLight != null
            ? Mathf.Max(0.001f, visualLight.intensity)
            : 1f;

        foreach (Material material in runtimeMaterials)
        {
            if (material == null)
                continue;

            if (material.HasProperty(FormBlendId))
            {
                material.SetFloat(
                    FormBlendId,
                    formBlend
                );
            }

            if (material.HasProperty(RevealLightIntensityId))
            {
                material.SetFloat(
                    RevealLightIntensityId,
                    revealLightIntensity
                );
            }
        }
    }

    private void CollectMaterialsAndSetVisibility(
        SpriteRenderer[] renderers,
        bool visible
    )
    {
        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = renderers[i];

            if (spriteRenderer == null)
                continue;

            spriteRenderer.enabled = visible;

            // 不再用 SpriteRenderer Alpha 控制型態。
            Color color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;

            Material material = spriteRenderer.sharedMaterial;

            if (material != null)
                runtimeMaterials.Add(material);
        }
    }

    private void UpdateVisualLight()
    {
        if (visualLight == null)
            return;

        float clampedLightPower = Mathf.Clamp01(lightPower);
        float clampedSanRatio = Mathf.Clamp01(sanRatio);

        // 控制 Light 2D 強度。
        visualLight.intensity = Mathf.Lerp(
            minLightIntensity,
            maxLightIntensity,
            clampedLightPower
        );

        // Falloff 建議使用原始 SAN。
        // 這樣 SAN 0% 一定會到 minLightFalloff，
        // SAN 100% 一定會到 maxLightFalloff。
        float falloffInput = useSanRatioForFalloff
            ? clampedSanRatio
            : clampedLightPower;

        float responsePower = Mathf.Max(
            0.01f,
            falloffResponsePower
        );

        float falloffT = Mathf.Pow(
            falloffInput,
            responsePower
        );

        float safeMinFalloff = Mathf.Max(
            0f,
            Mathf.Min(minLightFalloff, maxLightFalloff)
        );

        float safeMaxFalloff = Mathf.Max(
            safeMinFalloff,
            Mathf.Max(minLightFalloff, maxLightFalloff)
        );

        visualLight.shapeLightFalloffSize = Mathf.Lerp(
            safeMinFalloff,
            safeMaxFalloff,
            falloffT
        );

        // 這個屬性只對 Point Light 有明確作用。
        // 保留是為了相容舊設定，但不控制 Freeform Falloff。
        visualLight.pointLightOuterRadius = Mathf.Lerp(
            minLightOuterRadius,
            maxLightOuterRadius,
            clampedLightPower
        );
    }

    public void RegisterMonster(
        Transform normalRoot,
        Transform darkRoot
    )
    {
        if (normalRoot == null && darkRoot == null)
        {
            Debug.LogWarning(
                "[PSBMonsterLightReveal] normalRoot 和 darkRoot 都是 null，無法註冊怪物。"
            );

            return;
        }

        MonsterRevealTarget existing =
            FindTarget(normalRoot, darkRoot);

        if (existing != null)
        {
            existing.RefreshRenderers();
            PrepareTarget(existing);

            UpdateVisualLight();
            ApplyShaderValues(CalculateFormBlend());

            Debug.Log(
                $"[PSBMonsterLightReveal] 已存在，刷新怪物 roots：normal = {GetName(normalRoot)}, dark = {GetName(darkRoot)}"
            );

            return;
        }

        MonsterRevealTarget target =
            new MonsterRevealTarget(normalRoot, darkRoot);

        monsterTargets.Add(target);

        PrepareTarget(target);

        UpdateVisualLight();
        ApplyShaderValues(CalculateFormBlend());

        Debug.Log(
            $"[PSBMonsterLightReveal] 自動註冊怪物 roots：normal = {GetName(normalRoot)}, dark = {GetName(darkRoot)}"
        );
    }

    public void UnregisterMonster(
        Transform normalRoot,
        Transform darkRoot
    )
    {
        for (int i = monsterTargets.Count - 1; i >= 0; i--)
        {
            MonsterRevealTarget target = monsterTargets[i];

            if (target == null)
            {
                monsterTargets.RemoveAt(i);
                continue;
            }

            bool sameNormal =
                normalRoot != null &&
                target.normalRoot == normalRoot;

            bool sameDark =
                darkRoot != null &&
                target.darkRoot == darkRoot;

            if (sameNormal || sameDark)
            {
                monsterTargets.RemoveAt(i);
            }
        }
    }

    public void ClearTargets()
    {
        monsterTargets.Clear();
        runtimeMaterials.Clear();
    }

    public void RefreshAllTargets()
    {
        for (int i = 0; i < monsterTargets.Count; i++)
        {
            MonsterRevealTarget target = monsterTargets[i];

            if (target != null)
                target.RefreshRenderers();
        }
    }

    public void SetLightPower(float value)
    {
        lightPower = Mathf.Clamp01(value);
    }

    public void SetSanRatio(float value)
    {
        sanRatio = Mathf.Clamp01(value);
    }

    private void PrepareAllTargets()
    {
        for (int i = 0; i < monsterTargets.Count; i++)
        {
            MonsterRevealTarget target = monsterTargets[i];

            if (target != null)
                PrepareTarget(target);
        }
    }

    private void PrepareTarget(MonsterRevealTarget target)
    {
        if (target == null)
            return;

        PrepareRenderers(target.normalRenderers);
        PrepareRenderers(target.darkRenderers);
    }

    private void PrepareRenderers(
        SpriteRenderer[] renderers
    )
    {
        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = renderers[i];

            if (spriteRenderer == null)
                continue;

            Color color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;

            spriteRenderer.enabled = true;

            Material material = spriteRenderer.sharedMaterial;

            if (material == null)
                continue;

            if (!material.HasProperty(FormBlendId))
            {
                Debug.LogWarning(
                    $"[PSBMonsterLightReveal] {spriteRenderer.name} 的材質 {material.name} 沒有 _FormBlend。"
                );
            }

            if (!material.HasProperty(RevealLightIntensityId))
            {
                Debug.LogWarning(
                    $"[PSBMonsterLightReveal] {spriteRenderer.name} 的材質 {material.name} 沒有 _RevealLightIntensity。"
                );
            }
        }
    }

    private MonsterRevealTarget FindTarget(
        Transform normalRoot,
        Transform darkRoot
    )
    {
        for (int i = 0; i < monsterTargets.Count; i++)
        {
            MonsterRevealTarget target = monsterTargets[i];

            if (target == null)
                continue;

            if (target.normalRoot == normalRoot &&
                target.darkRoot == darkRoot)
            {
                return target;
            }
        }

        return null;
    }

    private string GetName(Transform target)
    {
        return target != null
            ? target.name
            : "null";
    }
}