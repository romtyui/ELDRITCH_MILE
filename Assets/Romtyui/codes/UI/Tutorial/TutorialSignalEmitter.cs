using UnityEngine;

public class TutorialSignalEmitter :
    MonoBehaviour
{
    [Header("Signal")]
    [Tooltip("必須與 TutorialStepData.requiredSignal 相同")]
    public string signalId;

    [Header("Debug")]
    public bool logSignal = true;

    public void Emit()
    {
        Emit(signalId);
    }

    public void Emit(
        string overrideSignalId
    )
    {
        if (string.IsNullOrWhiteSpace(
                overrideSignalId))
        {
            Debug.LogWarning(
                $"[TutorialSignalEmitter] " +
                $"{gameObject.name} 沒有 Signal ID",
                this
            );

            return;
        }

        string finalSignal =
            overrideSignalId.Trim();

        if (logSignal)
        {
            Debug.Log(
                $"[TutorialSignalEmitter] " +
                $"{gameObject.name} 發送：" +
                $"{finalSignal}",
                this
            );
        }

        TutorialEventBus.Raise(finalSignal);
    }
}