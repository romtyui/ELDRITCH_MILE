using System;
using UnityEngine;

public class GodCardAnimationSignalEmitter : MonoBehaviour
{
    // =========================================================
    // Events
    // =========================================================

    public event Action TransformMoment;

    public event Action AnimationFinished;


    // =========================================================
    // Transform Moment
    // =========================================================

    public void RaiseTransformMoment()
    {
        Debug.Log(
            $"[GodCardAnimationSignalEmitter] " +
            $"Transform Moment�G{gameObject.name}"
        );

        TransformMoment?.Invoke();
    }


    // =========================================================
    // Animation Finished
    // =========================================================

    public void RaiseAnimationFinished()
    {
        Debug.Log(
            $"[GodCardAnimationSignalEmitter] " +
            $"Animation Finished�G{gameObject.name}"
        );

        AnimationFinished?.Invoke();
    }
}