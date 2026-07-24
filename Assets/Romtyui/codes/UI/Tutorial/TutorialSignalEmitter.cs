using UnityEngine;

public class TutorialSignalEmitter : MonoBehaviour
{
	[Header("Signal")]
	[Tooltip("必須和 TutorialStepData.requiredSignal 一致")]
	public string signalId;

	[Header("Debug")]
	public bool logSignal = true;

	public void Emit()
	{
		if (string.IsNullOrWhiteSpace(signalId))
		{
			Debug.LogWarning(
				$"[TutorialSignalEmitter] " +
				$"{gameObject.name} 沒有設定 signalId",
				this
			);

			return;
		}

		string finalSignal = signalId.Trim();

		if (logSignal)
		{
			Debug.Log(
				$"[TutorialSignalEmitter] " +
				$"{gameObject.name} 發送 {finalSignal}",
				this
			);
		}

		TutorialEventBus.Raise(finalSignal);
	}

	public void Emit(string overrideSignalId)
	{
		if (string.IsNullOrWhiteSpace(overrideSignalId))
			return;

		TutorialEventBus.Raise(
			overrideSignalId.Trim()
		);
	}
}