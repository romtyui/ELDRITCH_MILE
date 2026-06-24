using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    [Header("Shake Targets")]
    public List<Transform> shakeTargets = new();

    [Header("Shake Settings")]
    public float defaultDuration = 0.15f;
    public float defaultStrength = 12f;

    private readonly Dictionary<Transform, Vector3> originalPositions = new();
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        Instance = this;
        CacheOriginalPositions();
    }

    private void OnEnable()
    {
        CacheOriginalPositions();
    }

    private void CacheOriginalPositions()
    {
        originalPositions.Clear();

        for (int i = 0; i < shakeTargets.Count; i++)
        {
            Transform target = shakeTargets[i];

            if (target == null)
                continue;

            originalPositions[target] = target.localPosition;
        }
    }

    public void Shake()
    {
        Shake(defaultDuration, defaultStrength);
    }

    public void Shake(float duration, float strength)
    {
        if (shakeTargets == null || shakeTargets.Count == 0)
            return;

        CacheOriginalPositions();

        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        shakeCoroutine = StartCoroutine(ShakeRoutine(duration, strength));
    }

    private IEnumerator ShakeRoutine(float duration, float strength)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            Vector2 randomOffset = Random.insideUnitCircle * strength;

            foreach (var pair in originalPositions)
            {
                Transform target = pair.Key;

                if (target == null)
                    continue;

                Vector3 origin = pair.Value;
                target.localPosition = origin + new Vector3(randomOffset.x, randomOffset.y, 0f);
            }

            yield return null;
        }

        ResetTargets();

        shakeCoroutine = null;
    }

    private void ResetTargets()
    {
        foreach (var pair in originalPositions)
        {
            Transform target = pair.Key;

            if (target == null)
                continue;

            target.localPosition = pair.Value;
        }
    }
}