using System.Collections.Generic;
using UnityEngine;

public class AnimationReferenceHolder : MonoBehaviour
{
    public List<GodCardAnimationData> godAnimations = new();
    public List<RuntimeAnimatorController> animatorControllers = new();
    public List<AnimationClip> animationClips = new();
}