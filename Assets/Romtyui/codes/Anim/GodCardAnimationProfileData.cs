using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/God Card/God Animation Profile")]
public class GodCardAnimationProfileData : ScriptableObject
{
    [Header("Info")]
    public string animationName;

    [Header("Tentacle Animation")]
    public GameObject tentacleRootPrefab;

    [Tooltip("如果這張神牌要用不同 Animator Controller，可以指定。可空。")]
    public RuntimeAnimatorController animatorController;

    [Header("Animator State Names")]
    public string idleStateName = "New Animation";
    public string appearStateName = "Appear";
    public string shakeStateName = "shake";
    public string reachBookStateName = "ReachBook";
    public string holdCardStateName = "HoldCard";
    public string returnCardStateName = "ReturnCard";

    [Header("Timing")]
    public float cardShowStayTime = 0.85f;
    public float previewCardScale = 0.75f;

    [Header("Optional")]
    public AudioClip animationSound;
}