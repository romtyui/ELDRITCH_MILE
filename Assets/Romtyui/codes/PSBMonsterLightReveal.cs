using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PSBMonsterLightReveal : MonoBehaviour
{
    [System.Serializable]
    public class MonsterRevealTarget
    {
        public Transform normalRoot;
        public Transform darkRoot;

        [HideInInspector] public SpriteRenderer[] normalRenderers;
        [HideInInspector] public SpriteRenderer[] darkRenderers;

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
                : null;

            darkRenderers = darkRoot != null
                ? darkRoot.GetComponentsInChildren<SpriteRenderer>(true)
                : null;
        }
    }

    [Header("Light Source")]
    public Transform lightOrigin;

    [Header("Runtime Monster Targets")]
    public List<MonsterRevealTarget> monsterTargets = new();

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

    private MaterialPropertyBlock block;

    private void Awake()
    {
        block = new MaterialPropertyBlock();

        RefreshAllTargets();
    }

    private void Update()
    {
        if (lightOrigin == null)
            return;

        float radius = Mathf.Lerp(minRadius, maxRadius, lightPower);

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

            UpdateRenderers(target.normalRenderers, radius);
            UpdateRenderers(target.darkRenderers, radius);
        }

        if (visualLight != null)
        {
            visualLight.intensity = lightPower * maxLightIntensity;
            visualLight.pointLightOuterRadius = Mathf.Lerp(minRadius, maxLightOuterRadius, lightPower);
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
            return;
        }

        MonsterRevealTarget target = new MonsterRevealTarget(normalRoot, darkRoot);
        monsterTargets.Add(target);

        Debug.Log($"[PSBMonsterLightReveal] 註冊怪物 normalRoot = {(normalRoot != null ? normalRoot.name : "null")}, darkRoot = {(darkRoot != null ? darkRoot.name : "null")}");
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

            bool sameNormal = target.normalRoot == normalRoot;
            bool sameDark = target.darkRoot == darkRoot;

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

    private void UpdateRenderers(SpriteRenderer[] renderers, float radius)
    {
        if (renderers == null)
            return;

        foreach (SpriteRenderer sr in renderers)
        {
            if (sr == null)
                continue;

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