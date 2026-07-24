using System;
using UnityEngine;

public static class TutorialEventBus
{
    public static event Action<string> OnSignalRaised;

    public static void Raise(string signalId)
    {
        if (string.IsNullOrWhiteSpace(signalId))
            return;

        string normalized = signalId.Trim();

        Debug.Log(
            $"[TutorialEventBus] µo°e¨Æ¥ó¡G{normalized}"
        );

        OnSignalRaised?.Invoke(normalized);
    }
}