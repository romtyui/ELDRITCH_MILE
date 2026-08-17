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

        public MonsterRevealTarget(Transform normalRoot, Transform darkRoot)
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

    [Header("Blend Settings")]
    [Range(0f, 1f)]
    public float lightPower = 1f;

    [Tooltip("lightPower = 0 時，普通型態最低透明度")]
    [Range(0f, 1f)]
    public float minNormalAlpha = 0f;

    [Tooltip("lightPower = 1 時，普通型態最高透明度")]
    [Range(0f, 1f)]
    public float maxNormalAlpha = 1f;

    [Tooltip("lightPower = 0 時，黑暗型態最高透明度")]
    [Range(0f, 1f)]
    public float maxDarkAlpha = 1f;

    [Tooltip("lightPower = 1 時，黑暗型態最低透明度")]
    [Range(0f, 1f)]
    public float minDarkAlpha = 0f;

    [Header("Optional Visual Light")]
    public Light2D visualLight;

    [Header("Blend Threshold")]
    [Range(0f, 1f)] public float darkToNormalStart = 0.45f;
    [Range(0f, 1f)] public float darkToNormalEnd = 0.55f;
    [Header("Anti Flicker")]
    [Range(0f, 1f)] public float hideAlphaThreshold = 0.03f;

    [Header("Visual Light Settings")]
    public float minLightIntensity = 0.25f;
    public float maxLightIntensity = 1.5f;

    public float minLightOuterRadius = 1.5f;
    public float maxLightOuterRadius = 8f;

    private void Awake()
    {
        RefreshAllTargets();
    }

    private void Update()
    {
        float clampedPower = Mathf.Clamp01(lightPower);

        float normalAlpha = Mathf.Lerp(minNormalAlpha, maxNormalAlpha, clampedPower);
        float darkAlpha = Mathf.Lerp(maxDarkAlpha, minDarkAlpha, clampedPower);

        for (int i = monsterTargets.Count - 1; i >= 0; i--)
        {
            MonsterRevealTarget target = monsterTargets[i];

            if (target == null)
            {
                monsterTargets.RemoveAt(i);
                continue;
            }

            if (target.normalRoot == null && target.darkRoot == null)
            {
                monsterTargets.RemoveAt(i);
                continue;
            }

            SetRenderersAlpha(target.normalRenderers, normalAlpha);
            SetRenderersAlpha(target.darkRenderers, darkAlpha);
        }

        if (visualLight != null)
        {
            visualLight.intensity = Mathf.Lerp(minLightIntensity, maxLightIntensity, clampedPower);
            visualLight.pointLightOuterRadius = Mathf.Lerp(minLightOuterRadius, maxLightOuterRadius, clampedPower);
        }
    }

    public void RegisterMonster(Transform normalRoot, Transform darkRoot)
    {
        if (normalRoot == null && darkRoot == null)
        {
            Debug.LogWarning("[PSBMonsterLightReveal] normalRoot 和 darkRoot 都是 null，無法註冊怪物");
            return;
        }

        MonsterRevealTarget existing = FindTarget(normalRoot, darkRoot);

        if (existing != null)
        {
            existing.RefreshRenderers();
            Debug.Log($"[PSBMonsterLightReveal] 已存在，刷新怪物 roots：normal = {GetName(normalRoot)}, dark = {GetName(darkRoot)}");
            return;
        }

        MonsterRevealTarget target = new MonsterRevealTarget(normalRoot, darkRoot);
        monsterTargets.Add(target);

        Debug.Log($"[PSBMonsterLightReveal] 自動註冊怪物 roots：normal = {GetName(normalRoot)}, dark = {GetName(darkRoot)}");
    }

    public void UnregisterMonster(Transform normalRoot, Transform darkRoot)
    {
        for (int i = monsterTargets.Count - 1; i >= 0; i--)
        {
            MonsterRevealTarget target = monsterTargets[i];

            if (target == null)
            {
                monsterTargets.RemoveAt(i);
                continue;
            }

            bool sameNormal = normalRoot != null && target.normalRoot == normalRoot;
            bool sameDark = darkRoot != null && target.darkRoot == darkRoot;

            if (sameNormal || sameDark)
            {
                monsterTargets.RemoveAt(i);
            }
        }
    }

    public void ClearTargets()
    {
        monsterTargets.Clear();
    }

    public void RefreshAllTargets()
    {
        for (int i = 0; i < monsterTargets.Count; i++)
        {
            if (monsterTargets[i] != null)
                monsterTargets[i].RefreshRenderers();
        }
    }

    public void SetLightPower(float value)
    {
        lightPower = Mathf.Clamp01(value);
    }

    private MonsterRevealTarget FindTarget(Transform normalRoot, Transform darkRoot)
    {
        for (int i = 0; i < monsterTargets.Count; i++)
        {
            MonsterRevealTarget target = monsterTargets[i];

            if (target == null)
                continue;

            if (target.normalRoot == normalRoot && target.darkRoot == darkRoot)
                return target;
        }

        return null;
    }

    private void SetRenderersAlpha(SpriteRenderer[] renderers, float alpha)
    {
        if (renderers == null)
            return;

        bool visible = alpha > hideAlphaThreshold;

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];

            if (sr == null)
                continue;

            sr.enabled = visible;

            if (!visible)
                continue;

            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }

    private string GetName(Transform target)
    {
        return target != null ? target.name : "null";
    }
}