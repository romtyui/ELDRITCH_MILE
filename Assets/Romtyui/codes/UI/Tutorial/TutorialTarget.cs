using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class TutorialTarget : MonoBehaviour
{
    private static readonly Dictionary<string, TutorialTarget> registeredTargets = new();

    [Header("Identity")]
    public string targetId;

    public RectTransform RectTransform
    {
        get { return transform as RectTransform; }
    }

    private void OnEnable()
    {
        Register();
    }

    private void OnDisable()
    {
        Unregister();
    }

    private void OnDestroy()
    {
        Unregister();
    }

    private void Register()
    {
        if (string.IsNullOrWhiteSpace(targetId))
            return;

        string id = targetId.Trim();

        if (registeredTargets.TryGetValue(id, out TutorialTarget oldTarget))
        {
            if (oldTarget != null && oldTarget != this)
                Debug.LogWarning($"[TutorialTarget] targetId ­«½Æ¡G{id}", this);
        }

        registeredTargets[id] = this;
    }

    private void Unregister()
    {
        if (string.IsNullOrWhiteSpace(targetId))
            return;

        string id = targetId.Trim();

        if (!registeredTargets.TryGetValue(id, out TutorialTarget target))
            return;

        if (target == this)
            registeredTargets.Remove(id);
    }

    public static TutorialTarget Find(string targetId)
    {
        if (string.IsNullOrWhiteSpace(targetId))
            return null;

        string id = targetId.Trim();

        if (!registeredTargets.TryGetValue(id, out TutorialTarget target))
            return null;

        if (target == null)
        {
            registeredTargets.Remove(id);
            return null;
        }

        return target;
    }
}