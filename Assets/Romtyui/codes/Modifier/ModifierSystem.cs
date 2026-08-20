using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ModifierSystem : MonoBehaviour
{
    public static ModifierSystem Instance { get; private set; }

    [Header("Default Providers")]
    [SerializeField]
    private RelicsRuntime relicsRuntime;

    private readonly List<IModifierProvider> providers = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("[ModifierSystem] 場景中存在超過一個 ModifierSystem", this);
            enabled = false;
            return;
        }

        Instance = this;

        if (relicsRuntime == null)
            relicsRuntime = FindFirstObjectByType<RelicsRuntime>();

        if (relicsRuntime != null)
            RegisterProvider(relicsRuntime);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RegisterProvider(IModifierProvider provider)
    {
        if (provider == null)
            return;

        if (providers.Contains(provider))
            return;

        providers.Add(provider);
    }

    public void UnregisterProvider(IModifierProvider provider)
    {
        if (provider == null)
            return;

        providers.Remove(provider);
    }

    public int ModifyInt(ModifierQuery query, int baseValue)
    {
        if (query == null)
            return baseValue;

        List<ModifierValue> modifiers = ListPool<ModifierValue>.Get();

        try
        {
            CollectModifiers(query, modifiers);

            return ModifierCalculator.CalculateInt(
                baseValue,
                modifiers,
                query.roundingMode,
                query.clampResultToZero
            );
        }
        finally
        {
            ListPool<ModifierValue>.Release(modifiers);
        }
    }

    public float ModifyFloat(ModifierQuery query, float baseValue)
    {
        if (query == null)
            return baseValue;

        List<ModifierValue> modifiers = ListPool<ModifierValue>.Get();

        try
        {
            CollectModifiers(query, modifiers);

            return ModifierCalculator.CalculateFloat(
                baseValue,
                modifiers,
                query.clampResultToZero
            );
        }
        finally
        {
            ListPool<ModifierValue>.Release(modifiers);
        }
    }

    private void CollectModifiers(ModifierQuery query, List<ModifierValue> results)
    {
        if (query == null || results == null)
            return;

        for (int i = 0; i < providers.Count; i++)
        {
            IModifierProvider provider = providers[i];

            if (provider == null)
                continue;

            provider.CollectModifiers(query, results);
        }
    }
}