using UnityEngine;

public class GodCardAnimationEventRelay : MonoBehaviour
{
    private GodCardCorruptionAnimationController controller;


    public void Initialize(
        GodCardCorruptionAnimationController targetController
    )
    {
        controller =
            targetController;
    }


    // =========================================================
    // Animation Event
    // =========================================================

    public void AnimEvent_GodCorruptionFinished()
    {
        if (controller == null)
        {
            Debug.LogWarning(
                "[GodCardAnimationEventRelay] " +
                "Controller 尚未設定"
            );

            return;
        }


        //controller.AnimEvent_GodCorruptionFinished();
    }
}